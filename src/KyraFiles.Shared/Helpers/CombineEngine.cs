// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.Helpers;

/// <summary>
/// Política de tratamento quando já existe um arquivo com o nome de destino na recombinação (F060 M1).
/// </summary>
public enum CombineCollisionPolicy
{
	/// <summary>
	/// Recusa a operação e lança exceção se o arquivo de destino já existir (padrão seguro).
	/// </summary>
	Recusar = 0,

	/// <summary>
	/// Sobrescreve o arquivo de destino existente.
	/// </summary>
	Sobrescrever = 1,

	/// <summary>
	/// Renomeia automaticamente o arquivo gerado adicionando sufixo numérico (ex.: "arquivo (1).ext").
	/// </summary>
	Renomear = 2
}

/// <summary>
/// Motivo de falha na validação prévia das partes ou manifesto antes da recombinação (F060 M1).
/// </summary>
public enum CombineValidationFailureReason
{
	/// <summary>
	/// Uma ou mais partes necessárias não foram encontradas no disco.
	/// </summary>
	MissingPart,

	/// <summary>
	/// Há partes repetidas ou com índices/nomes duplicados.
	/// </summary>
	DuplicatePart,

	/// <summary>
	/// Uma parte possui tamanho em bytes menor do que o especificado no manifesto (truncada).
	/// </summary>
	TruncatedPart,

	/// <summary>
	/// Uma parte foi alterada (tamanho divergente ou resumo CRC-32/SHA-256 não confere).
	/// </summary>
	CorruptedPart,

	/// <summary>
	/// As partes estão fora de sequência (índices não formam 1..N ordenado).
	/// </summary>
	OutOfOrder,

	/// <summary>
	/// Manifesto inválido ou sem partes registradas.
	/// </summary>
	InvalidManifest
}

/// <summary>
/// Exceção lançada quando a validação prévia de partes ou manifesto falha antes da montagem (F060 M1).
/// Identifica qual parte causou a falha e o motivo detalhado.
/// </summary>
public sealed class CombineValidationException : Exception
{
	/// <summary>
	/// Motivo da falha de validação.
	/// </summary>
	public CombineValidationFailureReason Reason { get; }

	/// <summary>
	/// Índice da parte que causou a falha, se aplicável.
	/// </summary>
	public int? PartIndex { get; }

	/// <summary>
	/// Nome do arquivo da parte que causou a falha, se aplicável.
	/// </summary>
	public string? PartFileName { get; }

	public CombineValidationException(
		string message,
		CombineValidationFailureReason reason,
		int? partIndex = null,
		string? partFileName = null)
		: base(message)
	{
		Reason = reason;
		PartIndex = partIndex;
		PartFileName = partFileName;
	}
}

/// <summary>
/// Opções de configuração para a recombinação de arquivos particionados (F060 M1).
/// </summary>
public sealed class CombineOptions
{
	/// <summary>
	/// Política quando o arquivo de destino já existe. Padrão: <see cref="CombineCollisionPolicy.Recusar"/>.
	/// </summary>
	public CombineCollisionPolicy CollisionPolicy { get; set; } = CombineCollisionPolicy.Recusar;

	/// <summary>
	/// Diretório de destino onde o arquivo combinado final será gerado.
	/// Se for nulo ou vazio, utiliza o mesmo diretório das partes/manifesto.
	/// </summary>
	public string? DestinationDirectory { get; set; }

	/// <summary>
	/// Nome do arquivo de destino combinado.
	/// Se for nulo ou vazio, utiliza o <see cref="SplitManifest.OriginalFileName"/> registrado no manifesto.
	/// </summary>
	public string? DestinationFileName { get; set; }

	/// <summary>
	/// Tamanho do buffer de leitura e gravação em bytes. Padrão: 64 KB (65.536 bytes).
	/// </summary>
	public int BufferSize { get; set; } = 64 * 1024;

	/// <summary>
	/// Indica se deve conferir o SHA-256 caso esteja presente no manifesto ou partes. Padrão: true.
	/// </summary>
	public bool VerifySha256 { get; set; } = true;

	/// <summary>
	/// Fábrica opcional para criação da stream de gravação do arquivo temporário.
	/// Permite simular falhas de gravação (ex.: falta de espaço em disco) em testes unitários.
	/// </summary>
	public Func<string, FileMode, FileAccess, FileShare, int, FileOptions, Stream>? DestinationStreamFactory { get; set; }
}

/// <summary>
/// Informações de progresso da recombinação de arquivos.
/// </summary>
public readonly record struct CombineProgress(
	long TotalBytesWritten,
	long TotalBytes,
	int CurrentPartIndex,
	int TotalParts);

/// <summary>
/// Resultado da operação de recombinação de arquivos.
/// </summary>
public sealed class CombineResult
{
	/// <summary>
	/// Caminho completo do arquivo combinado promovido com sucesso.
	/// </summary>
	public required string DestinationFilePath { get; init; }

	/// <summary>
	/// Total de bytes gravados no arquivo combinado final.
	/// </summary>
	public required long TotalBytesWritten { get; init; }

	/// <summary>
	/// Resumo CRC-32 calculado do arquivo final (8 caracteres hex maiúsculos).
	/// </summary>
	public required string TotalCrc32 { get; init; }

	/// <summary>
	/// Resumo SHA-256 calculado do arquivo final (hex minúsculo), se calculado.
	/// </summary>
	public string? TotalSha256 { get; init; }

	/// <summary>
	/// Quantidade total de partes combinadas.
	/// </summary>
	public required int TotalPartsCombined { get; init; }
}

/// <summary>
/// Motor de validação e recombinação de partes com integridade estrita e montagem atômica (F060 M1).
/// </summary>
public static class CombineEngine
{
	/// <summary>
	/// Valida integralmente as partes e o manifesto antes de qualquer montagem.
	/// Verifica ausente, duplicada, truncada, alterada (CRC-32/SHA-256) e fora de ordem.
	/// </summary>
	public static async Task ValidatePartsAsync(
		SplitManifest manifest,
		string partsDirectory,
		CombineOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		if (manifest is null)
			throw new ArgumentNullException(nameof(manifest));

		if (string.IsNullOrWhiteSpace(partsDirectory))
			throw new ArgumentException("O diretório das partes não pode ser nulo ou vazio.", nameof(partsDirectory));

		if (manifest.Parts is null || manifest.Parts.Count == 0)
		{
			throw new CombineValidationException(
				"O manifesto não contém nenhuma parte registrada para validação.",
				CombineValidationFailureReason.InvalidManifest);
		}

		int bufferSize = options?.BufferSize ?? (64 * 1024);
		if (bufferSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(options), "O tamanho do buffer deve ser maior que zero.");

		// 1. Validação de ordem, unicidade e índices no manifesto
		var seenIndices = new HashSet<int>();
		var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < manifest.Parts.Count; i++)
		{
			var part = manifest.Parts[i];
			if (part is null)
			{
				throw new CombineValidationException(
					$"A entrada na posição {i + 1} da lista de partes é nula.",
					CombineValidationFailureReason.InvalidManifest);
			}

			EnsureSafeFileName(part.FileName, part.Index);

			if (!seenIndices.Add(part.Index))
			{
				throw new CombineValidationException(
					$"Parte duplicada: o índice {part.Index} ('{part.FileName}') aparece mais de uma vez no manifesto.",
					CombineValidationFailureReason.DuplicatePart,
					part.Index,
					part.FileName);
			}

			if (!seenNames.Add(part.FileName))
			{
				throw new CombineValidationException(
					$"Parte duplicada: o nome de arquivo '{part.FileName}' (índice {part.Index}) aparece mais de uma vez no manifesto.",
					CombineValidationFailureReason.DuplicatePart,
					part.Index,
					part.FileName);
			}

			int expectedIndex = i + 1;
			if (part.Index != expectedIndex)
			{
				throw new CombineValidationException(
					$"Partes fora de ordem: esperava-se o índice {expectedIndex}, mas foi encontrado {part.Index} ('{part.FileName}').",
					CombineValidationFailureReason.OutOfOrder,
					part.Index,
					part.FileName);
			}
		}

		// 2. Validação física de cada parte no disco (existência, tamanho, CRC-32 e SHA-256)
		byte[] buffer = new byte[bufferSize];

		for (int i = 0; i < manifest.Parts.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var part = manifest.Parts[i];
			string partPath = Path.Combine(partsDirectory, part.FileName);

			if (!File.Exists(partPath))
			{
				throw new CombineValidationException(
					$"Parte ausente: '{part.FileName}' (índice {part.Index}) não foi encontrada no diretório '{partsDirectory}'.",
					CombineValidationFailureReason.MissingPart,
					part.Index,
					part.FileName);
			}

			var fileInfo = new FileInfo(partPath);
			if (fileInfo.Length < part.Size)
			{
				throw new CombineValidationException(
					$"Parte truncada: '{part.FileName}' (índice {part.Index}) possui tamanho de {fileInfo.Length} bytes, menor que os {part.Size} bytes esperados.",
					CombineValidationFailureReason.TruncatedPart,
					part.Index,
					part.FileName);
			}

			if (fileInfo.Length > part.Size)
			{
				throw new CombineValidationException(
					$"Parte alterada: '{part.FileName}' (índice {part.Index}) possui tamanho de {fileInfo.Length} bytes, maior que os {part.Size} bytes esperados.",
					CombineValidationFailureReason.CorruptedPart,
					part.Index,
					part.FileName);
			}

			var partCrc = new Crc32();
			using var partSha = (options?.VerifySha256 == true && !string.IsNullOrWhiteSpace(part.Sha256))
				? IncrementalHash.CreateHash(HashAlgorithmName.SHA256)
				: null;

			await using (var partStream = new FileStream(
				partPath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				bufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan))
			{
				int bytesRead;
				while ((bytesRead = await partStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
				{
					var span = new ReadOnlySpan<byte>(buffer, 0, bytesRead);
					partCrc.Append(span);
					partSha?.AppendData(buffer, 0, bytesRead);
				}
			}

			var partCrcBytes = partCrc.GetCurrentHash();
			Array.Reverse(partCrcBytes);
			string actualCrcHex = Convert.ToHexString(partCrcBytes);

			if (!string.Equals(actualCrcHex, part.Crc32, StringComparison.OrdinalIgnoreCase))
			{
				throw new CombineValidationException(
					$"Parte alterada: '{part.FileName}' (índice {part.Index}) possui CRC-32 '{actualCrcHex}', divergindo do esperado '{part.Crc32}'.",
					CombineValidationFailureReason.CorruptedPart,
					part.Index,
					part.FileName);
			}

			if (partSha != null && !string.IsNullOrWhiteSpace(part.Sha256))
			{
				string actualShaHex = Convert.ToHexStringLower(partSha.GetHashAndReset());
				if (!string.Equals(actualShaHex, part.Sha256, StringComparison.OrdinalIgnoreCase))
				{
					throw new CombineValidationException(
						$"Parte alterada: '{part.FileName}' (índice {part.Index}) possui SHA-256 '{actualShaHex}', divergindo do esperado '{part.Sha256}'.",
						CombineValidationFailureReason.CorruptedPart,
						part.Index,
						part.FileName);
				}
			}
		}
	}

	/// <summary>
	/// Recombina partes de arquivo a partir de um arquivo de manifesto existente.
	/// </summary>
	public static async Task<CombineResult> CombineAsync(
		string manifestFilePath,
		CombineOptions? options = null,
		IProgress<CombineProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(manifestFilePath))
			throw new ArgumentException("O caminho do arquivo de manifesto não pode ser nulo ou vazio.", nameof(manifestFilePath));

		if (!File.Exists(manifestFilePath))
			throw new FileNotFoundException("Arquivo de manifesto não encontrado.", manifestFilePath);

		var manifest = await SplitManifest.LoadAsync(manifestFilePath, cancellationToken);
		if (manifest is null)
			throw new InvalidDataException($"Falha ao ler ou desserializar o manifesto em '{manifestFilePath}'.");

		string partsDirectory = Path.GetDirectoryName(manifestFilePath) ?? Directory.GetCurrentDirectory();
		return await CombineAsync(manifest, partsDirectory, options, progress, cancellationToken);
	}

	/// <summary>
	/// Valida e recombina partes de arquivo em um único arquivo de destino com garantia atômica e conferência estrita.
	/// Monta em arquivo temporário (.tmp) e promove apenas após validação de integridade do total.
	/// </summary>
	public static async Task<CombineResult> CombineAsync(
		SplitManifest manifest,
		string partsDirectory,
		CombineOptions? options = null,
		IProgress<CombineProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		if (manifest is null)
			throw new ArgumentNullException(nameof(manifest));

		if (string.IsNullOrWhiteSpace(partsDirectory))
			throw new ArgumentException("O diretório das partes não pode ser nulo ou vazio.", nameof(partsDirectory));

		options ??= new CombineOptions();

		string destDir = string.IsNullOrWhiteSpace(options.DestinationDirectory)
			? partsDirectory
			: options.DestinationDirectory;

		Directory.CreateDirectory(destDir);

		string targetFileName = string.IsNullOrWhiteSpace(options.DestinationFileName)
			? manifest.OriginalFileName
			: options.DestinationFileName;

		if (string.IsNullOrWhiteSpace(targetFileName))
			throw new ArgumentException("O nome do arquivo de destino não pode ser vazio.", nameof(options));

		EnsureSafeFileName(targetFileName, null);

		string targetPath = Path.Combine(destDir, targetFileName);

		// Conferência prévia de colisão quando política for Recusar
		if (File.Exists(targetPath) && options.CollisionPolicy == CombineCollisionPolicy.Recusar)
		{
			throw new IOException($"O arquivo de destino '{targetPath}' já existe e a política de colisão está configurada como Recusar.");
		}

		// Validação estrita de todas as partes ANTES de montar
		await ValidatePartsAsync(manifest, partsDirectory, options, cancellationToken);

		string? tempFilePath = null;

		try
		{
			// Montagem atômica em arquivo .tmp no diretório de destino
			tempFilePath = Path.Combine(destDir, $"{targetFileName}.{Guid.NewGuid():N}.tmp");

			var totalCrc = new Crc32();
			using var totalSha = (!string.IsNullOrWhiteSpace(manifest.TotalSha256) || options.VerifySha256)
				? IncrementalHash.CreateHash(HashAlgorithmName.SHA256)
				: null;

			long totalBytesWritten = 0;
			byte[] buffer = new byte[options.BufferSize];

			Stream CreateDestStream()
			{
				if (options.DestinationStreamFactory != null)
				{
					return options.DestinationStreamFactory(
						tempFilePath,
						FileMode.CreateNew,
						FileAccess.Write,
						FileShare.None,
						options.BufferSize,
						FileOptions.Asynchronous);
				}

				return new FileStream(
					tempFilePath,
					FileMode.CreateNew,
					FileAccess.Write,
					FileShare.None,
					options.BufferSize,
					FileOptions.Asynchronous);
			}

			await using (var destStream = CreateDestStream())
			{
				for (int i = 0; i < manifest.Parts.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var part = manifest.Parts[i];
					string partPath = Path.Combine(partsDirectory, part.FileName);

					await using (var partStream = new FileStream(
						partPath,
						FileMode.Open,
						FileAccess.Read,
						FileShare.Read,
						options.BufferSize,
						FileOptions.Asynchronous | FileOptions.SequentialScan))
					{
						int bytesRead;
						while ((bytesRead = await partStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
						{
							await destStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

							var span = new ReadOnlySpan<byte>(buffer, 0, bytesRead);
							totalCrc.Append(span);
							totalSha?.AppendData(buffer, 0, bytesRead);

							totalBytesWritten += bytesRead;
							progress?.Report(new CombineProgress(totalBytesWritten, manifest.TotalSize, part.Index, manifest.Parts.Count));
						}
					}
				}

				await destStream.FlushAsync(cancellationToken);
			}

			// Conferência final de integridade do total antes de promover
			if (totalBytesWritten != manifest.TotalSize)
			{
				throw new InvalidOperationException(
					$"O total de bytes gravados ({totalBytesWritten}) difere do tamanho total do manifesto ({manifest.TotalSize}).");
			}

			var totalCrcBytes = totalCrc.GetCurrentHash();
			Array.Reverse(totalCrcBytes);
			string actualTotalCrcHex = Convert.ToHexString(totalCrcBytes);

			if (!string.Equals(actualTotalCrcHex, manifest.TotalCrc32, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(
					$"O CRC-32 do arquivo total montado ('{actualTotalCrcHex}') não confere com o manifesto ('{manifest.TotalCrc32}').");
			}

			string? actualTotalShaHex = null;
			if (totalSha != null && !string.IsNullOrWhiteSpace(manifest.TotalSha256))
			{
				actualTotalShaHex = Convert.ToHexStringLower(totalSha.GetHashAndReset());
				if (!string.Equals(actualTotalShaHex, manifest.TotalSha256, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException(
						$"O SHA-256 do arquivo total montado ('{actualTotalShaHex}') não confere com o manifesto ('{manifest.TotalSha256}').");
				}
			}
			else if (totalSha != null)
			{
				actualTotalShaHex = Convert.ToHexStringLower(totalSha.GetHashAndReset());
			}

			// Resolução de colisão e promoção do .tmp para o destino final
			string finalDestinationPath = targetPath;
			if (File.Exists(targetPath))
			{
				switch (options.CollisionPolicy)
				{
					case CombineCollisionPolicy.Recusar:
						throw new IOException($"O arquivo de destino '{targetPath}' já existe e a política de colisão está configurada como Recusar.");

					case CombineCollisionPolicy.Sobrescrever:
						File.Move(tempFilePath, targetPath, overwrite: true);
						finalDestinationPath = targetPath;
						tempFilePath = null; // promovido com sucesso
						break;

					case CombineCollisionPolicy.Renomear:
						finalDestinationPath = GetNonCollidingFilePath(destDir, targetFileName);
						File.Move(tempFilePath, finalDestinationPath, overwrite: false);
						tempFilePath = null; // promovido com sucesso
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(options), $"Política de colisão não suportada: {options.CollisionPolicy}");
				}
			}
			else
			{
				File.Move(tempFilePath, targetPath, overwrite: false);
				finalDestinationPath = targetPath;
				tempFilePath = null; // promovido com sucesso
			}

			return new CombineResult
			{
				DestinationFilePath = finalDestinationPath,
				TotalBytesWritten = totalBytesWritten,
				TotalCrc32 = actualTotalCrcHex,
				TotalSha256 = actualTotalShaHex,
				TotalPartsCombined = manifest.Parts.Count
			};
		}
		catch
		{
			// Em caso de falha ou cancelamento (ex.: falta de espaço), exclui o arquivo .tmp
			if (tempFilePath != null)
			{
				try
				{
					if (File.Exists(tempFilePath))
						File.Delete(tempFilePath);
				}
				catch
				{
					// Ignora falha de exclusão durante desbobinamento
				}
			}

			throw;
		}
	}

	/// <summary>
	/// Recusa nomes vindos do manifesto que saiam da pasta (caminho absoluto, "..", separadores):
	/// um manifesto adulterado não pode ler nem gravar fora das pastas escolhidas pelo usuário.
	/// </summary>
	private static void EnsureSafeFileName(string? fileName, int? partIndex)
	{
		bool unsafeName =
			string.IsNullOrWhiteSpace(fileName) ||
			fileName is "." or ".." ||
			!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) ||
			fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;

		if (unsafeName)
		{
			throw new CombineValidationException(
				$"Nome de arquivo inválido ou fora da pasta no manifesto: '{fileName}'.",
				CombineValidationFailureReason.InvalidManifest,
				partIndex,
				fileName);
		}
	}

	private static string GetNonCollidingFilePath(string directory, string fileName)
	{
		string baseName = Path.GetFileNameWithoutExtension(fileName);
		string ext = Path.GetExtension(fileName);
		int counter = 1;
		string candidatePath;
		do
		{
			string candidateFileName = $"{baseName} ({counter}){ext}";
			candidatePath = Path.Combine(directory, candidateFileName);
			counter++;
		} while (File.Exists(candidatePath));

		return candidatePath;
	}
}
