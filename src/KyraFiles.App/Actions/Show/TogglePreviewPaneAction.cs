// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class TogglePreviewPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel infoPaneViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		private readonly IInfoPaneSettingsService infoPaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();
		private readonly IContentPageContext contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.TogglePreviewPane.GetLocalizedResource();

		public string Description
			=> Strings.TogglePreviewPaneDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.PanelRight");

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.Alt);

		public bool IsAccessibleGlobally
			=> true;

		public bool IsExecutable
			=> true;

		public bool IsOn
			=> infoPaneViewModel.IsEnabled && infoPaneSettingsService.SelectedTab == InfoPaneTabs.Preview;

		public TogglePreviewPaneAction()
		{
			infoPaneViewModel.PropertyChanged += InfoPaneViewModel_PropertyChanged;
			infoPaneSettingsService.PropertyChanged += InfoPaneSettingsService_PropertyChanged;
		}

		private void InfoPaneViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}

		private void InfoPaneSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IInfoPaneSettingsService.SelectedTab))
				OnPropertyChanged(nameof(IsOn));
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (parameter is "Tab")
			{
				infoPaneSettingsService.SelectedTab = InfoPaneTabs.Preview;
				return Task.CompletedTask;
			}

			var (isPaneOpen, selectedTab) = InfoPaneToggleHelper.Toggle(
				infoPaneViewModel.IsEnabled,
				infoPaneSettingsService.SelectedTab,
				InfoPaneTabs.Preview);

			infoPaneSettingsService.SelectedTab = selectedTab;
			infoPaneViewModel.IsEnabled = isPaneOpen;

			if (!isPaneOpen && contentPageContext.ShellPage is { } shellPage)
			{
				var paneHolder = shellPage.GetRequiredPaneHolder();
				paneHolder.FocusActivePane();
			}

			return Task.CompletedTask;
		}
	}
}
