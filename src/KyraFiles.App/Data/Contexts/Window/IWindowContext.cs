// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Contexts
{
	public interface IWindowContext : INotifyPropertyChanged
	{
		bool IsCompactOverlay { get; }

		bool IsFullScreen { get; }

		/// <inheritdoc cref="IWindowsSecurityService.IsAppElevated"/>
		bool IsRunningAsAdmin { get; }

		/// <inheritdoc cref="IWindowsSecurityService.CanDragAndDrop"/>
		bool CanDragAndDrop { get; }
	}
}
