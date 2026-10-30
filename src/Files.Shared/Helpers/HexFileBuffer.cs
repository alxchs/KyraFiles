// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Provides a block-based, LRU-cached file buffer for hex editing with in-memory edit tracking,
	/// undo/redo history, and modified block identification without loading the entire file into memory.
	/// </summary>
	public sealed class HexFileBuffer : IDisposable
	{
		public const int DefaultBlockSize = 64 * 1024; // 64 KB
		public const int DefaultMaxCachedBlocks = 32;

		private sealed class CacheNode
		{
			public long BlockIndex { get; }
			public byte[] Data { get; }

			public CacheNode(long blockIndex, byte[] data)
			{
				BlockIndex = blockIndex;
				Data = data;
			}
		}

		private sealed record EditAction(long Offset, byte[] OldBytes, byte[] NewBytes);

		private readonly Stream _stream;
		private readonly bool _ownsStream;
		private readonly object _syncLock = new();
		private readonly Dictionary<long, LinkedListNode<CacheNode>> _cacheMap = new();
		private readonly LinkedList<CacheNode> _lruList = new();
		private readonly Dictionary<long, byte> _edits = new();
		private readonly Stack<EditAction> _undoStack = new();
		private readonly Stack<EditAction> _redoStack = new();

		private bool _isDisposed;

		public string? FilePath { get; }
		public long Length { get; }
		public int BlockSize { get; }
		public int MaxCachedBlocks { get; }
		public DateTime? InitialLastWriteTimeUtc { get; private set; }

		public int CachedBlockCount
		{
			get
			{
				lock (_syncLock)
				{
					return _cacheMap.Count;
				}
			}
		}

		public int ModifiedBytesCount
		{
			get
			{
				lock (_syncLock)
				{
					return _edits.Count;
				}
			}
		}

		public bool CanUndo
		{
			get
			{
				lock (_syncLock)
				{
					return _undoStack.Count > 0;
				}
			}
		}

		public bool CanRedo
		{
			get
			{
				lock (_syncLock)
				{
					return _redoStack.Count > 0;
				}
			}
		}

		public IReadOnlyList<long> ModifiedBlockIndices => GetModifiedBlockIndices();

		public bool IsByteModified(long offset)
		{
			lock (_syncLock)
			{
				return _edits.ContainsKey(offset);
			}
		}

		public IReadOnlyDictionary<long, byte> Edits
		{
			get
			{
				lock (_syncLock)
				{
					return new Dictionary<long, byte>(_edits);
				}
			}
		}

		public HexFileBuffer(string filePath, int blockSize = DefaultBlockSize, int maxCachedBlocks = DefaultMaxCachedBlocks)
		{
			ArgumentNullException.ThrowIfNull(filePath);
			if (blockSize < 1)
				throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be greater than zero.");
			if (maxCachedBlocks < 1)
				throw new ArgumentOutOfRangeException(nameof(maxCachedBlocks), maxCachedBlocks, "Max cached blocks must be greater than zero.");

			FilePath = filePath;
			BlockSize = blockSize;
			MaxCachedBlocks = maxCachedBlocks;
			_ownsStream = true;

			_stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.RandomAccess);
			Length = _stream.Length;
			if (File.Exists(filePath))
			{
				InitialLastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
			}
		}

		public HexFileBuffer(Stream stream, int blockSize = DefaultBlockSize, int maxCachedBlocks = DefaultMaxCachedBlocks, bool leaveOpen = false)
		{
			ArgumentNullException.ThrowIfNull(stream);
			if (!stream.CanRead)
				throw new ArgumentException("Stream must be readable.", nameof(stream));
			if (!stream.CanSeek)
				throw new ArgumentException("Stream must be seekable.", nameof(stream));
			if (blockSize < 1)
				throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize, "Block size must be greater than zero.");
			if (maxCachedBlocks < 1)
				throw new ArgumentOutOfRangeException(nameof(maxCachedBlocks), maxCachedBlocks, "Max cached blocks must be greater than zero.");

			FilePath = (stream as FileStream)?.Name;
			BlockSize = blockSize;
			MaxCachedBlocks = maxCachedBlocks;
			_ownsStream = !leaveOpen;
			_stream = stream;
			Length = stream.Length;
			if (stream is FileStream fs && !string.IsNullOrEmpty(fs.Name) && File.Exists(fs.Name))
			{
				InitialLastWriteTimeUtc = File.GetLastWriteTimeUtc(fs.Name);
			}
		}

		public byte GetByte(long offset)
		{
			ThrowIfDisposed();
			if (offset < 0 || offset >= Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} is outside the file bounds (0..{Length - 1}).");

			lock (_syncLock)
			{
				if (_edits.TryGetValue(offset, out byte edited))
					return edited;

				return GetOriginalByteCore(offset);
			}
		}

		public int Read(long offset, byte[] destination, int destinationOffset, int count)
		{
			ArgumentNullException.ThrowIfNull(destination);
			ThrowIfDisposed();

			if (offset < 0 || offset > Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} is outside valid range (0..{Length}).");
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
			if (offset + count > Length)
				throw new ArgumentOutOfRangeException(nameof(count), count, $"Range [{offset}..{offset + count}) exceeds file length {Length}.");
			if (destinationOffset < 0 || destinationOffset + count > destination.Length)
				throw new ArgumentException("Destination buffer too small or invalid destination offset.", nameof(destination));

			if (count == 0)
				return 0;

			lock (_syncLock)
			{
				ReadBaseBytesCore(offset, destination.AsSpan(destinationOffset, count));
				OverlayEditsCore(offset, destination.AsSpan(destinationOffset, count));
			}

			return count;
		}

		public int Read(long offset, Span<byte> destination)
		{
			ThrowIfDisposed();

			if (offset < 0 || offset > Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} is outside valid range (0..{Length}).");
			if (offset + destination.Length > Length)
				throw new ArgumentOutOfRangeException(nameof(destination), destination.Length, $"Range [{offset}..{offset + destination.Length}) exceeds file length {Length}.");

			if (destination.Length == 0)
				return 0;

			lock (_syncLock)
			{
				ReadBaseBytesCore(offset, destination);
				OverlayEditsCore(offset, destination);
			}

			return destination.Length;
		}

		public byte[] Read(long offset, int count)
		{
			ThrowIfDisposed();
			if (offset < 0 || offset > Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} is outside valid range (0..{Length}).");
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
			if (offset + count > Length)
				throw new ArgumentOutOfRangeException(nameof(count), count, $"Range [{offset}..{offset + count}) exceeds file length {Length}.");

			byte[] buffer = new byte[count];
			Read(offset, buffer, 0, count);
			return buffer;
		}

		public byte[] ReadBlock(long blockIndex)
		{
			ThrowIfDisposed();
			long blockStart = blockIndex * BlockSize;
			if (blockStart < 0 || blockStart >= Length)
				throw new ArgumentOutOfRangeException(nameof(blockIndex), blockIndex, $"Block index {blockIndex} is outside file bounds.");

			int count = (int)Math.Min(BlockSize, Length - blockStart);
			byte[] destination = new byte[count];
			Read(blockStart, destination, 0, count);
			return destination;
		}

		public byte[] ReadOriginalBlock(long blockIndex)
		{
			ThrowIfDisposed();
			long blockStart = blockIndex * BlockSize;
			if (blockStart < 0 || blockStart >= Length)
				throw new ArgumentOutOfRangeException(nameof(blockIndex), blockIndex, $"Block index {blockIndex} is outside file bounds.");

			int count = (int)Math.Min(BlockSize, Length - blockStart);
			byte[] destination = new byte[count];
			lock (_syncLock)
			{
				ReadBaseBytesCore(blockStart, destination);
			}
			return destination;
		}

		public void SetByte(long offset, byte value)
		{
			ThrowIfDisposed();
			if (offset < 0 || offset >= Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} is outside the file bounds (0..{Length - 1}).");

			lock (_syncLock)
			{
				byte originalByte = GetOriginalByteCore(offset);
				byte currentByte = _edits.TryGetValue(offset, out byte existingEdit) ? existingEdit : originalByte;

				if (value == currentByte)
					return;

				_undoStack.Push(new EditAction(offset, new[] { currentByte }, new[] { value }));
				_redoStack.Clear();

				if (value == originalByte)
					_edits.Remove(offset);
				else
					_edits[offset] = value;
			}
		}

		public void SetBytes(long offset, ReadOnlySpan<byte> bytes)
		{
			ThrowIfDisposed();
			if (offset < 0 || offset > Length)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Offset {offset} is outside valid range (0..{Length}).");
			if (offset + bytes.Length > Length)
				throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length, $"Range [{offset}..{offset + bytes.Length}) exceeds file length {Length}.");

			if (bytes.Length == 0)
				return;

			lock (_syncLock)
			{
				byte[] oldBytes = new byte[bytes.Length];
				byte[] newBytes = bytes.ToArray();
				bool hasChange = false;

				for (int i = 0; i < bytes.Length; i++)
				{
					long curOffset = offset + i;
					byte originalByte = GetOriginalByteCore(curOffset);
					byte currentByte = _edits.TryGetValue(curOffset, out byte existingEdit) ? existingEdit : originalByte;
					oldBytes[i] = currentByte;

					if (newBytes[i] != currentByte)
						hasChange = true;
				}

				if (!hasChange)
					return;

				_undoStack.Push(new EditAction(offset, oldBytes, newBytes));
				_redoStack.Clear();

				for (int i = 0; i < bytes.Length; i++)
				{
					long curOffset = offset + i;
					byte originalByte = GetOriginalByteCore(curOffset);
					byte newVal = newBytes[i];

					if (newVal == originalByte)
						_edits.Remove(curOffset);
					else
						_edits[curOffset] = newVal;
				}
			}
		}

		public void SetBytes(long offset, byte[] bytes)
		{
			ArgumentNullException.ThrowIfNull(bytes);
			SetBytes(offset, bytes.AsSpan());
		}

		public bool Undo()
		{
			ThrowIfDisposed();
			lock (_syncLock)
			{
				if (_undoStack.Count == 0)
					return false;

				EditAction action = _undoStack.Pop();
				_redoStack.Push(action);

				for (int i = 0; i < action.OldBytes.Length; i++)
				{
					long curOffset = action.Offset + i;
					byte originalByte = GetOriginalByteCore(curOffset);
					byte oldVal = action.OldBytes[i];

					if (oldVal == originalByte)
						_edits.Remove(curOffset);
					else
						_edits[curOffset] = oldVal;
				}

				return true;
			}
		}

		public bool Redo()
		{
			ThrowIfDisposed();
			lock (_syncLock)
			{
				if (_redoStack.Count == 0)
					return false;

				EditAction action = _redoStack.Pop();
				_undoStack.Push(action);

				for (int i = 0; i < action.NewBytes.Length; i++)
				{
					long curOffset = action.Offset + i;
					byte originalByte = GetOriginalByteCore(curOffset);
					byte newVal = action.NewBytes[i];

					if (newVal == originalByte)
						_edits.Remove(curOffset);
					else
						_edits[curOffset] = newVal;
				}

				return true;
			}
		}

		public IReadOnlyList<long> GetModifiedBlockIndices()
		{
			ThrowIfDisposed();
			lock (_syncLock)
			{
				if (_edits.Count == 0)
					return Array.Empty<long>();

				var set = new SortedSet<long>();
				foreach (var offset in _edits.Keys)
				{
					set.Add(offset / BlockSize);
				}

				return set.ToArray();
			}
		}

		public void ClearEdits()
		{
			ThrowIfDisposed();
			lock (_syncLock)
			{
				_edits.Clear();
				_undoStack.Clear();
				_redoStack.Clear();
			}
		}

		public void ClearCache()
		{
			ThrowIfDisposed();
			lock (_syncLock)
			{
				_cacheMap.Clear();
				_lruList.Clear();
			}
		}

		public void MarkSaved(DateTime? newLastWriteTimeUtc = null)
		{
			ThrowIfDisposed();
			lock (_syncLock)
			{
				_edits.Clear();
				_undoStack.Clear();
				_redoStack.Clear();
				_cacheMap.Clear();
				_lruList.Clear();
				if (newLastWriteTimeUtc.HasValue)
				{
					InitialLastWriteTimeUtc = newLastWriteTimeUtc.Value;
				}
				else if (FilePath != null && File.Exists(FilePath))
				{
					InitialLastWriteTimeUtc = File.GetLastWriteTimeUtc(FilePath);
				}
			}
		}

		private byte GetOriginalByteCore(long offset)
		{
			long blockIndex = offset / BlockSize;
			int offsetInBlock = (int)(offset % BlockSize);
			byte[] blockData = GetOrLoadBlockCore(blockIndex);
			return blockData[offsetInBlock];
		}

		private byte[] GetOrLoadBlockCore(long blockIndex)
		{
			if (_cacheMap.TryGetValue(blockIndex, out var node))
			{
				_lruList.Remove(node);
				_lruList.AddFirst(node);
				return node.Value.Data;
			}

			long blockStart = blockIndex * BlockSize;
			int bytesToRead = (int)Math.Min(BlockSize, Length - blockStart);
			byte[] data = new byte[bytesToRead];

			_stream.Seek(blockStart, SeekOrigin.Begin);
			int totalRead = 0;
			while (totalRead < bytesToRead)
			{
				int read = _stream.Read(data, totalRead, bytesToRead - totalRead);
				if (read == 0)
					break;
				totalRead += read;
			}

			if (_cacheMap.Count >= MaxCachedBlocks)
			{
				var lastNode = _lruList.Last!;
				_cacheMap.Remove(lastNode.Value.BlockIndex);
				_lruList.RemoveLast();
			}

			var cacheNode = new CacheNode(blockIndex, data);
			var newNode = _lruList.AddFirst(cacheNode);
			_cacheMap[blockIndex] = newNode;

			return data;
		}

		private void ReadBaseBytesCore(long offset, Span<byte> destination)
		{
			int count = destination.Length;
			long startBlock = offset / BlockSize;
			long endBlock = (offset + count - 1) / BlockSize;
			int bytesCopied = 0;

			for (long block = startBlock; block <= endBlock; block++)
			{
				long blockStart = block * BlockSize;
				int blockLength = (int)Math.Min(BlockSize, Length - blockStart);
				byte[] blockData = GetOrLoadBlockCore(block);

				int sliceStart = (int)Math.Max(0, offset - blockStart);
				long sliceEnd = Math.Min(blockStart + blockLength, offset + count);
				int sliceLength = (int)(sliceEnd - (blockStart + sliceStart));

				blockData.AsSpan(sliceStart, sliceLength).CopyTo(destination.Slice(bytesCopied, sliceLength));
				bytesCopied += sliceLength;
			}
		}

		private void OverlayEditsCore(long offset, Span<byte> destination)
		{
			int count = destination.Length;
			if (_edits.Count == 0 || count == 0)
				return;

			if (_edits.Count < count)
			{
				foreach (var kvp in _edits)
				{
					if (kvp.Key >= offset && kvp.Key < offset + count)
					{
						destination[(int)(kvp.Key - offset)] = kvp.Value;
					}
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (_edits.TryGetValue(offset + i, out byte val))
					{
						destination[i] = val;
					}
				}
			}
		}

		private void ThrowIfDisposed()
		{
			ObjectDisposedException.ThrowIf(_isDisposed, this);
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			lock (_syncLock)
			{
				if (_isDisposed)
					return;

				_isDisposed = true;
				_cacheMap.Clear();
				_lruList.Clear();
				_edits.Clear();
				_undoStack.Clear();
				_redoStack.Clear();

				if (_ownsStream)
				{
					_stream.Dispose();
				}
			}
		}
	}
}
