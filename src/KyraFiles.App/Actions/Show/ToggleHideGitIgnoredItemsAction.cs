// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ToggleHideGitIgnoredItemsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings;

		public string Label
			=> Strings.HideGitIgnoredItems.GetLocalizedResource();

		public string Description
			=> Strings.ToggleHideGitIgnoredItemsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public bool IsOn
			=> settings.HideGitIgnoredItems;

		public ToggleHideGitIgnoredItemsAction()
		{
			settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			settings.HideGitIgnoredItems = !settings.HideGitIgnoredItems;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.HideGitIgnoredItems))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
