// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class FileTooLargeDialogViewModel : ObservableObject
	{
		public string[] Paths { get; private set; }

		public FileTooLargeDialogViewModel(string[] paths)
		{
			Paths = paths;
		}
	}
}
