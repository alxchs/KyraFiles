// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class SplitEngineTests
{
	private string _guid = null!;
	private string _tempRoot = null!;

	[TestInitialize]
	public void Initialize()
	{
		_guid = Guid.NewGuid().ToString("N");
		_tempRoot = Path.Combine(Path.GetTempPath(), "agy", _guid, "desenv", "utils", "KyraFiles-f060-m1", "Files");
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
	public async Task M1_AC1_Dividir_10MB_Partes4MB_Gera3Partes_Manifesto_E_OriginalIntacto()
	{
		string originalPath = Path.Combine(_tempRoot, "dados10mb.bin");
		long fileSize = 10L * 1024 * 1024; // 10 MB = 10.485.760 bytes
		long partSize = 4L * 1024 * 1024;  // 4 MB = 4.194.304 bytes

		await CreatePatternFileAsync(originalPath, fileSize);
		string hashBefore = await ComputeFileSha256Async(originalPath);

		var options = new SplitOptions
		{
			PartSize = partSize,
			DestinationDirectory = _tempRoot,
			CalculateSha256 = true
		};

		var result = await SplitEngine.SplitAsync(originalPath, options);

		// Conferir que o arquivo original permaneceu intacto
		string hashAfter = await ComputeFileSha256Async(originalPath);
		Assert.AreEqual(hashBefore, hashAfter, "O arquivo original não pode ser modificado durante ou após a divisão.");

		// Conferir quantidade e nomes das partes
		Assert.AreEqual(3, result.TotalParts);
		Assert.AreEqual(3, result.PartFilePaths.Count);

		Assert.AreEqual("dados10mb.bin.001", Path.GetFileName(result.PartFilePaths[0]));
		Assert.AreEqual("dados10mb.bin.002", Path.GetFileName(result.PartFilePaths[1]));
		Assert.AreEqual("dados10mb.bin.003", Path.GetFileName(result.PartFilePaths[2]));

		// Conferir tamanhos exatos das partes (4 MB, 4 MB, 2 MB)
		Assert.AreEqual(4L * 1024 * 1024, new FileInfo(result.PartFilePaths[0]).Length);
		Assert.AreEqual(4L * 1024 * 1024, new FileInfo(result.PartFilePaths[1]).Length);
		Assert.AreEqual(2L * 1024 * 1024, new FileInfo(result.PartFilePaths[2]).Length);

		// Conferir arquivo de manifesto
		Assert.IsTrue(File.Exists(result.ManifestFilePath));
		Assert.AreEqual("dados10mb.bin.manifest.json", Path.GetFileName(result.ManifestFilePath));

		var loadedManifest = await SplitManifest.LoadAsync(result.ManifestFilePath);
		Assert.IsNotNull(loadedManifest);
		Assert.AreEqual(1, loadedManifest.Version);
		Assert.AreEqual("dados10mb.bin", loadedManifest.OriginalFileName);
		Assert.AreEqual(fileSize, loadedManifest.TotalSize);
		Assert.AreEqual(partSize, loadedManifest.PartSize);
		Assert.AreEqual(3, loadedManifest.Parts.Count);
		Assert.IsFalse(string.IsNullOrWhiteSpace(loadedManifest.TotalCrc32));
		Assert.AreEqual(hashBefore, loadedManifest.TotalSha256);

		Assert.AreEqual("dados10mb.bin.001", loadedManifest.Parts[0].FileName);
		Assert.AreEqual(4L * 1024 * 1024, loadedManifest.Parts[0].Size);
		Assert.IsFalse(string.IsNullOrWhiteSpace(loadedManifest.Parts[0].Crc32));

		Assert.AreEqual("dados10mb.bin.002", loadedManifest.Parts[1].FileName);
		Assert.AreEqual(4L * 1024 * 1024, loadedManifest.Parts[1].Size);

		Assert.AreEqual("dados10mb.bin.003", loadedManifest.Parts[2].FileName);
		Assert.AreEqual(2L * 1024 * 1024, loadedManifest.Parts[2].Size);
	}

	[TestMethod]
	public async Task M1_AC4_Dividir_ArquivoVazio_Gera1ParteVazia_E_OriginalIntacto()
	{
		string originalPath = Path.Combine(_tempRoot, "vazio.bin");
		await CreatePatternFileAsync(originalPath, 0);
		string hashBefore = await ComputeFileSha256Async(originalPath);

		var options = new SplitOptions
		{
			PartSize = 4L * 1024 * 1024,
			DestinationDirectory = _tempRoot,
			CalculateSha256 = true
		};

		var result = await SplitEngine.SplitAsync(originalPath, options);

		string hashAfter = await ComputeFileSha256Async(originalPath);
		Assert.AreEqual(hashBefore, hashAfter);

		Assert.AreEqual(1, result.TotalParts);
		Assert.AreEqual(1, result.PartFilePaths.Count);
		Assert.AreEqual("vazio.bin.001", Path.GetFileName(result.PartFilePaths[0]));
		Assert.AreEqual(0L, new FileInfo(result.PartFilePaths[0]).Length);

		Assert.IsTrue(File.Exists(result.ManifestFilePath));
		var loadedManifest = await SplitManifest.LoadAsync(result.ManifestFilePath);
		Assert.IsNotNull(loadedManifest);
		Assert.AreEqual(0L, loadedManifest.TotalSize);
		Assert.AreEqual(1, loadedManifest.Parts.Count);
		Assert.AreEqual(0L, loadedManifest.Parts[0].Size);
		Assert.AreEqual("00000000", loadedManifest.TotalCrc32);
	}

	[TestMethod]
	public async Task M1_AC4_Dividir_MenorQueUmaParte_Gera1ParteUnica_E_OriginalIntacto()
	{
		string originalPath = Path.Combine(_tempRoot, "menor.bin");
		long fileSize = 2L * 1024 * 1024; // 2 MB < 4 MB
		long partSize = 4L * 1024 * 1024;

		await CreatePatternFileAsync(originalPath, fileSize);
		string hashBefore = await ComputeFileSha256Async(originalPath);

		var options = new SplitOptions
		{
			PartSize = partSize,
			DestinationDirectory = _tempRoot,
			CalculateSha256 = true
		};

		var result = await SplitEngine.SplitAsync(originalPath, options);

		string hashAfter = await ComputeFileSha256Async(originalPath);
		Assert.AreEqual(hashBefore, hashAfter);

		Assert.AreEqual(1, result.TotalParts);
		Assert.AreEqual(1, result.PartFilePaths.Count);
		Assert.AreEqual("menor.bin.001", Path.GetFileName(result.PartFilePaths[0]));
		Assert.AreEqual(fileSize, new FileInfo(result.PartFilePaths[0]).Length);

		var loadedManifest = await SplitManifest.LoadAsync(result.ManifestFilePath);
		Assert.IsNotNull(loadedManifest);
		Assert.AreEqual(fileSize, loadedManifest.TotalSize);
		Assert.AreEqual(1, loadedManifest.Parts.Count);
		Assert.AreEqual(fileSize, loadedManifest.Parts[0].Size);
	}

	[TestMethod]
	public async Task M1_AC4_Dividir_MultiploExato_GeraPartesExatasSemParteExtra_E_OriginalIntacto()
	{
		string originalPath = Path.Combine(_tempRoot, "multiplo.bin");
		long fileSize = 8L * 1024 * 1024; // 8 MB = 2 * 4 MB exatamente
		long partSize = 4L * 1024 * 1024;

		await CreatePatternFileAsync(originalPath, fileSize);
		string hashBefore = await ComputeFileSha256Async(originalPath);

		var options = new SplitOptions
		{
			PartSize = partSize,
			DestinationDirectory = _tempRoot,
			CalculateSha256 = true
		};

		var result = await SplitEngine.SplitAsync(originalPath, options);

		string hashAfter = await ComputeFileSha256Async(originalPath);
		Assert.AreEqual(hashBefore, hashAfter);

		Assert.AreEqual(2, result.TotalParts);
		Assert.AreEqual(2, result.PartFilePaths.Count);
		Assert.AreEqual("multiplo.bin.001", Path.GetFileName(result.PartFilePaths[0]));
		Assert.AreEqual("multiplo.bin.002", Path.GetFileName(result.PartFilePaths[1]));

		Assert.AreEqual(4L * 1024 * 1024, new FileInfo(result.PartFilePaths[0]).Length);
		Assert.AreEqual(4L * 1024 * 1024, new FileInfo(result.PartFilePaths[1]).Length);

		var loadedManifest = await SplitManifest.LoadAsync(result.ManifestFilePath);
		Assert.IsNotNull(loadedManifest);
		Assert.AreEqual(fileSize, loadedManifest.TotalSize);
		Assert.AreEqual(2, loadedManifest.Parts.Count);
		Assert.AreEqual(4L * 1024 * 1024, loadedManifest.Parts[0].Size);
		Assert.AreEqual(4L * 1024 * 1024, loadedManifest.Parts[1].Size);
	}

	[TestMethod]
	public async Task M1_AC1_Dividir_Crc32_BateComVetorPadraoEComAsPartesConcatenadas()
	{
		// Vetor padrão da indústria: CRC-32 de "123456789" é CBF43926.
		string originalPath = Path.Combine(_tempRoot, "vetor.bin");
		byte[] content = System.Text.Encoding.ASCII.GetBytes("123456789");
		await File.WriteAllBytesAsync(originalPath, content);

		var result = await SplitEngine.SplitAsync(originalPath, new SplitOptions
		{
			PartSize = 4,
			BufferSize = 3, // força várias leituras por parte
			DestinationDirectory = _tempRoot
		});

		Assert.AreEqual(3, result.TotalParts); // "1234", "5678", "9"
		Assert.AreEqual("CBF43926", result.Manifest.TotalCrc32);

		// Cada CRC de parte confere com o cálculo independente sobre o arquivo gravado.
		byte[] joined = Array.Empty<byte>();
		for (int i = 0; i < result.PartFilePaths.Count; i++)
		{
			byte[] partBytes = await File.ReadAllBytesAsync(result.PartFilePaths[i]);
			uint expected = System.IO.Hashing.Crc32.HashToUInt32(partBytes);
			Assert.AreEqual(expected.ToString("X8"), result.Manifest.Parts[i].Crc32);
			joined = [.. joined, .. partBytes];
		}

		// As partes juntas, na ordem, reproduzem o original byte a byte.
		CollectionAssert.AreEqual(content, joined);
	}

	[TestMethod]
	public async Task M1_AC6_Dividir_ParteOuManifestoJaExistente_Recusa_SemApagarNemAlterarNadaDoUsuario()
	{
		string originalPath = Path.Combine(_tempRoot, "dados.bin");
		await File.WriteAllBytesAsync(originalPath, new byte[10]);

		// Arquivo alheio com o nome da 2ª parte, na pasta de destino.
		string foreignPart = Path.Combine(_tempRoot, "dados.bin.002");
		byte[] foreignContent = System.Text.Encoding.ASCII.GetBytes("nao e meu");
		await File.WriteAllBytesAsync(foreignPart, foreignContent);

		var ex = await AssertThrowsAsync<IOException>(() => SplitEngine.SplitAsync(
			originalPath,
			new SplitOptions { PartSize = 4, DestinationDirectory = _tempRoot }));
		Assert.IsNotNull(ex);

		CollectionAssert.AreEqual(foreignContent, await File.ReadAllBytesAsync(foreignPart), "O arquivo alheio não pode ser alterado nem apagado.");
		Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "dados.bin.001")), "As partes criadas antes da recusa devem ser removidas.");
		Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "dados.bin.manifest.json")), "Nenhum manifesto deve ficar.");

		// Manifesto já existente também recusa antes de escrever qualquer parte.
		File.Delete(foreignPart);
		string foreignManifest = Path.Combine(_tempRoot, "dados.bin.manifest.json");
		await File.WriteAllBytesAsync(foreignManifest, foreignContent);
		await AssertThrowsAsync<IOException>(() => SplitEngine.SplitAsync(
			originalPath,
			new SplitOptions { PartSize = 4, DestinationDirectory = _tempRoot }));
		CollectionAssert.AreEqual(foreignContent, await File.ReadAllBytesAsync(foreignManifest));
		Assert.IsFalse(File.Exists(Path.Combine(_tempRoot, "dados.bin.001")));

		// Com a opção explícita, sobrescrever é permitido.
		var result = await SplitEngine.SplitAsync(
			originalPath,
			new SplitOptions { PartSize = 4, DestinationDirectory = _tempRoot, OverwriteExisting = true });
		Assert.AreEqual(3, result.TotalParts);
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

		Assert.Fail($"Esperava exceção do tipo {typeof(TException).Name}, mas nenhuma foi lançada.");
		return null!;
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
}
