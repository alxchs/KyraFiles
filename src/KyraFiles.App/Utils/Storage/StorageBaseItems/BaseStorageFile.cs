// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace KyraFiles.App.Utils.Storage
{
	public abstract partial class BaseStorageFile : IBaseStorageFile
	{
		public bool IsAvailable
			=> true;

		public StorageProvider? Provider
			=> null;

		public abstract string Path { get; }

		public abstract string Name { get; }

		public abstract string DisplayName { get; }

		public abstract string ContentType { get; }

		public abstract string FileType { get; }

		public abstract string DisplayType { get; }

		public abstract DateTimeOffset DateCreated { get; }

		public abstract Windows.Storage.FileAttributes Attributes { get; }

		public abstract string FolderRelativeId { get; }

		public abstract IStorageItemExtraProperties Properties { get; }
		[MaybeNull]
		StorageItemContentProperties IStorageItemProperties.Properties
			=> this is SystemStorageFile file ? file.File.Properties : null;

		public static implicit operator BaseStorageFile?(StorageFile? value)
		{
			return value is not null ? new SystemStorageFile(value) : null;
		}

		public abstract IAsyncOperation<StorageFile> ToStorageFileAsync();

		public abstract bool IsEqual(IStorageItem item);

		public abstract bool IsOfType(StorageItemTypes type);

		public abstract IAsyncOperation<BaseStorageFolder?> GetParentAsync();

		IAsyncOperation<StorageFolder?> IStorageItem2.GetParentAsync()
		{
			return AsyncInfo.Run<StorageFolder?>(async (cancellationToken) =>
			{
				var parent = await GetParentAsync()
					?? throw new InvalidOperationException("The file does not have an accessible parent.");
				return await parent.ToStorageFolderAsync();
			});
		}

		public abstract IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();

		IAsyncOperation<BasicProperties> IStorageItem.GetBasicPropertiesAsync()
		{
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await (await ToStorageFileAsync()).GetBasicPropertiesAsync());
		}

		public abstract IAsyncOperation<IRandomAccessStream?> OpenAsync(FileAccessMode accessMode);

		public abstract IAsyncOperation<IRandomAccessStream?> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options);

		public abstract IAsyncOperation<IRandomAccessStreamWithContentType?> OpenReadAsync();

		public abstract IAsyncOperation<IInputStream?> OpenSequentialReadAsync();

		public abstract IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync();

		public abstract IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options);

		public abstract IAsyncOperation<BaseStorageFile?> CopyAsync(IStorageFolder destinationFolder);

		IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder)
		{
			return
				AsyncInfo.Run(async (cancellationToken) =>
				{
					var file = await CopyAsync(destinationFolder)
						?? throw new IOException("The storage provider did not copy the file.");
					return await file.ToStorageFileAsync();
				});
		}

		public abstract IAsyncOperation<BaseStorageFile?> CopyAsync(IStorageFolder destinationFolder, string desiredNewName);

		IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
		{
			return
				AsyncInfo.Run(async (cancellationToken) =>
				{
					var file = await CopyAsync(destinationFolder, desiredNewName)
						?? throw new IOException($"The storage provider did not copy the file as '{desiredNewName}'.");
					return await file.ToStorageFileAsync();
				});
		}

		public abstract IAsyncOperation<BaseStorageFile?> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);

		IAsyncOperation<StorageFile> IStorageFile.CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return
				AsyncInfo.Run(async (cancellationToken) =>
				{
					var file = await CopyAsync(destinationFolder, desiredNewName, option)
						?? throw new IOException($"The storage provider did not copy the file as '{desiredNewName}'.");
					return await file.ToStorageFileAsync();
				});
		}

		public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder);

		public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName);

		public abstract IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);

		public abstract IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace);

		public abstract IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace);

		public abstract IAsyncAction RenameAsync(string desiredName);

		public abstract IAsyncAction RenameAsync(string desiredName, NameCollisionOption option);

		public abstract IAsyncAction DeleteAsync();

		public abstract IAsyncAction DeleteAsync(StorageDeleteOption option);

		public abstract IAsyncOperation<StorageItemThumbnail?> GetThumbnailAsync(ThumbnailMode mode);

		public abstract IAsyncOperation<StorageItemThumbnail?> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize);

		public abstract IAsyncOperation<StorageItemThumbnail?> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options);

		public IAsyncOperation<StorageItemThumbnail?> GetScaledImageAsThumbnailAsync(ThumbnailMode mode)
		{
			return Task.FromResult<StorageItemThumbnail?>(null).AsAsyncOperation();
		}

		public IAsyncOperation<StorageItemThumbnail?> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize)
		{
			return Task.FromResult<StorageItemThumbnail?>(null).AsAsyncOperation();
		}

		public IAsyncOperation<StorageItemThumbnail?> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
		{
			return Task.FromResult<StorageItemThumbnail?>(null).AsAsyncOperation();
		}

		public static IAsyncOperation<BaseStorageFile?> GetFileFromPathAsync(string path)
		{
			return AsyncInfo.Run<BaseStorageFile?>(async (cancellationToken)
					=> await ZipStorageFile.FromPathAsync(path) ?? await FtpStorageFile.FromPathAsync(path) ?? await ShellStorageFile.FromPathAsync(path) ?? await NativeStorageFile.FromPathAsync(path) ?? await SystemStorageFile.FromPathAsync(path)
				);
		}

		public async Task<string> ReadTextAsync(int maxLength = -1)
		{
			using var inputStream = await OpenReadAsync()
				?? throw new IOException("The file could not be opened for reading.");
			await using var stream = inputStream.AsStreamForRead();

			// Detecta codificação inicial por BOM ou amostra de bytes UTF-8 / ANSI (ex: arquivos .pas em Windows-1252)
			long streamLen = stream.CanSeek ? stream.Length : -1;
			int sampleSize = (int)(streamLen > 0 ? Math.Min(4096, streamLen) : 4096);
			byte[] sampleBuffer = new byte[sampleSize];
			int sampleRead = await stream.ReadAsync(sampleBuffer.AsMemory(0, sampleSize));
			Stream effectiveStream = stream;
			if (stream.CanSeek)
			{
				stream.Position = 0;
			}
			else if (sampleRead > 0)
			{
				effectiveStream = new PrependSampleStream(sampleBuffer, sampleRead, stream);
			}

			var detectedEncoding = KyraFiles.Shared.Helpers.TextEncodingHelper.DetectEncoding(sampleBuffer, sampleRead);
			using var dataReader = new StreamReader(effectiveStream, detectedEncoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
			StringBuilder builder = new();
			int charsRead, charsToRead;
			do
			{
				charsToRead = maxLength < 0 ? 4096 : Math.Min(maxLength - builder.Length, 4096);
				var data = new char[charsToRead];
				charsRead = await dataReader.ReadAsync(data);
				builder.Append(data, 0, charsRead);
			} while (charsRead > 0 && (maxLength < 0 || builder.Length < maxLength) && (!stream.CanSeek || stream.Position < stream.Length));
			return builder.ToString();
		}

		private sealed partial class PrependSampleStream(byte[] prefix, int prefixLength, Stream inner) : Stream
		{
			private int _prefixOffset;

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => throw new NotSupportedException();
			public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
			public override void Flush() { }
			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
			public override void SetLength(long value) => throw new NotSupportedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (_prefixOffset < prefixLength)
				{
					int toCopy = Math.Min(count, prefixLength - _prefixOffset);
					System.Buffer.BlockCopy(prefix, _prefixOffset, buffer, offset, toCopy);
					_prefixOffset += toCopy;
					return toCopy;
				}
				return inner.Read(buffer, offset, count);
			}

			public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			{
				if (_prefixOffset < prefixLength)
				{
					int toCopy = Math.Min(buffer.Length, prefixLength - _prefixOffset);
					prefix.AsMemory(_prefixOffset, toCopy).CopyTo(buffer);
					_prefixOffset += toCopy;
					return toCopy;
				}
				return await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
			}
		}

		public async Task WriteTextAsync(string text)
		{
			using var stream = await OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders)
				?? throw new IOException("The file could not be opened for writing.");
			using var outputStream = stream.GetOutputStreamAt(0);
			using var dataWriter = new DataWriter(outputStream);
			dataWriter.WriteString(text);
			await dataWriter.StoreAsync();
			await stream.FlushAsync();
		}

		public async Task WriteBytesAsync(byte[] dataBytes)
		{
			using var stream = await OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders)
				?? throw new IOException("The file could not be opened for writing.");
			using var outputStream = stream.GetOutputStreamAt(0);
			using var dataWriter = new DataWriter(outputStream);
			dataWriter.WriteBytes(dataBytes);
			await dataWriter.StoreAsync();
			await stream.FlushAsync();
		}
	}
}
