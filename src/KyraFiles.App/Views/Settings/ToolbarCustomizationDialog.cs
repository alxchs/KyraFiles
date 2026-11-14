// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Windows.Win32;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Creates and manages the toolbar customization window lifecycle.
	/// </summary>
	internal static class ToolbarCustomizationDialog
	{
		private static WindowEx? customizationWindow;

		public static bool IsShowing => customizationWindow is not null && customizationWindow.AppWindow.IsVisible;
		public static nint WindowHandle => customizationWindow?.WindowHandle ?? 0;
		public static XamlRoot? XamlRoot => customizationWindow?.Content?.XamlRoot;

		public static void BringToFront()
		{
			if (customizationWindow is not null)
			{
				customizationWindow.AppWindow.Show();
				customizationWindow.Activate();
				PInvoke.SetForegroundWindow(new Windows.Win32.Foundation.HWND(customizationWindow.WindowHandle));
				PInvoke.SetActiveWindow(new Windows.Win32.Foundation.HWND(customizationWindow.WindowHandle));
			}
		}

		public static void Close()
		{
			customizationWindow?.Close();
		}

		[DynamicWindowsRuntimeCast(typeof(Frame))]
		public static void Show()
		{
			if (customizationWindow is not null)
			{
				BringToFront();
				return;
			}

			var themeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();
			var window = customizationWindow = CreateCustomizationWindow(themeService);
			if (window.Content is Frame frame) frame.RequestedTheme = themeService.AppThemeMode;
			if ((window.Content as Frame)?.Content is ToolbarCustomizationPage page) window.SetTitleBar(page.TitleBarElement);
			themeService.SetAppThemeMode(window, window.AppWindow.TitleBar, themeService.AppThemeMode, callThemeModeChangedEvent: false);

			var parentHwnd = SettingsDialog.IsShowing ? SettingsDialog.WindowHandle : (MainWindow.Instance?.WindowHandle ?? 0);
			CenterOnParent(window, parentHwnd);
			SetupModalBehavior(window, parentHwnd);

			window.AppWindow.Show();
			window.Activate();
			PInvoke.SetForegroundWindow(new Windows.Win32.Foundation.HWND(window.WindowHandle));
		}

		private static void CenterOnParent(WindowEx window, nint parentHwnd)
		{
			var appWindow = window.AppWindow;
			var parentAppWindow = SettingsDialog.IsShowing && SettingsDialog.SettingsWindow is not null
				? SettingsDialog.SettingsWindow.AppWindow
				: MainWindow.Instance?.AppWindow;

			if (parentAppWindow is not null)
			{
				var parentPos = parentAppWindow.Position;
				var parentSize = parentAppWindow.Size;

				if (parentPos.X > -10000 && parentPos.Y > -10000 && parentSize.Width > 0 && parentSize.Height > 0)
				{
					int centerX = parentPos.X + (parentSize.Width - appWindow.Size.Width) / 2;
					int centerY = parentPos.Y + (parentSize.Height - appWindow.Size.Height) / 2;

					var displayArea = DisplayArea.GetFromWindowId(parentAppWindow.Id, DisplayAreaFallback.Nearest);
					if (displayArea is not null)
					{
						centerX = Math.Max(displayArea.WorkArea.X, Math.Min(displayArea.WorkArea.X + displayArea.WorkArea.Width - appWindow.Size.Width, centerX));
						centerY = Math.Max(displayArea.WorkArea.Y, Math.Min(displayArea.WorkArea.Y + displayArea.WorkArea.Height - appWindow.Size.Height, centerY));
					}

					appWindow.Move(new PointInt32(centerX, centerY));
					return;
				}
			}

			PInvoke.GetCursorPos(out var pointerPosition);
			var cursorDisplayArea = DisplayArea.GetFromPoint(new PointInt32(pointerPosition.X, pointerPosition.Y), DisplayAreaFallback.Nearest);
			if (cursorDisplayArea is not null)
			{
				appWindow.Move(new PointInt32
				{
					X = cursorDisplayArea.WorkArea.X + Math.Max(0, Math.Min(cursorDisplayArea.WorkArea.Width - appWindow.Size.Width, pointerPosition.X - cursorDisplayArea.WorkArea.X)),
					Y = cursorDisplayArea.WorkArea.Y + Math.Max(0, Math.Min(cursorDisplayArea.WorkArea.Height - appWindow.Size.Height, pointerPosition.Y - cursorDisplayArea.WorkArea.Y)),
				});
			}
		}

		private static void SetupModalBehavior(WindowEx window, nint parentHwnd)
		{
			if (parentHwnd != 0)
			{
				PInvoke.SetWindowLongPtr(new Windows.Win32.Foundation.HWND(window.WindowHandle), Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, parentHwnd);
				PInvoke.EnableWindow(new Windows.Win32.Foundation.HWND(parentHwnd), false);
			}

			window.Closed += (_, _) =>
			{
				if (parentHwnd != 0)
				{
					PInvoke.EnableWindow(new Windows.Win32.Foundation.HWND(parentHwnd), true);
					PInvoke.SetForegroundWindow(new Windows.Win32.Foundation.HWND(parentHwnd));
					PInvoke.SetActiveWindow(new Windows.Win32.Foundation.HWND(parentHwnd));
				}

				customizationWindow = null;
			};
		}

		private static WindowEx CreateCustomizationWindow(IAppThemeModeService themeService)
		{
			var frame = new Frame { RequestedTheme = themeService.AppThemeMode };
			var window = new WindowEx(460, 400)
			{
				ExtendsContentIntoTitleBar = true,
				IsMaximizable = false,
				Content = frame,
				SystemBackdrop = new AppSystemBackdrop(true),
			};
			window.Closed += (_, _) => customizationWindow = null;
			var appWindow = window.AppWindow;
			appWindow.Title = Strings.CustomizeToolbar.GetLocalizedResource();
			appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			appWindow.SetIcon(AppLifecycleHelper.AppIconPath);
			frame.Navigate(typeof(ToolbarCustomizationPage), window, new SuppressNavigationTransitionInfo());
			appWindow.Resize(new SizeInt32(
				Math.Max(1, Convert.ToInt32(760 * App.AppModel.AppWindowDPI)),
				Math.Max(1, Convert.ToInt32(560 * App.AppModel.AppWindowDPI))));
			return window;
		}
	}
}
