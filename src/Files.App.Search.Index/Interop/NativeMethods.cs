// Copyright (c) Files Community. Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Files.App.Search.Index.Interop;

internal static partial class NativeMethods
{
	public const uint DRIVE_UNKNOWN = 0;
	public const uint DRIVE_NO_ROOT_DIR = 1;
	public const uint DRIVE_REMOVABLE = 2;
	public const uint DRIVE_FIXED = 3;
	public const uint DRIVE_REMOTE = 4;
	public const uint DRIVE_CDROM = 5;
	public const uint DRIVE_RAMDISK = 6;

	public const uint FILE_LIST_DIRECTORY = 0x0001;
	public const uint FILE_READ_ATTRIBUTES = 0x80;
	public const uint FILE_SHARE_READ = 1;
	public const uint FILE_SHARE_WRITE = 2;
	public const uint FILE_SHARE_DELETE = 4;
	public const uint OPEN_EXISTING = 3;
	public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

	public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
	public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

	public const uint FILE_NAME_NORMALIZED = 0x0;
	public const uint VOLUME_NAME_DOS = 0x0;
	public const uint VOLUME_NAME_GUID = 0x1;

	public const int FileIdBothDirectoryInfo = 10;
	public const int FileIdBothDirectoryRestartInfo = 11;

	public const int THREAD_MODE_BACKGROUND_BEGIN = 0x00010000;
	public const int THREAD_MODE_BACKGROUND_END = 0x00020000;

	public const uint IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;

	[StructLayout(LayoutKind.Sequential)]
	public struct STORAGE_DEVICE_NUMBER
	{
		public uint DeviceType;
		public uint DeviceNumber;
		public uint PartitionNumber;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct FILE_ID_BOTH_DIR_INFO
	{
		public uint NextEntryOffset;
		public uint FileIndex;
		public long CreationTime;
		public long LastAccessTime;
		public long LastWriteTime;
		public long ChangeTime;
		public long EndOfFile;
		public long AllocationSize;
		public uint FileAttributes;
		public uint FileNameLength;
		public uint EaSize;
		public byte ShortNameLength;
		public unsafe fixed char ShortName[12];
		public ulong FileId;
		public char FileName; // First wchar_t of variable-length FileName
	}

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial uint QueryDosDeviceW(string lpDeviceName, Span<char> lpTargetPath, uint ucchMax);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial uint GetDriveTypeW(string lpRootPathName);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetVolumeNameForVolumeMountPointW(string lpszVolumeMountPoint, Span<char> lpszVolumeName, uint cchBufferLength);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetVolumePathNamesForVolumeNameW(string lpszVolumeName, Span<char> lpszVolumePathNames, uint cchBufferLength, out uint lpcchReturnLength);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial IntPtr FindFirstVolumeW(Span<char> lpszVolumeName, uint cchBufferLength);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool FindNextVolumeW(IntPtr hFindVolume, Span<char> lpszVolumeName, uint cchBufferLength);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool FindVolumeClose(IntPtr hFindVolume);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial SafeFileHandle CreateFileW(
		string lpFileName,
		uint dwDesiredAccess,
		uint dwShareMode,
		IntPtr lpSecurityAttributes,
		uint dwCreationDisposition,
		uint dwFlagsAndAttributes,
		IntPtr hTemplateFile);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	public static partial uint GetFinalPathNameByHandleW(
		SafeFileHandle hFile,
		Span<char> lpszFilePath,
		uint cchFilePath,
		uint dwFlags);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static unsafe partial bool GetFileInformationByHandleEx(
		SafeFileHandle hFile,
		int FileInformationClass,
		void* lpFileInformation,
		uint dwBufferSize);

	[LibraryImport("kernel32.dll")]
	public static partial IntPtr GetCurrentThread();

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool SetThreadPriority(IntPtr hThread, int nPriority);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	public static partial IntPtr FindFirstFileNameW(
		string lpFileName,
		uint dwFlags,
		ref uint StringLength,
		Span<char> LinkName);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool FindNextFileNameW(
		IntPtr hFindStream,
		ref uint StringLength,
		Span<char> LinkName);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool FindClose(IntPtr hFindFile);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static unsafe partial bool DeviceIoControl(
		SafeFileHandle hDevice,
		uint dwIoControlCode,
		void* lpInBuffer,
		uint nInBufferSize,
		void* lpOutBuffer,
		uint nOutBufferSize,
		out uint lpBytesReturned,
		IntPtr lpOverlapped);

	[LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetVolumeInformationW(
		string lpRootPathName,
		Span<char> lpVolumeNameBuffer,
		uint nVolumeNameSize,
		out uint lpVolumeSerialNumber,
		out uint lpMaximumComponentLength,
		out uint lpFileSystemFlags,
		Span<char> lpFileSystemNameBuffer,
		uint nFileSystemNameSize);

	public const uint FILE_NOTIFY_CHANGE_FILE_NAME = 0x00000001;
	public const uint FILE_NOTIFY_CHANGE_DIR_NAME = 0x00000002;
	public const uint FILE_NOTIFY_CHANGE_ATTRIBUTES = 0x00000004;
	public const uint FILE_NOTIFY_CHANGE_SIZE = 0x00000008;
	public const uint FILE_NOTIFY_CHANGE_LAST_WRITE = 0x00000010;
	public const uint FILE_NOTIFY_CHANGE_CREATION = 0x00000040;

	public const uint FILE_ACTION_ADDED = 0x00000001;
	public const uint FILE_ACTION_REMOVED = 0x00000002;
	public const uint FILE_ACTION_MODIFIED = 0x00000003;
	public const uint FILE_ACTION_RENAMED_OLD_NAME = 0x00000004;
	public const uint FILE_ACTION_RENAMED_NEW_NAME = 0x00000005;

	public const int ReadDirectoryNotifyExtendedInformation = 2;
	public const int ERROR_NOTIFY_ENUM_DIR = 1022;
	public const int ERROR_OPERATION_ABORTED = 995;
	public const int ERROR_IO_PENDING = 997;
	public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

	[StructLayout(LayoutKind.Sequential)]
	public struct FILE_NOTIFY_EXTENDED_INFORMATION
	{
		public uint NextEntryOffset;
		public uint Action;
		public long CreationTime;
		public long LastModificationTime;
		public long LastChangeTime;
		public long LastAccessTime;
		public long AllocatedLength;
		public long FileSize;
		public uint FileAttributes;
		public uint ReparsePointTag;
		public ulong FileId;
		public ulong ParentFileId;
		public uint FileNameLength;
		public char FileName;
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static unsafe partial bool ReadDirectoryChangesExW(
		SafeFileHandle hDirectory,
		void* lpBuffer,
		uint nBufferLength,
		[MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
		uint dwNotifyFilter,
		out uint lpBytesReturned,
		NativeOverlapped* lpOverlapped,
		IntPtr lpCompletionRoutine,
		int ReadDirectoryNotifyInformationClass);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static unsafe partial bool GetOverlappedResult(
		SafeFileHandle hFile,
		NativeOverlapped* lpOverlapped,
		out uint lpNumberOfBytesTransferred,
		[MarshalAs(UnmanagedType.Bool)] bool bWait);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool CancelIoEx(SafeFileHandle hFile, IntPtr lpOverlapped);
}

