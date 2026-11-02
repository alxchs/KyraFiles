// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;
using Files.App.ViewModels.Settings;
using Files.App.Views.Settings;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenSettingsAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.Settings.GetLocalizedResource();

		public string Description
			=> Strings.OpenSettingsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.Ctrl);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Settings");

		public Task ExecuteAsync(object? parameter = null)
		{
			var pageKind = (parameter as SettingsNavigationParams)?.PageKind ?? SettingsPageKind.GeneralPage;
			SettingsDialog.Show(pageKind);
			return Task.CompletedTask;
		}
	}
}
