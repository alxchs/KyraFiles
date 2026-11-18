// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System.IO;
using KyraFiles.Shared.Helpers;
using KyraFiles.Shared.ProjectActions;
using Windows.Storage;

namespace KyraFiles.App.Services
{
	internal sealed class ProjectActionsService : IProjectActionsService
	{
		private readonly ProjectDetector _detector = new();
		private readonly ProjectCommandRunner _runner = new();
		private readonly ProjectActionsStore _store;

		public ProjectActionsService()
		{
			string? filePath = null;
			try
			{
				filePath = Path.Combine(
					ApplicationData.Current.LocalFolder.Path,
					Constants.LocalSettings.SettingsFolderName,
					"project-actions.json");
			}
			catch
			{
				// Fallback to in-memory outside packaged runtime.
			}

			_store = new ProjectActionsStore(filePath);
			_store.Load();
		}

		public event EventHandler? CustomCommandsChanged;

		public Task<ProjectDetectionResult> DetectAsync(string? folderPath, CancellationToken cancellationToken = default)
			=> _detector.DetectAsync(folderPath, cancellationToken);

		public IReadOnlyList<CustomProjectCommand> GetCustomCommands(IEnumerable<ProjectKind>? kinds = null)
			=> kinds is null ? _store.GetAll() : _store.GetForKinds(kinds);

		public void UpsertCustomCommand(CustomProjectCommand command)
		{
			_store.Upsert(command);
			CustomCommandsChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool RemoveCustomCommand(string id)
		{
			var removed = _store.Remove(id);
			if (removed)
				CustomCommandsChanged?.Invoke(this, EventArgs.Empty);
			return removed;
		}

		public (bool Ok, string? Error) ImportCustomCommandsFromJson(string json)
		{
			var res = _store.ImportFromJson(json, saveAfter: true);
			if (res.Ok)
				CustomCommandsChanged?.Invoke(this, EventArgs.Empty);
			return res;
		}

		public string ExportCustomCommandsToJson()
			=> _store.ExportToJson();

		public Task<ProjectCommandResult> RunBuiltInActionAsync(
			BuiltInProjectAction action,
			ProjectActionExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(action);
			ArgumentNullException.ThrowIfNull(context);

			var rawArgs = string.IsNullOrWhiteSpace(action.Arguments)
				? []
				: action.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			var expanded = PlaceholderExpander.ExpandArgumentTemplates(rawArgs, context);

			if (action.UsePowerShell)
			{
				var psCommand = PlaceholderExpander.BuildSafePowerShellCommand(action.Command, expanded);
				return _runner.RunAsync(
					PowerShellHelpers.PreferredExecutable,
					["-NoProfile", "-Command", psCommand],
					context.FolderPath,
					cancellationToken: cancellationToken);
			}

			return _runner.RunAsync(
				action.Command,
				expanded,
				context.FolderPath,
				cancellationToken: cancellationToken);
		}

		public async Task<IReadOnlyList<ProjectCommandResult>> RunCustomCommandAsync(
			CustomProjectCommand command,
			ProjectActionExecutionContext context,
			CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(command);
			ArgumentNullException.ThrowIfNull(context);

			var results = new List<ProjectCommandResult>();

			if (command.SelectionMode == SelectionExecutionMode.OneByOne && context.SelectedPaths.Count > 0)
			{
				foreach (var item in context.SelectedPaths)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var expanded = PlaceholderExpander.ExpandArgumentTemplates(command.Arguments, context, singleSelectedPath: item);
					results.Add(await ExecuteSingleAsync(command, expanded, context.FolderPath, cancellationToken));
				}

				return results;
			}

			var allArgs = PlaceholderExpander.ExpandArgumentTemplates(command.Arguments, context);
			results.Add(await ExecuteSingleAsync(command, allArgs, context.FolderPath, cancellationToken));
			return results;
		}

		private Task<ProjectCommandResult> ExecuteSingleAsync(
			CustomProjectCommand command,
			IReadOnlyList<string> expandedArgs,
			string workingDir,
			CancellationToken cancellationToken)
		{
			if (command.UsePowerShell)
			{
				var psCmd = PlaceholderExpander.BuildSafePowerShellCommand(command.Executable, expandedArgs);
				return _runner.RunAsync(
					PowerShellHelpers.PreferredExecutable,
					["-NoProfile", "-Command", psCmd],
					workingDir,
					cancellationToken: cancellationToken);
			}

			return _runner.RunAsync(
				command.Executable,
				expandedArgs,
				workingDir,
				cancellationToken: cancellationToken);
		}
	}
}
