// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Status do resultado da operação de salvamento do editor hexadecimal (F060 M2 / D-F060-1).
	/// </summary>
	public enum HexSaveStatus
	{
		Success = 0,
		NoChanges = 1,
		FileIdentityChanged = 2,
		ReadOnly = 3,
		FileInUse = 4,
		DiskFullOrIoError = 5,
		JournalError = 6,
		TargetNotFound = 7,
		Failed = 8
	}

	/// <summary>
	/// Resultado da operação de salvamento no editor hexadecimal.
	/// </summary>
	public sealed class HexSaveResult
	{
		public required bool Success { get; init; }
		public required HexSaveStatus Status { get; init; }
		public string? ErrorMessage { get; init; }
		public Exception? Exception { get; init; }
		public int ModifiedBlocksWritten { get; init; }
		public int ModifiedBytesCount { get; init; }
		public string? JournalFilePath { get; init; }
	}

	/// <summary>
	/// Resultado da operação de recuperação a partir de um diário remanescente.
	/// </summary>
	public sealed class HexRecoveryResult
	{
		public required bool Recovered { get; init; }
		public required int BlocksRestored { get; init; }
		public required int IncompleteEntriesIgnored { get; init; }
		public string? ErrorMessage { get; init; }
		public string? JournalFilePath { get; init; }
	}

	/// <summary>
	/// Opções de salvamento e injeção de dependência para testes de segurança (D-F060-1).
	/// </summary>
	public sealed class HexSaveOptions
	{
		/// <summary>
		/// Caminho personalizado para o arquivo de diário de recuperação (.hexjournal).
		/// Se nulo, utiliza &lt;caminho-do-arquivo&gt;.hexjournal.
		/// </summary>
		public string? JournalFilePath { get; set; }

		/// <summary>
		/// Fábrica para stream do arquivo de destino (permite simular falta de espaço ou falha de disco em testes).
		/// </summary>
		public Func<string, FileMode, FileAccess, FileShare, int, FileOptions, Stream>? DestinationStreamFactory { get; set; }

		/// <summary>
		/// Fábrica para stream do diário de recuperação (permite simular falhas durante a criação do diário).
		/// </summary>
		public Func<string, FileMode, FileAccess, FileShare, int, FileOptions, Stream>? JournalStreamFactory { get; set; }

		/// <summary>
		/// Se definido e &gt; 0, simula a queda abrupta do processo após gravar N blocos no arquivo original,
		/// interrompendo sem reverter nem apagar o diário para permitir testar a recuperação posterior.
		/// </summary>
		public int? SimulateCrashAfterBlocksWritten { get; set; }
	}

	/// <summary>
	/// Exceção lançada exclusivamente em testes para simular queda de processo durante a gravação.
	/// </summary>
	public sealed class HexCrashSimulatedException : Exception
	{
		public HexCrashSimulatedException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Motor de gravação segura in-place e recuperação com diário transacional (F060 M2 / D-F060-1).
	/// </summary>
	public static class HexFileSaver
	{
		public const string JournalExtension = ".hexjournal";
		private static readonly byte[] JournalMagic = Encoding.ASCII.GetBytes("HEXJRNL1"); // 8 bytes
		private const int JournalVersion = 1;
		private const int HeaderSize = 40; // 8 (magic) + 4 (ver) + 8 (len) + 8 (ticks) + 4 (blkSize) + 4 (count) + 4 (crc)

		public static string GetDefaultJournalPath(string targetFilePath)
		{
			ArgumentNullException.ThrowIfNull(targetFilePath);
			return targetFilePath + JournalExtension;
		}

		public static bool HasPendingJournal(string targetFilePath, string? journalFilePath = null)
		{
			string journalPath = journalFilePath ?? GetDefaultJournalPath(targetFilePath);
			return File.Exists(journalPath);
		}

		public static uint ComputeEntryCrc32(long blockIndex, int blockLength, ReadOnlySpan<byte> originalBytes, ReadOnlySpan<byte> newBytes)
		{
			var crc = new Crc32();
			Span<byte> prefix = stackalloc byte[12];
			BinaryPrimitives.WriteInt64LittleEndian(prefix[..8], blockIndex);
			BinaryPrimitives.WriteInt32LittleEndian(prefix[8..12], blockLength);
			crc.Append(prefix);
			crc.Append(originalBytes);
			crc.Append(newBytes);
			return BinaryPrimitives.ReadUInt32LittleEndian(crc.GetCurrentHash());
		}

		public static uint ComputeHeaderCrc32(int version, long length, long lastWriteTimeTicks, int blockSize, int entryCount)
		{
			var crc = new Crc32();
			Span<byte> buffer = stackalloc byte[28];
			BinaryPrimitives.WriteInt32LittleEndian(buffer[..4], version);
			BinaryPrimitives.WriteInt64LittleEndian(buffer[4..12], length);
			BinaryPrimitives.WriteInt64LittleEndian(buffer[12..20], lastWriteTimeTicks);
			BinaryPrimitives.WriteInt32LittleEndian(buffer[20..24], blockSize);
			BinaryPrimitives.WriteInt32LittleEndian(buffer[24..28], entryCount);
			crc.Append(buffer);
			return BinaryPrimitives.ReadUInt32LittleEndian(crc.GetCurrentHash());
		}

		public static HexSaveResult Save(HexFileBuffer buffer, HexSaveOptions? options = null)
		{
			ArgumentNullException.ThrowIfNull(buffer);
			return Save(buffer, buffer.FilePath ?? throw new ArgumentException("Buffer does not have a FilePath specified.", nameof(buffer)), options);
		}

		public static HexSaveResult Save(HexFileBuffer buffer, string targetFilePath, HexSaveOptions? options = null)
		{
			ArgumentNullException.ThrowIfNull(buffer);
			ArgumentNullException.ThrowIfNull(targetFilePath);

			if (buffer.ModifiedBytesCount == 0)
			{
				return new HexSaveResult
				{
					Success = true,
					Status = HexSaveStatus.NoChanges,
					ModifiedBlocksWritten = 0,
					ModifiedBytesCount = 0
				};
			}

			if (!File.Exists(targetFilePath))
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.TargetNotFound,
					ErrorMessage = $"O arquivo de destino '{targetFilePath}' não existe."
				};
			}

			var fileInfo = new FileInfo(targetFilePath);

			// 1. Somente leitura
			if ((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.ReadOnly,
					ErrorMessage = $"O arquivo '{targetFilePath}' é somente leitura."
				};
			}

			// 2. Conferir identidade: Tamanho
			if (fileInfo.Length != buffer.Length)
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.FileIdentityChanged,
					ErrorMessage = $"A identidade do arquivo foi alterada: tamanho no disco ({fileInfo.Length} bytes) difere da abertura ({buffer.Length} bytes)."
				};
			}

			// 3. Conferir identidade: Data de modificação
			if (buffer.InitialLastWriteTimeUtc.HasValue &&
				fileInfo.LastWriteTimeUtc != buffer.InitialLastWriteTimeUtc.Value)
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.FileIdentityChanged,
					ErrorMessage = $"A identidade do arquivo foi alterada: data de modificação no disco ({fileInfo.LastWriteTimeUtc:O}) difere da abertura ({buffer.InitialLastWriteTimeUtc.Value:O})."
				};
			}

			// 4. Arquivo em uso
			try
			{
				using var testStream = new FileStream(targetFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			}
			catch (IOException ex) when (IsSharingViolation(ex))
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.FileInUse,
					ErrorMessage = $"O arquivo '{targetFilePath}' está em uso por outro processo.",
					Exception = ex
				};
			}
			catch (UnauthorizedAccessException ex)
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.ReadOnly,
					ErrorMessage = $"Acesso negado ao arquivo '{targetFilePath}'.",
					Exception = ex
				};
			}

			string journalPath = options?.JournalFilePath ?? GetDefaultJournalPath(targetFilePath);
			var modifiedBlocks = buffer.GetModifiedBlockIndices();

			// Se houver um diário anterior remanescente, restaura antes para garantir integridade prévia
			if (File.Exists(journalPath))
			{
				RecoverCore(targetFilePath, journalPath);
			}

			// 5. Gravação do diário de recuperação (antes de qualquer escrita no original)
			try
			{
				WriteJournal(journalPath, buffer, modifiedBlocks, options);
			}
			catch (Exception ex)
			{
				try
				{
					if (File.Exists(journalPath))
						File.Delete(journalPath);
				}
				catch
				{
				}

				HexSaveStatus status = IsDiskFull(ex) ? HexSaveStatus.DiskFullOrIoError : HexSaveStatus.JournalError;
				return new HexSaveResult
				{
					Success = false,
					Status = status,
					ErrorMessage = $"Falha ao gravar diário de recuperação: {ex.Message}",
					Exception = ex,
					JournalFilePath = journalPath
				};
			}

			// 6. Gravação dos blocos alterados no próprio arquivo original
			int blocksWritten = 0;
			try
			{
				Stream destStream = options?.DestinationStreamFactory != null
					? options.DestinationStreamFactory(targetFilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.None)
					: new FileStream(targetFilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.None);

				using (destStream)
				{
					for (int i = 0; i < modifiedBlocks.Count; i++)
					{
						if (options?.SimulateCrashAfterBlocksWritten.HasValue == true &&
							blocksWritten >= options.SimulateCrashAfterBlocksWritten.Value)
						{
							destStream.Flush();
							throw new HexCrashSimulatedException(
								$"Queda simulada do processo após gravar {blocksWritten} blocos.");
						}

						long blockIndex = modifiedBlocks[i];
						byte[] newBytes = buffer.ReadBlock(blockIndex);
						long blockOffset = blockIndex * buffer.BlockSize;

						destStream.Seek(blockOffset, SeekOrigin.Begin);
						destStream.Write(newBytes, 0, newBytes.Length);
						blocksWritten++;
					}

					// Os blocos precisam estar no disco ANTES de apagar o diário (queda de energia).
					FlushToDisk(destStream);
				}
			}
			catch (HexCrashSimulatedException)
			{
				// Não desfaz nem limpa o diário: simulação deliberada de crash para teste de recuperação
				throw;
			}
			catch (Exception ex)
			{
				// Falha durante a gravação (ex.: falta de espaço): reverte pelo diário e apaga o diário
				if (File.Exists(journalPath))
				{
					try
					{
						RecoverCore(targetFilePath, journalPath);
					}
					catch
					{
					}
				}

				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.DiskFullOrIoError,
					ErrorMessage = $"Falha ao gravar blocos alterados no arquivo: {ex.Message}",
					Exception = ex,
					JournalFilePath = journalPath
				};
			}

			// 7. Descarregar concluído: apaga o diário
			try
			{
				if (File.Exists(journalPath))
				{
					File.Delete(journalPath);
				}
			}
			catch
			{
			}

			int totalModifiedBytes = buffer.ModifiedBytesCount;
			DateTime newTime = File.GetLastWriteTimeUtc(targetFilePath);
			buffer.MarkSaved(newTime);

			return new HexSaveResult
			{
				Success = true,
				Status = HexSaveStatus.Success,
				ModifiedBlocksWritten = blocksWritten,
				ModifiedBytesCount = totalModifiedBytes,
				JournalFilePath = journalPath
			};
		}

		public static Task<HexSaveResult> SaveAsync(HexFileBuffer buffer, HexSaveOptions? options = null, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => Save(buffer, options), cancellationToken);
		}

		public static Task<HexSaveResult> SaveAsync(HexFileBuffer buffer, string targetFilePath, HexSaveOptions? options = null, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => Save(buffer, targetFilePath, options), cancellationToken);
		}

		public static HexRecoveryResult Recover(string targetFilePath, string? journalFilePath = null)
		{
			ArgumentNullException.ThrowIfNull(targetFilePath);
			string journalPath = journalFilePath ?? GetDefaultJournalPath(targetFilePath);
			return RecoverCore(targetFilePath, journalPath);
		}

		public static Task<HexRecoveryResult> RecoverAsync(string targetFilePath, string? journalFilePath = null, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => Recover(targetFilePath, journalFilePath), cancellationToken);
		}

		private static void WriteJournal(string journalPath, HexFileBuffer buffer, IReadOnlyList<long> modifiedBlocks, HexSaveOptions? options)
		{
			Stream stream = options?.JournalStreamFactory != null
				? options.JournalStreamFactory(journalPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)
				: new FileStream(journalPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);

			using (stream)
			{
				int version = JournalVersion;
				long length = buffer.Length;
				long ticks = buffer.InitialLastWriteTimeUtc?.Ticks ?? 0L;
				int blockSize = buffer.BlockSize;
				int entryCount = modifiedBlocks.Count;
				uint headerCrc = ComputeHeaderCrc32(version, length, ticks, blockSize, entryCount);

				Span<byte> header = stackalloc byte[HeaderSize];
				JournalMagic.CopyTo(header[..8]);
				BinaryPrimitives.WriteInt32LittleEndian(header[8..12], version);
				BinaryPrimitives.WriteInt64LittleEndian(header[12..20], length);
				BinaryPrimitives.WriteInt64LittleEndian(header[20..28], ticks);
				BinaryPrimitives.WriteInt32LittleEndian(header[28..32], blockSize);
				BinaryPrimitives.WriteInt32LittleEndian(header[32..36], entryCount);
				BinaryPrimitives.WriteUInt32LittleEndian(header[36..40], headerCrc);

				stream.Write(header);

				Span<byte> entryPrefix = stackalloc byte[12];
				Span<byte> crcBuf = stackalloc byte[4];

				for (int i = 0; i < modifiedBlocks.Count; i++)
				{
					long blockIndex = modifiedBlocks[i];
					byte[] orig = buffer.ReadOriginalBlock(blockIndex);
					byte[] modified = buffer.ReadBlock(blockIndex);
					int blockLen = orig.Length;

					uint entryCrc = ComputeEntryCrc32(blockIndex, blockLen, orig, modified);

					BinaryPrimitives.WriteInt64LittleEndian(entryPrefix[..8], blockIndex);
					BinaryPrimitives.WriteInt32LittleEndian(entryPrefix[8..12], blockLen);

					stream.Write(entryPrefix);
					stream.Write(orig);
					stream.Write(modified);

					BinaryPrimitives.WriteUInt32LittleEndian(crcBuf, entryCrc);
					stream.Write(crcBuf);
				}

				// O diário precisa estar no disco ANTES da primeira escrita no original.
				FlushToDisk(stream);
			}
		}

		private static void FlushToDisk(Stream stream)
		{
			if (stream is FileStream fileStream)
				fileStream.Flush(flushToDisk: true);
			else
				stream.Flush();
		}

		private static HexRecoveryResult RecoverCore(string targetFilePath, string journalPath)
		{
			if (!File.Exists(journalPath))
			{
				return new HexRecoveryResult
				{
					Recovered = false,
					BlocksRestored = 0,
					IncompleteEntriesIgnored = 0,
					ErrorMessage = "Diário de recuperação não encontrado.",
					JournalFilePath = journalPath
				};
			}

			if (!File.Exists(targetFilePath))
			{
				return new HexRecoveryResult
				{
					Recovered = false,
					BlocksRestored = 0,
					IncompleteEntriesIgnored = 0,
					ErrorMessage = $"Arquivo de destino '{targetFilePath}' não encontrado.",
					JournalFilePath = journalPath
				};
			}

			long targetLength = 0;
			long targetTicks = 0;
			int blockSize = 0;
			var validEntries = new List<(long BlockIndex, byte[] OriginalBytes)>();
			int incompleteEntriesIgnored = 0;

			using (var stream = new FileStream(journalPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				if (stream.Length < HeaderSize)
				{
					incompleteEntriesIgnored++;
				}
				else
				{
					byte[] headerBytes = new byte[HeaderSize];
					int read = ReadExact(stream, headerBytes, 0, HeaderSize);
					if (read < HeaderSize)
					{
						incompleteEntriesIgnored++;
					}
					else
					{
						ReadOnlySpan<byte> span = headerBytes;
						if (!span[..8].SequenceEqual(JournalMagic))
						{
							incompleteEntriesIgnored++;
						}
						else
						{
							int version = BinaryPrimitives.ReadInt32LittleEndian(span[8..12]);
							targetLength = BinaryPrimitives.ReadInt64LittleEndian(span[12..20]);
							targetTicks = BinaryPrimitives.ReadInt64LittleEndian(span[20..28]);
							blockSize = BinaryPrimitives.ReadInt32LittleEndian(span[28..32]);
							int entryCount = BinaryPrimitives.ReadInt32LittleEndian(span[32..36]);
							uint headerCrc = BinaryPrimitives.ReadUInt32LittleEndian(span[36..40]);

							uint expectedCrc = ComputeHeaderCrc32(version, targetLength, targetTicks, blockSize, entryCount);
							if (headerCrc != expectedCrc || version != JournalVersion || blockSize <= 0)
							{
								incompleteEntriesIgnored++;
							}
							else
							{
								byte[] prefixBuf = new byte[12];
								byte[] crcBuf = new byte[4];

								while (true)
								{
									int prefixRead = ReadExact(stream, prefixBuf, 0, 12);
									if (prefixRead == 0)
									{
										break;
									}

									if (prefixRead < 12)
									{
										incompleteEntriesIgnored++;
										break;
									}

									long blockIndex = BinaryPrimitives.ReadInt64LittleEndian(prefixBuf.AsSpan(0, 8));
									int blockLen = BinaryPrimitives.ReadInt32LittleEndian(prefixBuf.AsSpan(8, 4));

									if (blockLen <= 0 || blockLen > blockSize)
									{
										incompleteEntriesIgnored++;
										break;
									}

									byte[] orig = new byte[blockLen];
									if (ReadExact(stream, orig, 0, blockLen) < blockLen)
									{
										incompleteEntriesIgnored++;
										break;
									}

									byte[] mod = new byte[blockLen];
									if (ReadExact(stream, mod, 0, blockLen) < blockLen)
									{
										incompleteEntriesIgnored++;
										break;
									}

									if (ReadExact(stream, crcBuf, 0, 4) < 4)
									{
										incompleteEntriesIgnored++;
										break;
									}

									uint entryCrc = BinaryPrimitives.ReadUInt32LittleEndian(crcBuf);
									uint expectedEntryCrc = ComputeEntryCrc32(blockIndex, blockLen, orig, mod);

									if (entryCrc != expectedEntryCrc)
									{
										incompleteEntriesIgnored++;
										break;
									}

									validEntries.Add((blockIndex, orig));
								}
							}
						}
					}
				}
			}

			if (validEntries.Count > 0)
			{
				// A v1 só substitui bytes: o tamanho nunca muda. Um diário que não bate com o arquivo
				// (tamanho diferente, bloco fora do arquivo) não é nosso ou foi adulterado: não toca em nada
				// e mantém o diário para o usuário decidir.
				long actualLength = new FileInfo(targetFilePath).Length;
				bool consistent = actualLength == targetLength;
				foreach (var entry in validEntries)
				{
					if (entry.BlockIndex < 0 ||
						entry.BlockIndex > (long.MaxValue - entry.OriginalBytes.Length) / blockSize ||
						entry.BlockIndex * blockSize + entry.OriginalBytes.Length > actualLength)
					{
						consistent = false;
						break;
					}
				}

				if (!consistent)
				{
					return new HexRecoveryResult
					{
						Recovered = false,
						BlocksRestored = 0,
						IncompleteEntriesIgnored = incompleteEntriesIgnored,
						ErrorMessage = "O diário de recuperação não corresponde a este arquivo (tamanho ou posição de bloco inconsistentes). Nada foi alterado.",
						JournalFilePath = journalPath
					};
				}

				using (var destStream = new FileStream(targetFilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
				{
					foreach (var entry in validEntries)
					{
						long offset = entry.BlockIndex * blockSize;
						destStream.Seek(offset, SeekOrigin.Begin);
						destStream.Write(entry.OriginalBytes, 0, entry.OriginalBytes.Length);
					}

					FlushToDisk(destStream);
				}
			}

			if (targetTicks > 0)
			{
				try
				{
					File.SetLastWriteTimeUtc(targetFilePath, new DateTime(targetTicks, DateTimeKind.Utc));
				}
				catch
				{
				}
			}

			try
			{
				File.Delete(journalPath);
			}
			catch
			{
			}

			return new HexRecoveryResult
			{
				Recovered = true,
				BlocksRestored = validEntries.Count,
				IncompleteEntriesIgnored = incompleteEntriesIgnored,
				JournalFilePath = journalPath
			};
		}

		private static int ReadExact(Stream stream, byte[] buffer, int offset, int count)
		{
			int totalRead = 0;
			while (totalRead < count)
			{
				int read = stream.Read(buffer, offset + totalRead, count - totalRead);
				if (read == 0)
					break;
				totalRead += read;
			}
			return totalRead;
		}

		private static bool IsSharingViolation(IOException ex)
		{
			int hr = ex.HResult;
			return hr == unchecked((int)0x80070020) || hr == unchecked((int)0x80070021);
		}

		private static bool IsDiskFull(Exception ex)
		{
			if (ex is IOException ioEx)
			{
				int hr = ioEx.HResult & 0xFFFF;
				if (hr == 112 || hr == 39)
					return true;

				string msg = ioEx.Message;
				if (msg.Contains("disk full", StringComparison.OrdinalIgnoreCase) ||
					msg.Contains("espaço", StringComparison.OrdinalIgnoreCase) ||
					msg.Contains("space", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}
}
