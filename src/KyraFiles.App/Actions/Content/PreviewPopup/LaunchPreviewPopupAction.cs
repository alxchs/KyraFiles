// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class LaunchPreviewPopupAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IPreviewPopupService previewPopupService;

		public string Label
			=> Strings.LaunchPreviewPopup.GetLocalizedResource();

		public string Description
			=> Strings.LaunchPreviewPopupDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Show;

		public HotKey HotKey
			=> new(Keys.Space);

		public bool IsExecutable =>
			context.SelectedItems.Count == 1 &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public LaunchPreviewPopupAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			previewPopupService = Ioc.Default.GetRequiredService<IPreviewPopupService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var provider = await previewPopupService.GetProviderAsync();
			if (provider is null)
				return;

			var itemPath = context.SelectedItem?.ItemPath;
			if (itemPath is not null)
				await provider.TogglePreviewPopupAsync(itemPath);
		}

		// F028: provider detection (pipe + registry) must not run on every step of a held arrow key;
		// only the last selection change of a burst switches the popup.
		private int switchVersion;
		private IPreviewPopupProvider? switchProvider;
		private long switchProviderTick = long.MinValue / 2;

		private async Task SwitchPopupPreviewAsync()
		{
			var version = Interlocked.Increment(ref switchVersion);
			await Task.Delay(150);
			if (version != Volatile.Read(ref switchVersion))
				return;

			if (IsExecutable)
			{
				// Detection probes a named pipe and the registry; remember the answer for a few seconds on this path only
				// (the explicit "toggle preview" command above always detects afresh).
				if (Environment.TickCount64 - switchProviderTick > 5000)
				{
					switchProvider = await previewPopupService.GetProviderAsync();
					switchProviderTick = Environment.TickCount64;
				}
				var provider = switchProvider;
				if (provider is null)
					return;

				var itemPath = context.SelectedItem?.ItemPath;
				if (itemPath is not null)
					await provider.SwitchPreviewAsync(itemPath);
			}
		}

		public async void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					await SwitchPopupPreviewAsync();
					break;
			}
		}
	}
}
