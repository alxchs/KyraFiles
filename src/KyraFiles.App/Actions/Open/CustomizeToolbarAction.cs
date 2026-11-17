// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

using KyraFiles.App.Views.Settings;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CustomizeToolbarAction : IAction
	{
		public string Label
			=> Strings.CustomizeToolbar.GetLocalizedResource();

		public string Description
			=> Strings.CustomizeToolbarDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		// No icon needed for this action

		public Task ExecuteAsync(object? parameter = null)
		{
			ToolbarCustomizationDialog.Show();

			return Task.CompletedTask;
		}
	}
}