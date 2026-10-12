// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenAboutAction : IAction
	{
		public string Label
			=> Strings.About.GetLocalizedResource();

		public string Description
			=> Strings.OpenAboutDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Info");

		public Task ExecuteAsync(object? parameter = null)
		{
			return NavigationHelpers.AddNewTabByParamAsync(
				typeof(ShellPanesPage),
				new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = "Settings",
					LeftPaneSelectItemParam = SettingsPageKind.AboutPage.ToString(),
				});
		}
	}
}
