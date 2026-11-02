// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Data.Enums;
using Files.App.Data.Items;
using Files.App.Helpers;
using Files.App.ViewModels.Settings;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Creates and manages the modal Settings window lifecycle.
	/// </summary>
	public static class SettingsDialog
	{
		private static WindowEx? settingsWindow;
		private static Frame? settingsFrame;

		[DynamicWindowsRuntimeCast(typeof(Frame))]
		public static void Show(SettingsPageKind pageKind = SettingsPageKind.GeneralPage)
		{
			var themeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();

			if (settingsWindow is not null)
			{
				if (settingsFrame?.Content is SettingsPage existingPage)
				{
					existingPage.NavigateTo(new SettingsNavigationParams { PageKind = pageKind });
				}

				settingsWindow.AppWindow.Show();
				settingsWindow.Activate();
				PInvoke.SetForegroundWindow(new HWND(settingsWindow.WindowHandle));
				return;
			}

			settingsWindow = CreateSettingsWindow(themeService, pageKind);

			themeService.SetAppThemeMode(settingsWindow, settingsWindow.AppWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false);

			CenterOnParent(settingsWindow);
			SetupModalBehavior(settingsWindow);

			settingsWindow.AppWindow.Show();
			settingsWindow.Activate();
			PInvoke.SetForegroundWindow(new HWND(settingsWindow.WindowHandle));
		}

		[DynamicWindowsRuntimeCast(typeof(Style))]
		private static WindowEx CreateSettingsWindow(IAppThemeModeService themeService, SettingsPageKind initialPageKind)
		{
			settingsFrame = new Frame { RequestedTheme = themeService.AppThemeMode };

			var rootGrid = new Grid
			{
				RequestedTheme = themeService.AppThemeMode
			};
			rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
			rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			var titleBarGrid = new Grid
			{
				Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
				Padding = new Thickness(16, 0, 160, 0),
			};

			var titleTextBlock = new TextBlock
			{
				Text = Strings.Settings.GetLocalizedResource(),
				VerticalAlignment = VerticalAlignment.Center,
				Style = Application.Current.Resources.TryGetValue("App.Theme.CaptionTextBlockStyle", out var styleObj) && styleObj is Style captionStyle ? captionStyle : null,
			};
			titleBarGrid.Children.Add(titleTextBlock);

			rootGrid.Children.Add(titleBarGrid);
			Grid.SetRow(titleBarGrid, 0);

			rootGrid.Children.Add(settingsFrame);
			Grid.SetRow(settingsFrame, 1);

			var window = new WindowEx(minWidth: 700, minHeight: 480)
			{
				ExtendsContentIntoTitleBar = true,
				IsMaximizable = true,
				Content = rootGrid,
				SystemBackdrop = new AppSystemBackdrop(true),
			};

			rootGrid.KeyDown += (s, e) =>
			{
				if (e.Key == Windows.System.VirtualKey.Escape)
				{
					window.Close();
					e.Handled = true;
				}
			};

			var appWindow = window.AppWindow;
			appWindow.Title = Strings.Settings.GetLocalizedResource();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
			appWindow.SetIcon(AppLifecycleHelper.AppIconPath);

			window.SetTitleBar(titleBarGrid);

			var width = Math.Max(1, Convert.ToInt32(960 * App.AppModel.AppWindowDPI));
			var height = Math.Max(1, Convert.ToInt32(660 * App.AppModel.AppWindowDPI));
			appWindow.Resize(new SizeInt32(width, height));

			settingsFrame.Navigate(typeof(SettingsPage), new SettingsNavigationParams { PageKind = initialPageKind }, new SuppressNavigationTransitionInfo());

			return window;
		}

		private static void CenterOnParent(WindowEx window)
		{
			var appWindow = window.AppWindow;
			var mainAppWindow = MainWindow.Instance?.AppWindow;

			if (mainAppWindow is not null)
			{
				var parentPos = mainAppWindow.Position;
				var parentSize = mainAppWindow.Size;

				int centerX = parentPos.X + (parentSize.Width - appWindow.Size.Width) / 2;
				int centerY = parentPos.Y + (parentSize.Height - appWindow.Size.Height) / 2;

				var displayArea = DisplayArea.GetFromWindowId(mainAppWindow.Id, DisplayAreaFallback.Nearest);
				if (displayArea is not null)
				{
					centerX = Math.Max(displayArea.WorkArea.X, Math.Min(displayArea.WorkArea.X + displayArea.WorkArea.Width - appWindow.Size.Width, centerX));
					centerY = Math.Max(displayArea.WorkArea.Y, Math.Min(displayArea.WorkArea.Y + displayArea.WorkArea.Height - appWindow.Size.Height, centerY));
				}

				appWindow.Move(new PointInt32(centerX, centerY));
			}
		}

		private static void SetupModalBehavior(WindowEx window)
		{
			var parentHwnd = MainWindow.Instance?.WindowHandle ?? 0;
			if (parentHwnd != 0)
			{
				PInvoke.SetWindowLongPtr(new HWND(window.WindowHandle), WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, parentHwnd);
				PInvoke.EnableWindow(new HWND(parentHwnd), false);
			}

			window.Closed += (_, _) =>
			{
				if (parentHwnd != 0)
				{
					PInvoke.EnableWindow(new HWND(parentHwnd), true);
					PInvoke.SetForegroundWindow(new HWND(parentHwnd));
					PInvoke.SetActiveWindow(new HWND(parentHwnd));
				}

				settingsWindow = null;
				settingsFrame = null;
			};
		}
	}
}

