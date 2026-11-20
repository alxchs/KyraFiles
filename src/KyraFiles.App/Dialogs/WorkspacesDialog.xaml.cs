// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class WorkspacesDialog : ContentDialog, IDialog<WorkspacesDialogViewModel>
	{
		public WorkspacesDialogViewModel ViewModel
		{
			get => (WorkspacesDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public WorkspacesDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this);
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();
	}
}
