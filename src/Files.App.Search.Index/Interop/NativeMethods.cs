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

	public const uint FILE_READ_ATTRIBUTES = 0x80;
	public const uint FILE_SHARE_READ = 1;
	public const uint FILE_SHARE_WRITE = 2;
	public const uint FILE_SHARE_DELETE = 4;
	public const uint OPEN_EXISTING = 3;
	public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

	public const uint FILE_NAME_NORMALIZED = 0x0;
	public const uint VOLUME_NAME_DOS = 0x0;
	public const uint VOLUME_NAME_GUID = 0x1;

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
}
