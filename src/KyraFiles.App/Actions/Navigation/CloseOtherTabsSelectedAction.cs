// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CloseOtherTabsSelectedAction : CloseTabBaseAction
	{
		public override string Label
			=> Strings.CloseOtherTabs.GetLocalizedResource();

		public override string Description
			=> Strings.CloseOtherTabsSelectedDescription.GetLocalizedResource();

		public CloseOtherTabsSelectedAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			MultitaskingTabsHelpers.CloseOtherTabs(context.SelectedTabItem, context.Control);

			return Task.CompletedTask;
		}
	}
}
