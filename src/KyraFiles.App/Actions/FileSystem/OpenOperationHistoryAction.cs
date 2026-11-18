// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using KyraFiles.App.ViewModels.Dialogs;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenOperationHistoryAction : ObservableObject, IAction
	{
		private readonly IOperationHistoryService _historyService;
		private readonly IDialogService _dialogService;
		private readonly IContentPageContext _contentPageContext;

		public string Label => "Historico de operacoes";

		public string Description => "Abrir o historico de operacoes recentes com opcao de desfazer por item";

		public ActionCategory Category => ActionCategory.FileSystem;

		public RichGlyph Glyph => new(baseGlyph: "\uE81C");

		public bool IsExecutable => true;

		public OpenOperationHistoryAction()
		{
			_historyService = Ioc.Default.GetRequiredService<IOperationHistoryService>();
			_dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			_contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var vm = new OperationHistoryDialogViewModel(_historyService, _contentPageContext.ShellPage);
			await _dialogService.ShowDialogAsync(vm);
		}
	}
}
