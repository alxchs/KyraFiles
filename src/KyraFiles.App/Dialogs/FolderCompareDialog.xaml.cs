// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class FolderCompareDialog : ContentDialog, IDialog<FolderCompareDialogViewModel>
	{
		public FolderCompareDialogViewModel ViewModel
		{
			get => (FolderCompareDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public FolderCompareDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this);
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();
	}
}
