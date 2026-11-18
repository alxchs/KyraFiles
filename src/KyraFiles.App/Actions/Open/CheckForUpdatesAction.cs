// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Windows.System;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CheckForUpdatesAction : ObservableObject, IAction
	{
		private bool isChecking;

		public string Label
			=> Strings.CheckForUpdates.GetLocalizedResource();

		public string Description
			=> Strings.CheckForUpdatesDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public bool IsExecutable
			=> !isChecking;

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (isChecking)
				return;

			SetChecking(true);
			ReleaseCheckResult result;
			try
			{
				result = await ReleaseCheckService.CheckAsync();
			}
			finally
			{
				SetChecking(false);
			}

			var title = Strings.CheckForUpdates.GetLocalizedResource();

			switch (result.Status)
			{
				case ReleaseCheckStatus.Available:
					var message = string.Format(Strings.UpdateAvailableMessage.GetLocalizedResource(), result.Latest, result.Current);
					if (await DialogDisplayHelper.ShowDialogAsync(title, message, Strings.UpdateOpenDownloadPage.GetLocalizedResource(), Strings.Close.GetLocalizedResource())
						&& result.PageUrl is not null)
					{
						// F058 (FR-002): libera primeiro plano para o processo que abrira o instalador
						Windows.Win32.PInvoke.AllowSetForegroundWindow(uint.MaxValue);
						await Launcher.LaunchUriAsync(new Uri(result.PageUrl));
					}
					break;

				case ReleaseCheckStatus.UpToDate:
					await DialogDisplayHelper.ShowDialogAsync(title, string.Format(Strings.UpdateUpToDateMessage.GetLocalizedResource(), result.Current), Strings.OK.GetLocalizedResource());
					break;

				case ReleaseCheckStatus.NoReleases:
					await DialogDisplayHelper.ShowDialogAsync(title, string.Format(Strings.UpdateNoReleasesMessage.GetLocalizedResource(), result.Current), Strings.OK.GetLocalizedResource());
					break;

				default:
					await DialogDisplayHelper.ShowDialogAsync(title, Strings.UpdateCheckFailedMessage.GetLocalizedResource(), Strings.OK.GetLocalizedResource());
					break;
			}
		}

		private void SetChecking(bool value)
		{
			isChecking = value;
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
