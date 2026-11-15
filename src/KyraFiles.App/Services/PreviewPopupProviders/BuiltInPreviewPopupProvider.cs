// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using KyraFiles.App.UserControls;
using Microsoft.Extensions.Logging;

namespace KyraFiles.App.Services.PreviewPopupProviders
{
	public sealed class BuiltInPreviewPopupProvider : IPreviewPopupProvider
	{
		public static BuiltInPreviewPopupProvider Instance { get; } = new();

		private QuickLookOverlay? activeOverlay;
		private long lastToggleTick;

		public bool IsShowing => activeOverlay?.IsShowing ?? false;

		public void RegisterOverlay(QuickLookOverlay overlay)
		{
			activeOverlay = overlay;
		}

		public void UnregisterOverlay(QuickLookOverlay overlay)
		{
			if (activeOverlay == overlay)
				activeOverlay = null;
		}

		public Task<bool> DetectAvailability()
		{
			return Task.FromResult(true);
		}

		public async Task TogglePreviewPopupAsync(string path)
		{
			var now = Environment.TickCount64;
			if (now - lastToggleTick < 350)
				return;

			lastToggleTick = now;

			if (activeOverlay is null)
				return;

			if (activeOverlay.IsShowing)
			{
				activeOverlay.Close();
			}
			else
			{
				await activeOverlay.ShowAsync(path);
			}
		}

		public async Task SwitchPreviewAsync(string path)
		{
			if (activeOverlay is null || !activeOverlay.IsShowing)
				return;

			await activeOverlay.ShowAsync(path);
		}

		public void Close()
		{
			activeOverlay?.Close();
		}
	}
}
