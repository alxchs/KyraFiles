// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Files.Shared.Helpers;

namespace SplitCombineHarness;

public static class Program
{
	public static async Task<int> Main(string[] args)
	{
		if (args.Length == 0)
		{
			PrintUsage();
			return 1;
		}

		string command = args[0].ToLowerInvariant();

		try
		{
			switch (command)
			{
				case "generate":
					return ExecuteGenerate(args);

				case "split":
					return await ExecuteSplitAsync(args);

				case "combine":
					return await ExecuteCombineAsync(args);

				case "verify-hash":
					return ExecuteVerifyHash(args);

				default:
					Console.Error.WriteLine($"Comando desconhecido: {command}");
					PrintUsage();
					return 1;
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"ERRO FATAL: {ex.GetType().Name}: {ex.Message}");
			Console.Error.WriteLine(ex.StackTrace);
			return 2;
		}
	}

	private static void PrintUsage()
	{
		Console.WriteLine("Uso:");
		Console.WriteLine("  SplitCombineHarness generate <caminhoArquivo> <tamanhoBytes> [seed]");
		Console.WriteLine("  SplitCombineHarness split <caminhoOrigem> <tamanhoParteBytes> <diretorioDestino>");
		Console.WriteLine("  SplitCombineHarness combine <caminhoManifesto> <diretorioDestino> <nomeDestino>");
		Console.WriteLine("  SplitCombineHarness verify-hash <arquivoOrigem> <arquivoRecombinado>");
	}

	private static int ExecuteGenerate(string[] args)
	{
		if (args.Length < 3)
		{
			Console.Error.WriteLine("generate requer: <caminhoArquivo> <tamanhoBytes> [seed]");
			return 1;
		}

		string path = args[1];
		if (!long.TryParse(args[2], out long totalBytes) || totalBytes < 0)
		{
			Console.Error.WriteLine("tamanhoBytes inválido.");
			return 1;
		}

		int seed = 42;
		if (args.Length > 3 && int.TryParse(args[3], out int customSeed))
		{
			seed = customSeed;
		}

		string? dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}

		var sw = Stopwatch.StartNew();
		const int bufferSize = 1024 * 1024; // 1 MB
		byte[] buffer = new byte[bufferSize];
		var rng = new Random(seed);

		using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.SequentialScan))
		{
			long remaining = totalBytes;
			while (remaining > 0)
			{
				int toWrite = (int)Math.Min(remaining, buffer.Length);
				rng.NextBytes(buffer.AsSpan(0, toWrite));
				fs.Write(buffer, 0, toWrite);
				sha.AppendData(buffer, 0, toWrite);
				remaining -= toWrite;
			}
			fs.Flush();
		}
		sw.Stop();

		string sha256Hex = Convert.ToHexStringLower(sha.GetHashAndReset());
		Console.WriteLine($"PHASE_OUTPUT: Phase=Generate|Bytes={totalBytes}|Sha256={sha256Hex}|ElapsedMs={sw.ElapsedMilliseconds}");
		return 0;
	}

	private static async Task<int> ExecuteSplitAsync(string[] args)
	{
		if (args.Length < 4)
		{
			Console.Error.WriteLine("split requer: <caminhoOrigem> <tamanhoParteBytes> <diretorioDestino>");
			return 1;
		}

		string sourcePath = args[1];
		if (!long.TryParse(args[2], out long partSize) || partSize <= 0)
		{
			Console.Error.WriteLine("tamanhoParteBytes inválido.");
			return 1;
		}

		string destinationDir = args[3];
		Directory.CreateDirectory(destinationDir);

		var options = new SplitOptions
		{
			PartSize = partSize,
			DestinationDirectory = destinationDir,
			CalculateSha256 = true,
			BufferSize = 64 * 1024
		};

		var sw = Stopwatch.StartNew();
		var result = await SplitEngine.SplitAsync(sourcePath, options);
		sw.Stop();

		Console.WriteLine($"PHASE_OUTPUT: Phase=Split|Parts={result.TotalParts}|Manifest={result.ManifestFilePath}|Sha256={result.Manifest.TotalSha256 ?? string.Empty}|TotalCrc32={result.Manifest.TotalCrc32}|ElapsedMs={sw.ElapsedMilliseconds}");
		return 0;
	}

	private static async Task<int> ExecuteCombineAsync(string[] args)
	{
		if (args.Length < 4)
		{
			Console.Error.WriteLine("combine requer: <caminhoManifesto> <diretorioDestino> <nomeDestino>");
			return 1;
		}

		string manifestPath = args[1];
		string destinationDir = args[2];
		string destinationName = args[3];

		Directory.CreateDirectory(destinationDir);

		var options = new CombineOptions
		{
			DestinationDirectory = destinationDir,
			DestinationFileName = destinationName,
			VerifySha256 = true,
			BufferSize = 64 * 1024,
			CollisionPolicy = CombineCollisionPolicy.Sobrescrever
		};

		var sw = Stopwatch.StartNew();
		var result = await CombineEngine.CombineAsync(manifestPath, options);
		sw.Stop();

		Console.WriteLine($"PHASE_OUTPUT: Phase=Combine|DestPath={result.DestinationFilePath}|Bytes={result.TotalBytesWritten}|TotalCrc32={result.TotalCrc32}|ElapsedMs={sw.ElapsedMilliseconds}");
		return 0;
	}

	private static int ExecuteVerifyHash(string[] args)
	{
		if (args.Length < 3)
		{
			Console.Error.WriteLine("verify-hash requer: <arquivoOrigem> <arquivoRecombinado>");
			return 1;
		}

		string path1 = args[1];
		string path2 = args[2];

		var fi1 = new FileInfo(path1);
		var fi2 = new FileInfo(path2);

		if (!fi1.Exists)
		{
			Console.Error.WriteLine($"Arquivo inexistente: {path1}");
			return 1;
		}

		if (!fi2.Exists)
		{
			Console.Error.WriteLine($"Arquivo inexistente: {path2}");
			return 1;
		}

		if (fi1.Length != fi2.Length)
		{
			Console.WriteLine($"PHASE_OUTPUT: Phase=VerifyHash|Matched=False|Reason=LengthMismatch|Length1={fi1.Length}|Length2={fi2.Length}|ElapsedMs=0");
			return 0;
		}

		var sw = Stopwatch.StartNew();
		string hash1 = ComputeSha256(path1);
		string hash2 = ComputeSha256(path2);
		sw.Stop();

		bool matched = string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);
		Console.WriteLine($"PHASE_OUTPUT: Phase=VerifyHash|Matched={matched}|Sha256Original={hash1}|Sha256Recombined={hash2}|ElapsedMs={sw.ElapsedMilliseconds}");
		return matched ? 0 : 3;
	}

	private static string ComputeSha256(string path)
	{
		const int bufferSize = 1024 * 1024; // 1 MB
		byte[] buffer = new byte[bufferSize];
		using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
		int bytesRead;
		while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
		{
			sha.AppendData(buffer, 0, bytesRead);
		}
		return Convert.ToHexStringLower(sha.GetHashAndReset());
	}
}
