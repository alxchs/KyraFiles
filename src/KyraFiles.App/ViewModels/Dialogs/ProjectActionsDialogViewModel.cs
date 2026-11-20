// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Collections.ObjectModel;
using KyraFiles.Shared.ProjectActions;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class ProjectActionsDialogViewModel : ObservableObject
	{
		private readonly IProjectActionsService _service;

		public ObservableCollection<CustomProjectCommand> Commands { get; } = [];

		[ObservableProperty]
		public partial string NewName { get; set; } = string.Empty;

		[ObservableProperty]
		public partial string NewExecutable { get; set; } = string.Empty;

		[ObservableProperty]
		public partial string NewArguments { get; set; } = "{caminho}";

		[ObservableProperty]
		public partial bool NewUsePowerShell { get; set; }

		[ObservableProperty]
		public partial string JsonText { get; set; } = string.Empty;

		[ObservableProperty]
		public partial string StatusMessage { get; set; } = string.Empty;

		[ObservableProperty]
		public partial bool HasStatusMessage { get; set; }

		public ProjectActionsDialogViewModel(IProjectActionsService service)
		{
			_service = service;
			Refresh();
		}

		public void Refresh()
		{
			Commands.Clear();
			foreach (var cmd in _service.GetCustomCommands())
				Commands.Add(cmd);

			JsonText = _service.ExportCustomCommandsToJson();
		}

		public void AddCommand()
		{
			if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewExecutable))
			{
				StatusMessage = "Informe nome e executavel do comando.";
				HasStatusMessage = true;
				return;
			}

			var args = string.IsNullOrWhiteSpace(NewArguments)
				? []
				: NewArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

			_service.UpsertCustomCommand(new CustomProjectCommand
			{
				Name = NewName.Trim(),
				Executable = NewExecutable.Trim(),
				Arguments = args,
				UsePowerShell = NewUsePowerShell,
			});

			NewName = string.Empty;
			NewExecutable = string.Empty;
			NewArguments = "{caminho}";
			NewUsePowerShell = false;
			StatusMessage = "Comando adicionado.";
			HasStatusMessage = true;
			Refresh();
		}

		public void RemoveCommand(string id)
		{
			if (_service.RemoveCustomCommand(id))
			{
				StatusMessage = "Comando removido.";
				HasStatusMessage = true;
				Refresh();
			}
		}

		public void ImportJson()
		{
			var (ok, err) = _service.ImportCustomCommandsFromJson(JsonText);
			StatusMessage = ok ? "Configuracao JSON importada com sucesso." : (err ?? "Erro ao importar JSON.");
			HasStatusMessage = true;
			if (ok)
				Refresh();
		}
	}
}
