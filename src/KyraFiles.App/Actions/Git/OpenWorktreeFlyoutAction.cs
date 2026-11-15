// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenWorktreeFlyoutAction : ObservableObject, IAction
	{
		public string Label
			=> Strings.Worktrees.GetLocalizedResource();

		public string Description
			=> Strings.OpenWorktreeFlyoutDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Git;

		public bool IsExecutable
			=> true;

		public Task ExecuteAsync(object? parameter = null)
		{
			return Task.CompletedTask;
		}
	}
}
