// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using KyraFiles.Shared.ProjectActions;

namespace KyraFiles.App.Data.Contracts
{
	public interface IProjectActionsService
	{
		event EventHandler? CustomCommandsChanged;

		Task<ProjectDetectionResult> DetectAsync(string? folderPath, CancellationToken cancellationToken = default);

		IReadOnlyList<CustomProjectCommand> GetCustomCommands(IEnumerable<ProjectKind>? kinds = null);

		void UpsertCustomCommand(CustomProjectCommand command);

		bool RemoveCustomCommand(string id);

		(bool Ok, string? Error) ImportCustomCommandsFromJson(string json);

		string ExportCustomCommandsToJson();

		Task<ProjectCommandResult> RunBuiltInActionAsync(
			BuiltInProjectAction action,
			ProjectActionExecutionContext context,
			CancellationToken cancellationToken = default);

		Task<IReadOnlyList<ProjectCommandResult>> RunCustomCommandAsync(
			CustomProjectCommand command,
			ProjectActionExecutionContext context,
			CancellationToken cancellationToken = default);
	}
}
