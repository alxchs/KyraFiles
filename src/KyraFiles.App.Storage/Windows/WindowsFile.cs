// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

using System.IO;
using Windows.Win32.UI.Shell;

namespace KyraFiles.App.Storage
{
	[DebuggerDisplay("{" + nameof(ToString) + "()}")]
	public class WindowsFile : WindowsStorable, IWindowsFile
	{
		public WindowsFile(IShellItem ptr)
		{
			ThisPtr = ptr;
		}

		public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
