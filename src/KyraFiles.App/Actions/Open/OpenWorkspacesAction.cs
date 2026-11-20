// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.ViewModels.Dialogs;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenWorkspacesAction : ObservableObject, IAction
	{
		private readonly IDialogService _dialogService;
		private readonly INamedWorkspacesService _workspacesService;

		public string Label => "Espaços de trabalho...";

		public string Description => "Gerencia e restaura conjuntos de abas salvas (Workspaces)";

		public ActionCategory Category => ActionCategory.Open;

		public bool IsExecutable => true;

		public OpenWorkspacesAction()
		{
			_dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			_workspacesService = Ioc.Default.GetRequiredService<INamedWorkspacesService>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var vm = new WorkspacesDialogViewModel(_workspacesService);
			await _dialogService.ShowDialogAsync(vm);
		}
	}
}
