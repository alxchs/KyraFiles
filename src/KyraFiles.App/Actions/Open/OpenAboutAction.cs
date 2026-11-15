// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Enums;
using KyraFiles.App.Views.Settings;

namespace KyraFiles.App.Actions
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
			SettingsDialog.Show(SettingsPageKind.AboutPage);
			return Task.CompletedTask;
		}
	}
}
