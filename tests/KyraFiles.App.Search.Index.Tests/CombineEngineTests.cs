// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class CombineEngineTests
{
	private string _guid = null!;
	private string _tempRoot = null!;

	[TestInitialize]
	public void Initialize()
	{
		_guid = Guid.NewGuid().ToString("N");
		_tempRoot = Path.Combine(Path.GetTempPath(), "agy", _guid, "desenv", "utils", "FilesApp-f060-m1", "Files");
		Directory.CreateDirectory(_tempRoot);
	}

	[TestCleanup]
	public void Cleanup()
	{
		try
		{
			var guidRoot = Path.Combine(Path.GetTempPath(), "agy", _guid);
			if (Directory.Exists(guidRoot))
				Directory.Delete(guidRoot, recursive: true);
		}
		catch
		{
			// Limpeza de contingência ignorada
		}
	}

	[TestMethod]
	public async Task M1_AC2_Juntar_3PartesEManifesto_ReconstroiArquivoIdenticoComSha256Independente()
	{
		// M1-AC-2: as 3 partes e o arquivo de conferência | junto | o resultado tem os mesmos bytes do original (conferido por resumo)
		string sourceDir = Path.Combine(_tempRoot, "split_source");
		string destDir = Path.Combine(_tempRoot, "combine_dest");
		Directory.CreateDirectory(sourceDir);
		Directory.CreateDirectory(destDir);

		string originalPath = Path.Combine(sourceDir, "dados10mb.bin");
		long fileSize = 10L * 1024 * 1024; // 10 MB
		long partSize = 4L * 1024 * 1024;  // 4 MB

		await CreatePatternFileAsync(originalPath, fileSize);
		string originalSha256 = await ComputeFileSha256Async(originalPath);

		// Divide em 3 partes (4 MB, 4 MB, 2 MB)
		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = partSize,
			DestinationDirectory = sourceDir,
			CalculateSha256 = true
		});

		Assert.AreEqual(3, splitResult.TotalParts);

		// Recombina para a pasta de destino
		var combineResult = await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
		{
			DestinationDirectory = destDir,
			CollisionPolicy = CombineCollisionPolicy.Recusar
		});

		Assert.IsTrue(File.Exists(combineResult.DestinationFilePath));
		Assert.AreEqual("dados10mb.bin", Path.GetFileName(combineResult.DestinationFilePath));
		Assert.AreEqual(fileSize, combineResult.TotalBytesWritten);
		Assert.AreEqual(splitResult.Manifest.TotalCrc32, combineResult.TotalCrc32);

		// Conferência independente por SHA-256
		string combinedSha256 = await ComputeFileSha256Async(combineResult.DestinationFilePath);
		Assert.AreEqual(originalSha256, combinedSha256, "O arquivo recombinado deve possuir SHA-256 estritamente idêntico ao original.");

		// Não deve restar nenhum arquivo .tmp na pasta de destino
		var tmpFiles = Directory.GetFiles(destDir, "*.tmp");
		Assert.AreEqual(0, tmpFiles.Length, "Nenhum arquivo temporário .tmp deve restar após recombinação bem-sucedida.");
	}

	[TestMethod]
	public async Task M1_AC2_Juntar_VetorPadrao_123456789_ReconstroiConteudoEConfereCrc32ESha256()
	{
		// Vetor padrão da indústria (Lição 22): "123456789" possui CRC-32 CBF43926 e SHA-256 conhecido.
		string sourceDir = Path.Combine(_tempRoot, "vetor_source");
		string destDir = Path.Combine(_tempRoot, "vetor_dest");
		Directory.CreateDirectory(sourceDir);
		Directory.CreateDirectory(destDir);

		string originalPath = Path.Combine(sourceDir, "vetor.bin");
		byte[] content = System.Text.Encoding.ASCII.GetBytes("123456789");
		await File.WriteAllBytesAsync(originalPath, content);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 4,
			BufferSize = 3,
			DestinationDirectory = sourceDir,
			CalculateSha256 = true
		});

		Assert.AreEqual("CBF43926", splitResult.Manifest.TotalCrc32);

		var combineResult = await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
		{
			DestinationDirectory = destDir
		});

		Assert.AreEqual("CBF43926", combineResult.TotalCrc32);

		byte[] combinedBytes = await File.ReadAllBytesAsync(combineResult.DestinationFilePath);
		CollectionAssert.AreEqual(content, combinedBytes, "O conteúdo recombinado deve ser byte a byte idêntico a '123456789'.");

		string expectedSha256 = "15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225";
		string actualSha256 = await ComputeFileSha256Async(combineResult.DestinationFilePath);
		Assert.AreEqual(expectedSha256, actualSha256, "O SHA-256 do vetor padrão deve bater com o vetor conhecido.");
	}

	[TestMethod]
	public async Task M1_AC3_Juntar_ParteAusente_RecusaDizQualParteEPorQue_SemCriarArquivoFinalNemTmp()
	{
		// M1-AC-3: parte ausente | recusa, diz qual parte e por quê, e não cria o arquivo final
		string dir = Path.Combine(_tempRoot, "parte_ausente");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "arquivo.bin");
		await CreatePatternFileAsync(originalPath, 6 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 2 * 1024 * 1024,
			DestinationDirectory = dir
		});

		// Apaga a parte 2 propositalmente
		string part2Path = splitResult.PartFilePaths[1];
		File.Delete(part2Path);

		string destCombinedPath = Path.Combine(dir, "arquivo.bin.combinado");

		var ex = await AssertThrowsAsync<CombineValidationException>(async () =>
		{
			await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
			{
				DestinationFileName = "arquivo.bin.combinado"
			});
		});

		Assert.AreEqual(CombineValidationFailureReason.MissingPart, ex.Reason);
		Assert.AreEqual(2, ex.PartIndex);
		StringAssert.Contains(ex.Message, "arquivo.bin.002");
		StringAssert.Contains(ex.Message, "Parte ausente");

		Assert.IsFalse(File.Exists(destCombinedPath), "O arquivo final não deve ser criado quando falta uma parte.");
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length, "Nenhum arquivo .tmp deve ser deixado em disco.");
	}

	[TestMethod]
	public async Task M1_AC3_Juntar_ParteRepetida_RecusaDizQualParteEPorQue_SemCriarArquivoFinalNemTmp()
	{
		// M1-AC-3: parte repetida / duplicada | recusa, diz qual parte e por quê
		string dir = Path.Combine(_tempRoot, "parte_duplicada");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "duplicada.bin");
		await CreatePatternFileAsync(originalPath, 4 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 2 * 1024 * 1024,
			DestinationDirectory = dir
		});

		// Corrompe o manifesto duplicando a primeira parte na lista
		var manifest = await SplitManifest.LoadAsync(splitResult.ManifestFilePath);
		Assert.IsNotNull(manifest);
		manifest.Parts.Add(new SplitPartInfo
		{
			Index = 1,
			FileName = manifest.Parts[0].FileName,
			Size = manifest.Parts[0].Size,
			Crc32 = manifest.Parts[0].Crc32
		});

		string corruptedManifestPath = Path.Combine(dir, "corrupted.manifest.json");
		await manifest.SaveAsync(corruptedManifestPath);

		var ex = await AssertThrowsAsync<CombineValidationException>(async () =>
		{
			await CombineEngine.CombineAsync(corruptedManifestPath, new CombineOptions
			{
				DestinationFileName = "resultado.bin"
			});
		});

		Assert.AreEqual(CombineValidationFailureReason.DuplicatePart, ex.Reason);
		Assert.AreEqual(1, ex.PartIndex);
		StringAssert.Contains(ex.Message, "duplicada");

		Assert.IsFalse(File.Exists(Path.Combine(dir, "resultado.bin")));
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
	}

	[TestMethod]
	public async Task M1_AC3_Juntar_ParteTruncada_RecusaDizQualParteEPorQue_SemCriarArquivoFinalNemTmp()
	{
		// M1-AC-3: parte truncada | recusa, diz qual parte e por quê
		string dir = Path.Combine(_tempRoot, "parte_truncada");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "truncada.bin");
		await CreatePatternFileAsync(originalPath, 6 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 2 * 1024 * 1024,
			DestinationDirectory = dir
		});

		// Trunca a parte 2 (grava apenas 1 KB no lugar de 2 MB)
		string part2Path = splitResult.PartFilePaths[1];
		await File.WriteAllBytesAsync(part2Path, new byte[1024]);

		var ex = await AssertThrowsAsync<CombineValidationException>(async () =>
		{
			await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
			{
				DestinationFileName = "resultado_truncada.bin"
			});
		});

		Assert.AreEqual(CombineValidationFailureReason.TruncatedPart, ex.Reason);
		Assert.AreEqual(2, ex.PartIndex);
		StringAssert.Contains(ex.Message, "truncada.bin.002");
		StringAssert.Contains(ex.Message, "truncada");

		Assert.IsFalse(File.Exists(Path.Combine(dir, "resultado_truncada.bin")));
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
	}

	[TestMethod]
	public async Task M1_AC3_Juntar_ParteAlterada_RecusaDizQualParteEPorQue_SemCriarArquivoFinalNemTmp()
	{
		// M1-AC-3: parte alterada (tamanho idêntico mas bytes corrompidos) | recusa, diz qual parte e por quê
		string dir = Path.Combine(_tempRoot, "parte_alterada");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "alterada.bin");
		await CreatePatternFileAsync(originalPath, 6 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 2 * 1024 * 1024,
			DestinationDirectory = dir
		});

		// Altera 1 byte da parte 2 mantendo o mesmo tamanho de 2 MB
		string part2Path = splitResult.PartFilePaths[1];
		await using (var fs = new FileStream(part2Path, FileMode.Open, FileAccess.ReadWrite))
		{
			fs.Seek(100, SeekOrigin.Begin);
			int b = fs.ReadByte();
			fs.Seek(100, SeekOrigin.Begin);
			fs.WriteByte((byte)(b ^ 0xFF));
		}

		var ex = await AssertThrowsAsync<CombineValidationException>(async () =>
		{
			await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
			{
				DestinationFileName = "resultado_alterada.bin"
			});
		});

		Assert.AreEqual(CombineValidationFailureReason.CorruptedPart, ex.Reason);
		Assert.AreEqual(2, ex.PartIndex);
		StringAssert.Contains(ex.Message, "alterada.bin.002");
		StringAssert.Contains(ex.Message, "CRC-32");

		Assert.IsFalse(File.Exists(Path.Combine(dir, "resultado_alterada.bin")));
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
	}

	[TestMethod]
	public async Task M1_AC3_Juntar_PartesForaDeOrdem_RecusaDizQualParteEPorQue_SemCriarArquivoFinalNemTmp()
	{
		// M1-AC-3: partes fora de ordem | recusa, diz qual parte e por quê
		string dir = Path.Combine(_tempRoot, "fora_de_ordem");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "ordem.bin");
		await CreatePatternFileAsync(originalPath, 6 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 2 * 1024 * 1024,
			DestinationDirectory = dir
		});

		var manifest = await SplitManifest.LoadAsync(splitResult.ManifestFilePath);
		Assert.IsNotNull(manifest);

		// Inverte ordem das partes no manifesto: parte 2 antes da parte 1
		(manifest.Parts[0], manifest.Parts[1]) = (manifest.Parts[1], manifest.Parts[0]);

		string outOfOrderManifestPath = Path.Combine(dir, "ordem.manifest.json");
		await manifest.SaveAsync(outOfOrderManifestPath);

		var ex = await AssertThrowsAsync<CombineValidationException>(async () =>
		{
			await CombineEngine.CombineAsync(outOfOrderManifestPath, new CombineOptions
			{
				DestinationFileName = "resultado_ordem.bin"
			});
		});

		Assert.AreEqual(CombineValidationFailureReason.OutOfOrder, ex.Reason);
		StringAssert.Contains(ex.Message, "fora de ordem");

		Assert.IsFalse(File.Exists(Path.Combine(dir, "resultado_ordem.bin")));
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
	}

	[TestMethod]
	public async Task M1_AC4_Juntar_ArquivoVazio_ReconstroiArquivoZeroBytesComCrcZero()
	{
		// M1-AC-4: arquivo vazio | recombina gerando arquivo de zero bytes com integridade conferida
		string dir = Path.Combine(_tempRoot, "vazio");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "vazio.bin");
		await File.WriteAllBytesAsync(originalPath, Array.Empty<byte>());

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 1024 * 1024,
			DestinationDirectory = dir
		});

		string destCombinedPath = Path.Combine(dir, "vazio_recombinado.bin");

		var combineResult = await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
		{
			DestinationFileName = "vazio_recombinado.bin"
		});

		Assert.IsTrue(File.Exists(destCombinedPath));
		Assert.AreEqual(0L, new FileInfo(destCombinedPath).Length);
		Assert.AreEqual("00000000", combineResult.TotalCrc32);
		Assert.AreEqual(1, combineResult.TotalPartsCombined);
	}

	[TestMethod]
	public async Task M1_AC5_Juntar_DestinoExistente_PoliticaRecusar_LancaExcecaoSemSobrescrever()
	{
		// M1-AC-5: destino existente com política Recusar | lança IOException e preserva destino original
		string dir = Path.Combine(_tempRoot, "colisao_recusar");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "dados.bin");
		await CreatePatternFileAsync(originalPath, 2 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 1024 * 1024,
			DestinationDirectory = dir
		});

		string existingDestFile = Path.Combine(dir, "alvo.bin");
		string preexistingContent = "CONTEUDO_PREEXISTENTE_PRESERVADO";
		await File.WriteAllTextAsync(existingDestFile, preexistingContent);

		await AssertThrowsAsync<IOException>(async () =>
		{
			await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
			{
				DestinationFileName = "alvo.bin",
				CollisionPolicy = CombineCollisionPolicy.Recusar
			});
		});

		// Conferir que o arquivo existente permaneceu rigorosamente intacto
		string currentContent = await File.ReadAllTextAsync(existingDestFile);
		Assert.AreEqual(preexistingContent, currentContent, "O arquivo existente não pode ser sobrescrito quando a política for Recusar.");
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length, "Nenhum arquivo .tmp deve restar.");
	}

	[TestMethod]
	public async Task M1_AC5_Juntar_DestinoExistente_PoliticaSobrescrever_SobrescreveComSucesso()
	{
		// M1-AC-5: destino existente com política Sobrescrever | substitui arquivo existente pelo combinado
		string dir = Path.Combine(_tempRoot, "colisao_sobrescrever");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "dados.bin");
		await CreatePatternFileAsync(originalPath, 2 * 1024 * 1024);
		string originalSha256 = await ComputeFileSha256Async(originalPath);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 1024 * 1024,
			DestinationDirectory = dir
		});

		string existingDestFile = Path.Combine(dir, "alvo.bin");
		await File.WriteAllTextAsync(existingDestFile, "CONTEUDO_ANTIGO_A_SER_SOBRESCRITO");

		var result = await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
		{
			DestinationFileName = "alvo.bin",
			CollisionPolicy = CombineCollisionPolicy.Sobrescrever
		});

		Assert.AreEqual(existingDestFile, result.DestinationFilePath);
		string resultSha256 = await ComputeFileSha256Async(existingDestFile);
		Assert.AreEqual(originalSha256, resultSha256, "O arquivo deve ter sido sobrescrito com o conteúdo integral combinado.");
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
	}

	[TestMethod]
	public async Task M1_AC5_Juntar_DestinoExistente_PoliticaRenomear_GeraArquivoComSufixoNumerico()
	{
		// M1-AC-5: destino existente com política Renomear | gera novo arquivo com sufixo e mantém o existente
		string dir = Path.Combine(_tempRoot, "colisao_renomear");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "relatorio.txt");
		await CreatePatternFileAsync(originalPath, 2 * 1024 * 1024);
		string originalSha256 = await ComputeFileSha256Async(originalPath);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 1024 * 1024,
			DestinationDirectory = dir
		});

		string existingDestFile = Path.Combine(dir, "saida.txt");
		string preexistingText = "TEXTO_ORIGINAL_INTACTO";
		await File.WriteAllTextAsync(existingDestFile, preexistingText);

		var result = await CombineEngine.CombineAsync(splitResult.ManifestFilePath, new CombineOptions
		{
			DestinationFileName = "saida.txt",
			CollisionPolicy = CombineCollisionPolicy.Renomear
		});

		string expectedRenamedFile = Path.Combine(dir, "saida (1).txt");
		Assert.AreEqual(expectedRenamedFile, result.DestinationFilePath);
		Assert.IsTrue(File.Exists(expectedRenamedFile), "O arquivo renomeado 'saida (1).txt' deve ter sido criado.");

		// Arquivo preexistente continua intacto
		string textStillThere = await File.ReadAllTextAsync(existingDestFile);
		Assert.AreEqual(preexistingText, textStillThere, "O arquivo preexistente deve permanecer inalterado.");

		// Arquivo renomeado possui o conteúdo correto
		string renamedSha256 = await ComputeFileSha256Async(expectedRenamedFile);
		Assert.AreEqual(originalSha256, renamedSha256);
		Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
	}

	[TestMethod]
	public async Task M1_AC6_Juntar_FaltaEspaco_SimuladoPorStreamQueFalha_NaoDeixaArquivoFinalNemTmp()
	{
		// M1-AC-6: falta de espaço simulada por stream que falha | não deixa arquivo final nem .tmp
		string dir = Path.Combine(_tempRoot, "falta_espaco");
		Directory.CreateDirectory(dir);

		string originalPath = Path.Combine(dir, "dados_espaco.bin");
		await CreatePatternFileAsync(originalPath, 4 * 1024 * 1024);

		var splitResult = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 1024 * 1024,
			DestinationDirectory = dir
		});

		string destinationFileName = "dados_final.bin";
		string destinationFilePath = Path.Combine(dir, destinationFileName);

		var options = new CombineOptions
		{
			DestinationDirectory = dir,
			DestinationFileName = destinationFileName,
			DestinationStreamFactory = (path, mode, access, share, bufferSize, fileOptions) =>
			{
				var realFs = new FileStream(path, mode, access, share, bufferSize, fileOptions);
				// Falha simulando disco cheio após 512 KB gravados
				return new FailingDiskFullStream(realFs, failAfterBytes: 512 * 1024);
			}
		};

		var ex = await AssertThrowsAsync<IOException>(async () =>
		{
			await CombineEngine.CombineAsync(splitResult.ManifestFilePath, options);
		});

		StringAssert.Contains(ex.Message, "not enough space");

		// Verifica que o arquivo final NÃO existe
		Assert.IsFalse(File.Exists(destinationFilePath), "O arquivo final 'dados_final.bin' não deve existir após falha de espaço.");

		// Verifica que nenhum arquivo .tmp foi deixado no diretório
		var tmpFiles = Directory.GetFiles(dir, "*.tmp");
		Assert.AreEqual(0, tmpFiles.Length, "Nenhum arquivo temporário (.tmp) deve permanecer em disco após interrupção por falta de espaço.");
	}

	private static async Task CreatePatternFileAsync(string filePath, long size)
	{
		byte[] pattern = new byte[64 * 1024];
		for (int i = 0; i < pattern.Length; i++)
			pattern[i] = (byte)(i % 251);

		await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
		long written = 0;
		while (written < size)
		{
			int toWrite = (int)Math.Min((long)pattern.Length, size - written);
			await fs.WriteAsync(pattern.AsMemory(0, toWrite));
			written += toWrite;
		}
	}

	private static async Task<string> ComputeFileSha256Async(string filePath)
	{
		await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var hashBytes = await SHA256.HashDataAsync(fs);
		return Convert.ToHexStringLower(hashBytes);
	}

	private sealed class FailingDiskFullStream : Stream
	{
		private readonly Stream _inner;
		private readonly long _failAfterBytes;
		private long _bytesWritten;

		public FailingDiskFullStream(Stream inner, long failAfterBytes)
		{
			_inner = inner;
			_failAfterBytes = failAfterBytes;
		}

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => _inner.CanSeek;
		public override bool CanWrite => _inner.CanWrite;
		public override long Length => _inner.Length;
		public override long Position
		{
			get => _inner.Position;
			set => _inner.Position = value;
		}

		public override void Flush() => _inner.Flush();
		public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

		public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _inner.ReadAsync(buffer, offset, count, cancellationToken);
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			=> _inner.ReadAsync(buffer, cancellationToken);

		public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
		public override void SetLength(long value) => _inner.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (_bytesWritten + count > _failAfterBytes)
			{
				throw new IOException("There is not enough space on the disk.");
			}
			_inner.Write(buffer, offset, count);
			_bytesWritten += count;
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (_bytesWritten + count > _failAfterBytes)
			{
				throw new IOException("There is not enough space on the disk.");
			}
			await _inner.WriteAsync(buffer, offset, count, cancellationToken);
			_bytesWritten += count;
		}

		public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			if (_bytesWritten + buffer.Length > _failAfterBytes)
			{
				throw new IOException("There is not enough space on the disk.");
			}
			await _inner.WriteAsync(buffer, cancellationToken);
			_bytesWritten += buffer.Length;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_inner.Dispose();
			base.Dispose(disposing);
		}

		public override async ValueTask DisposeAsync()
		{
			await _inner.DisposeAsync();
			await base.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task M1_AC3_Juntar_ManifestoAdulteradoComCaminhoFora_RecusaSemLerNemGravarForaDaPasta()
	{
		string partsDir = Path.Combine(_tempRoot, "adulterado_partes");
		string destDir = Path.Combine(_tempRoot, "adulterado_destino");
		Directory.CreateDirectory(partsDir);
		Directory.CreateDirectory(destDir);

		string outsideFile = Path.Combine(_tempRoot, "fora.bin");
		await File.WriteAllBytesAsync(outsideFile, new byte[] { 1, 2, 3 });

		// Parte apontando para fora da pasta das partes (caminho relativo e absoluto).
		foreach (string maliciousPartName in new[] { @"..\fora.bin", outsideFile })
		{
			var manifest = new SplitManifest
			{
				OriginalFileName = "ok.bin",
				TotalSize = 3,
				PartSize = 3,
				TotalCrc32 = "00000000",
				Parts = [new SplitPartInfo { Index = 1, FileName = maliciousPartName, Size = 3, Crc32 = "00000000" }]
			};

			var ex = await AssertThrowsAsync<CombineValidationException>(() =>
				CombineEngine.CombineAsync(manifest, partsDir, new CombineOptions { DestinationDirectory = destDir }));
			Assert.AreEqual(CombineValidationFailureReason.InvalidManifest, ex.Reason);
		}

		// Nome lógico do original apontando para fora da pasta de destino.
		string realPart = Path.Combine(partsDir, "ok.bin.001");
		await File.WriteAllBytesAsync(realPart, new byte[] { 1, 2, 3 });
		var traversal = new SplitManifest
		{
			OriginalFileName = @"..\escapou.bin",
			TotalSize = 3,
			PartSize = 3,
			TotalCrc32 = "00000000",
			Parts = [new SplitPartInfo { Index = 1, FileName = "ok.bin.001", Size = 3, Crc32 = "00000000" }]
		};

		var ex2 = await AssertThrowsAsync<CombineValidationException>(() =>
			CombineEngine.CombineAsync(traversal, partsDir, new CombineOptions { DestinationDirectory = destDir }));
		Assert.AreEqual(CombineValidationFailureReason.InvalidManifest, ex2.Reason);

		Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "escapou.bin")), "Nada pode ser gravado fora da pasta de destino.");
		Assert.AreEqual(0, Directory.GetFiles(destDir).Length, "A pasta de destino deve ficar vazia.");
	}

	private static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action) where TException : Exception
	{
		try
		{
			await action();
		}
		catch (TException ex)
		{
			return ex;
		}
		catch (Exception ex)
		{
			Assert.Fail($"Esperava exceção do tipo {typeof(TException).Name}, mas foi lançada {ex.GetType().Name}: {ex.Message}");
		}

		Assert.Fail($"Esperava exceção do tipo {typeof(TException).Name}, mas nenhuma exceção foi lançada.");
		return null!;
	}
}
