// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Helpers;

/// <summary>
/// Opções de configuração para o particionamento de arquivo (F060 M1).
/// </summary>
public sealed class SplitOptions
{
	/// <summary>
	/// Tamanho de cada parte em bytes. Padrão: 4 MB (4.194.304 bytes).
	/// </summary>
	public long PartSize { get; set; } = 4L * 1024 * 1024;

	/// <summary>
	/// Diretório de destino onde as partes e o manifesto serão gravados.
	/// Se for nulo ou vazio, utiliza a mesma pasta do arquivo de origem.
	/// </summary>
	public string? DestinationDirectory { get; set; }

	/// <summary>
	/// Indica se deve calcular resumo SHA-256 (além do CRC32 obrigatório).
	/// </summary>
	public bool CalculateSha256 { get; set; }

	/// <summary>
	/// Permite sobrescrever partes ou manifesto que já existem na pasta de destino.
	/// Padrão: <c>false</c> (recusa com <see cref="IOException"/> e não altera nada que já exista).
	/// </summary>
	public bool OverwriteExisting { get; set; }

	/// <summary>
	/// Tamanho do buffer de leitura e gravação em bytes. Padrão: 64 KB (65.536 bytes).
	/// </summary>
	public int BufferSize { get; set; } = 64 * 1024;
}

/// <summary>
/// Informações de progresso do particionamento.
/// </summary>
public readonly record struct SplitProgress(
	long TotalBytesRead,
	long TotalBytes,
	int CurrentPartIndex,
	int EstimatedTotalParts);

/// <summary>
/// Resultado da operação de divisão de arquivo.
/// </summary>
public sealed class SplitResult
{
	/// <summary>
	/// Manifesto gerado para o arquivo dividido.
	/// </summary>
	public required SplitManifest Manifest { get; init; }

	/// <summary>
	/// Caminho completo do arquivo de manifesto gerado.
	/// </summary>
	public required string ManifestFilePath { get; init; }

	/// <summary>
	/// Lista de caminhos completos dos arquivos de parte gerados.
	/// </summary>
	public required IReadOnlyList<string> PartFilePaths { get; init; }

	/// <summary>
	/// Total de bytes lidos do arquivo original.
	/// </summary>
	public required long TotalBytesRead { get; init; }

	/// <summary>
	/// Quantidade total de partes geradas.
	/// </summary>
	public required int TotalParts { get; init; }
}

/// <summary>
/// Motor de divisão de arquivos grandes em partes de tamanho exato com manifesto e integridade (F060 M1).
/// </summary>
public static class SplitEngine
{
	/// <summary>
	/// Retorna o nome padrão da parte numerada em sequência (ex.: <c>arquivo.001</c>).
	/// </summary>
	public static string GetPartFileName(string originalFileName, int partIndex)
	{
		return $"{originalFileName}.{partIndex:D3}";
	}

	/// <summary>
	/// Retorna o nome padrão do manifesto (ex.: <c>arquivo.manifest.json</c>).
	/// </summary>
	public static string GetManifestFileName(string originalFileName)
	{
		return $"{originalFileName}.manifest.json";
	}

	/// <summary>
	/// Divide um arquivo em partes de tamanho fixo de forma assíncrona, calculando CRC32 e SHA-256 opcional.
	/// O arquivo original permanece inalterado.
	/// </summary>
	public static async Task<SplitResult> SplitAsync(
		string sourceFilePath,
		SplitOptions options,
		IProgress<SplitProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(sourceFilePath))
			throw new ArgumentException("O caminho do arquivo de origem não pode ser nulo ou vazio.", nameof(sourceFilePath));

		if (!File.Exists(sourceFilePath))
			throw new FileNotFoundException("Arquivo de origem não encontrado.", sourceFilePath);

		if (options is null)
			throw new ArgumentNullException(nameof(options));

		if (options.PartSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(options), "O tamanho da parte deve ser maior que zero.");

		if (options.BufferSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(options), "O tamanho do buffer deve ser maior que zero.");

		var sourceFileInfo = new FileInfo(sourceFilePath);
		long totalSize = sourceFileInfo.Length;
		string sourceFileName = sourceFileInfo.Name;

		var destDir = string.IsNullOrWhiteSpace(options.DestinationDirectory)
			? (sourceFileInfo.DirectoryName ?? Directory.GetCurrentDirectory())
			: options.DestinationDirectory;

		Directory.CreateDirectory(destDir);

		if (!options.OverwriteExisting)
		{
			string existingManifestPath = Path.Combine(destDir, GetManifestFileName(sourceFileName));
			if (File.Exists(existingManifestPath))
				throw new IOException($"O arquivo '{existingManifestPath}' já existe e não será sobrescrito.");
		}

		var partFilePaths = new List<string>();
		var parts = new List<SplitPartInfo>();

		try
		{
			// Caso especial: arquivo vazio (0 bytes)
			if (totalSize == 0)
			{
				string emptyPartFileName = GetPartFileName(sourceFileName, 1);
				string emptyPartFilePath = Path.Combine(destDir, emptyPartFileName);
				await using (var emptyStream = new FileStream(
					emptyPartFilePath,
					options.OverwriteExisting ? FileMode.Create : FileMode.CreateNew,
					FileAccess.Write,
					FileShare.None,
					options.BufferSize,
					FileOptions.Asynchronous))
				{
					// Arquivo vazio de 0 bytes criado
				}

				partFilePaths.Add(emptyPartFilePath);

				var emptyCrcInstance = new Crc32();
				var emptyCrcBytes = emptyCrcInstance.GetCurrentHash();
				Array.Reverse(emptyCrcBytes);
				string emptyCrcHex = Convert.ToHexString(emptyCrcBytes);

				string? emptyShaHex = null;
				if (options.CalculateSha256)
				{
					using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
					emptyShaHex = Convert.ToHexStringLower(sha.GetHashAndReset());
				}

				parts.Add(new SplitPartInfo
				{
					Index = 1,
					FileName = emptyPartFileName,
					Size = 0,
					Crc32 = emptyCrcHex,
					Sha256 = emptyShaHex
				});

				var emptyManifest = new SplitManifest
				{
					Version = 1,
					OriginalFileName = sourceFileName,
					TotalSize = 0,
					PartSize = options.PartSize,
					TotalCrc32 = emptyCrcHex,
					TotalSha256 = emptyShaHex,
					CreatedAtUtc = DateTimeOffset.UtcNow,
					Parts = parts
				};

				string emptyManifestFileName = GetManifestFileName(sourceFileName);
				string emptyManifestFilePath = Path.Combine(destDir, emptyManifestFileName);
				await emptyManifest.SaveAsync(emptyManifestFilePath, cancellationToken);

				progress?.Report(new SplitProgress(0, 0, 1, 1));

				return new SplitResult
				{
					Manifest = emptyManifest,
					ManifestFilePath = emptyManifestFilePath,
					PartFilePaths = partFilePaths,
					TotalBytesRead = 0,
					TotalParts = 1
				};
			}

			// Arquivo com dados: leitura em blocos com buffer fixo
			int estimatedParts = (int)((totalSize + options.PartSize - 1) / options.PartSize);
			byte[] buffer = new byte[options.BufferSize];

			var totalCrc = new Crc32();
			using var totalSha = options.CalculateSha256 ? IncrementalHash.CreateHash(HashAlgorithmName.SHA256) : null;

			long totalBytesRead = 0;
			int partIndex = 1;

			await using (var sourceStream = new FileStream(
				sourceFilePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				options.BufferSize,
				FileOptions.Asynchronous | FileOptions.SequentialScan))
			{
				while (totalBytesRead < totalSize)
				{
					cancellationToken.ThrowIfCancellationRequested();

					long currentPartTargetSize = Math.Min(options.PartSize, totalSize - totalBytesRead);
					string partFileName = GetPartFileName(sourceFileName, partIndex);
					string partFilePath = Path.Combine(destDir, partFileName);
					var partCrc = new Crc32();
					using var partSha = options.CalculateSha256 ? IncrementalHash.CreateHash(HashAlgorithmName.SHA256) : null;

					long partBytesWritten = 0;
					await using (var partStream = new FileStream(
						partFilePath,
						options.OverwriteExisting ? FileMode.Create : FileMode.CreateNew,
						FileAccess.Write,
						FileShare.None,
						options.BufferSize,
						FileOptions.Asynchronous))
					{
						partFilePaths.Add(partFilePath);

						while (partBytesWritten < currentPartTargetSize)
						{
							cancellationToken.ThrowIfCancellationRequested();

							int bytesToRead = (int)Math.Min((long)buffer.Length, currentPartTargetSize - partBytesWritten);
							int read = await sourceStream.ReadAsync(buffer.AsMemory(0, bytesToRead), cancellationToken);
							if (read == 0)
								break;

							await partStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);

							var readSpan = new ReadOnlySpan<byte>(buffer, 0, read);
							partCrc.Append(readSpan);
							totalCrc.Append(readSpan);

							partSha?.AppendData(buffer, 0, read);
							totalSha?.AppendData(buffer, 0, read);

							partBytesWritten += read;
							totalBytesRead += read;

							progress?.Report(new SplitProgress(totalBytesRead, totalSize, partIndex, estimatedParts));
						}
					}

					var partCrcBytes = partCrc.GetCurrentHash();
					Array.Reverse(partCrcBytes);
					string partCrcHex = Convert.ToHexString(partCrcBytes);
					string? partShaHex = partSha != null ? Convert.ToHexStringLower(partSha.GetHashAndReset()) : null;

					parts.Add(new SplitPartInfo
					{
						Index = partIndex,
						FileName = partFileName,
						Size = partBytesWritten,
						Crc32 = partCrcHex,
						Sha256 = partShaHex
					});

					partIndex++;
				}
			}

			var totalCrcBytes = totalCrc.GetCurrentHash();
			Array.Reverse(totalCrcBytes);
			string totalCrcHex = Convert.ToHexString(totalCrcBytes);
			string? totalShaHex = totalSha != null ? Convert.ToHexStringLower(totalSha.GetHashAndReset()) : null;

			var manifest = new SplitManifest
			{
				Version = 1,
				OriginalFileName = sourceFileName,
				TotalSize = totalBytesRead,
				PartSize = options.PartSize,
				TotalCrc32 = totalCrcHex,
				TotalSha256 = totalShaHex,
				CreatedAtUtc = DateTimeOffset.UtcNow,
				Parts = parts
			};

			string manifestFileName = GetManifestFileName(sourceFileName);
			string manifestFilePath = Path.Combine(destDir, manifestFileName);
			await manifest.SaveAsync(manifestFilePath, cancellationToken);

			return new SplitResult
			{
				Manifest = manifest,
				ManifestFilePath = manifestFilePath,
				PartFilePaths = partFilePaths,
				TotalBytesRead = totalBytesRead,
				TotalParts = parts.Count
			};
		}
		catch
		{
			// Limpa partes incompletas e manifesto em caso de falha/cancelamento
			foreach (var path in partFilePaths)
			{
				try
				{
					if (File.Exists(path))
						File.Delete(path);
				}
				catch
				{
					// Ignora falha de limpeza durante desbobinamento
				}
			}

			try
			{
				string manifestPath = Path.Combine(destDir, GetManifestFileName(sourceFileName));
				if (File.Exists(manifestPath))
					File.Delete(manifestPath);
			}
			catch
			{
				// Ignora falha de limpeza durante desbobinamento
			}

			throw;
		}
	}
}
