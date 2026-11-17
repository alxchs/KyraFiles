// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32.UI.Shell;

namespace KyraFiles.App.Storage
{
	public interface IWindowsFolder : IWindowsStorable, IChildFolder
	{
		/// <summary>
		/// Gets or sets the cached <see cref="IContextMenu"/> for the ShellNew context menu.
		/// </summary>
		public IContextMenu? ShellNewMenu { get; set; }
	}
}
