// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class FoldersPage : Page
	{
		public FoldersPage()
		{
			InitializeComponent();
		}

		private async void ReindexVolume_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button { Tag: SearchIndexVolumeItemViewModel vol })
				await ViewModel.ReindexVolumeAsync(vol.VolumeGuid);
		}

		private async void ToggleExcludeVolume_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button { Tag: SearchIndexVolumeItemViewModel vol })
				await ViewModel.ToggleExcludeVolumeAsync(vol);
		}

		private async void RemoveExcludedPath_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button { Tag: string path })
				await ViewModel.RemoveExcludedPathAsync(path);
		}
	}
}
