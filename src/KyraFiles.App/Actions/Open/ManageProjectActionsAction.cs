// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.ViewModels.Dialogs;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ManageProjectActionsAction : ObservableObject, IAction
	{
		private readonly IProjectActionsService _projectActionsService;
		private readonly IDialogService _dialogService;

		public string Label => "Gerenciar comandos de projeto";

		public string Description => "Configurar comandos proprios e importar/exportar acoes de projeto";

		public ActionCategory Category => ActionCategory.Open;

		public RichGlyph Glyph => new(baseGlyph: "\uE756");

		public bool IsExecutable => true;

		public ManageProjectActionsAction()
		{
			_projectActionsService = Ioc.Default.GetRequiredService<IProjectActionsService>();
			_dialogService = Ioc.Default.GetRequiredService<IDialogService>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var vm = new ProjectActionsDialogViewModel(_projectActionsService);
			await _dialogService.ShowDialogAsync(vm);
		}
	}
}
