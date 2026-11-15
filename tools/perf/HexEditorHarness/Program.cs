// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using KyraFiles.Shared.Helpers;

namespace HexEditorHarness;

public static class Program
{
	public static int Main(string[] args)
	{
		if (args.Length == 0)
		{
			PrintUsage();
			return 1;
		}

		string command = args[0].ToLowerInvariant();

		try
		{
			return command switch
			{
				"generate" => ExecuteGenerate(args),
				"open" => ExecuteOpen(args),
				"seek-end" => ExecuteSeekEnd(args),
				"edit-save" => ExecuteEditSave(args),
				"verify" => ExecuteVerify(args),
				_ => HandleUnknownCommand(command)
			};
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
		Console.WriteLine("  HexEditorHarness generate <caminhoArquivo> <tamanhoBytes> [seed]");
		Console.WriteLine("  HexEditorHarness open <caminhoArquivo>");
		Console.WriteLine("  HexEditorHarness seek-end <caminhoArquivo>");
		Console.WriteLine("  HexEditorHarness edit-save <caminhoArquivo> [seed]");
		Console.WriteLine("  HexEditorHarness verify <caminhoArquivo> [seed]");
	}

	private static int HandleUnknownCommand(string command)
	{
		Console.Error.WriteLine($"Comando desconhecido: {command}");
		PrintUsage();
		return 1;
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

	private static int ExecuteOpen(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("open requer: <caminhoArquivo>");
			return 1;
		}

		string path = args[1];
		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"Arquivo inexistente: {path}");
			return 1;
		}

		var sw = Stopwatch.StartNew();
		using var buffer = new HexFileBuffer(path);
		byte[] block0 = buffer.ReadBlock(0);
		sw.Stop();

		Console.WriteLine($"PHASE_OUTPUT: Phase=Open|FileLength={buffer.Length}|BlockSize={buffer.BlockSize}|ReadBytes={block0.Length}|FirstByte={block0[0]}|ElapsedMs={sw.ElapsedMilliseconds}");
		return 0;
	}

	private static int ExecuteSeekEnd(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("seek-end requer: <caminhoArquivo>");
			return 1;
		}

		string path = args[1];
		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"Arquivo inexistente: {path}");
			return 1;
		}

		var sw = Stopwatch.StartNew();
		using var buffer = new HexFileBuffer(path);
		long offset = Math.Max(0, buffer.Length - 256);
		int count = (int)Math.Min(256, buffer.Length);
		byte[] lastBytes = buffer.Read(offset, count);
		sw.Stop();

		byte lastByte = lastBytes.Length > 0 ? lastBytes[^1] : (byte)0;
		Console.WriteLine($"PHASE_OUTPUT: Phase=SeekEnd|FileLength={buffer.Length}|Offset={offset}|ReadBytes={lastBytes.Length}|LastByte={lastByte}|ElapsedMs={sw.ElapsedMilliseconds}");
		return 0;
	}

	private static int ExecuteEditSave(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("edit-save requer: <caminhoArquivo> [seed]");
			return 1;
		}

		string path = args[1];
		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"Arquivo inexistente: {path}");
			return 1;
		}

		using var buffer = new HexFileBuffer(path);

		if (buffer.Length < 3)
		{
			Console.Error.WriteLine($"Arquivo pequeno demais para teste de 3 bytes: {buffer.Length}");
			return 1;
		}

		long offset0 = 0;
		long offsetMid = buffer.Length / 2;
		long offsetEnd = buffer.Length - 1;

		byte orig0 = buffer.GetByte(offset0);
		byte origMid = buffer.GetByte(offsetMid);
		byte origEnd = buffer.GetByte(offsetEnd);

		byte new0 = (byte)(orig0 ^ 0xAA);
		byte newMid = (byte)(origMid ^ 0x55);
		byte newEnd = (byte)(origEnd ^ 0xFF);

		buffer.SetByte(offset0, new0);
		buffer.SetByte(offsetMid, newMid);
		buffer.SetByte(offsetEnd, newEnd);

		var sw = Stopwatch.StartNew();
		var saveResult = HexFileSaver.Save(buffer);
		sw.Stop();

		if (!saveResult.Success)
		{
			Console.Error.WriteLine($"Save falhou: Status={saveResult.Status}, Mensagem={saveResult.ErrorMessage}");
			return 2;
		}

		Console.WriteLine($"PHASE_OUTPUT: Phase=EditAndSave|Status={saveResult.Status}|ModifiedBlocksWritten={saveResult.ModifiedBlocksWritten}|ModifiedBytesCount={saveResult.ModifiedBytesCount}|Offset0={offset0}|Orig0={orig0}|New0={new0}|OffsetMid={offsetMid}|OrigMid={origMid}|NewMid={newMid}|OffsetEnd={offsetEnd}|OrigEnd={origEnd}|NewEnd={newEnd}|ElapsedMs={sw.ElapsedMilliseconds}");
		return 0;
	}

	private static int ExecuteVerify(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("verify requer: <caminhoArquivo> [seed]");
			return 1;
		}

		string path = args[1];
		if (!File.Exists(path))
		{
			Console.Error.WriteLine($"Arquivo inexistente: {path}");
			return 1;
		}

		int seed = 42;
		if (args.Length > 2 && int.TryParse(args[2], out int customSeed))
		{
			seed = customSeed;
		}

		// 1. Conferir que o diario foi apagado
		string journalPath = HexFileSaver.GetDefaultJournalPath(path);
		bool journalDeleted = !File.Exists(journalPath);

		var sw = Stopwatch.StartNew();
		var fileInfo = new FileInfo(path);
		long fileLength = fileInfo.Length;

		long offset0 = 0;
		long offsetMid = fileLength / 2;
		long offsetEnd = fileLength - 1;

		const int bufferSize = 1024 * 1024; // 1 MB
		byte[] savedBuf = new byte[bufferSize];
		byte[] expectedBuf = new byte[bufferSize];

		var rng = new Random(seed);
		using var shaSaved = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		using var shaExpected = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);

		long currentOffset = 0;
		long remaining = fileLength;
		bool allBytesMatched = true;
		long mismatchOffset = -1;

		while (remaining > 0)
		{
			int toRead = (int)Math.Min(remaining, bufferSize);
			int bytesRead = fs.Read(savedBuf, 0, toRead);
			if (bytesRead != toRead)
			{
				throw new EndOfStreamException($"Término inesperado do stream ao ler {toRead} bytes no offset {currentOffset}");
			}

			// Gera bloco original deterministico
			rng.NextBytes(expectedBuf.AsSpan(0, toRead));

			// Substitui exatamente os 3 bytes nos offsets alterados
			if (offset0 >= currentOffset && offset0 < currentOffset + toRead)
			{
				int idx = (int)(offset0 - currentOffset);
				expectedBuf[idx] = (byte)(expectedBuf[idx] ^ 0xAA);
			}

			if (offsetMid >= currentOffset && offsetMid < currentOffset + toRead)
			{
				int idx = (int)(offsetMid - currentOffset);
				expectedBuf[idx] = (byte)(expectedBuf[idx] ^ 0x55);
			}

			if (offsetEnd >= currentOffset && offsetEnd < currentOffset + toRead)
			{
				int idx = (int)(offsetEnd - currentOffset);
				expectedBuf[idx] = (byte)(expectedBuf[idx] ^ 0xFF);
			}

			shaSaved.AppendData(savedBuf, 0, toRead);
			shaExpected.AppendData(expectedBuf, 0, toRead);

			if (allBytesMatched && !savedBuf.AsSpan(0, toRead).SequenceEqual(expectedBuf.AsSpan(0, toRead)))
			{
				allBytesMatched = false;
				for (int i = 0; i < toRead; i++)
				{
					if (savedBuf[i] != expectedBuf[i])
					{
						mismatchOffset = currentOffset + i;
						break;
					}
				}
			}

			currentOffset += toRead;
			remaining -= toRead;
		}

		sw.Stop();

		string shaSavedHex = Convert.ToHexStringLower(shaSaved.GetHashAndReset());
		string shaExpectedHex = Convert.ToHexStringLower(shaExpected.GetHashAndReset());
		bool hashMatch = string.Equals(shaSavedHex, shaExpectedHex, StringComparison.OrdinalIgnoreCase);

		bool verified = hashMatch && allBytesMatched && journalDeleted;

		Console.WriteLine($"PHASE_OUTPUT: Phase=Verify|Verified={verified}|HashMatch={hashMatch}|AllBytesMatched={allBytesMatched}|JournalDeleted={journalDeleted}|Sha256Saved={shaSavedHex}|Sha256Expected={shaExpectedHex}|MismatchOffset={mismatchOffset}|ElapsedMs={sw.ElapsedMilliseconds}");
		return verified ? 0 : 3;
	}
}
