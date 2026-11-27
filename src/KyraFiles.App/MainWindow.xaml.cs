// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using KyraFiles.App.Helpers;
using KyraFiles.Shared.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.Win32.Foundation;
using WinRT;
using IO = System.IO;

namespace KyraFiles.App
{
	public sealed partial class MainWindow : WindowEx
	{
		private static MainWindow? _Instance;
		public static MainWindow Instance => _Instance ??= new();

		private bool CanWindowToFront { get; set; } = true;
		private readonly Lock _canWindowToFrontLock = new();

		protected override bool PersistPlacement => true;

		public MainWindow() : base(416, 316)
		{
			InitializeComponent();

			ExtendsContentIntoTitleBar = true;
			Title = "KyraFiles";
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;

			// Deferred: reads the .ico from disk
			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
				AppWindow.SetIcon(AppLifecycleHelper.AppIconPath));

			AppWindow.Changed += AppWindow_Changed;
			AppWindow.Closing += AppWindow_Closing;
			Activated += MainWindow_Activated;
		}

		public void ShowSplashScreen()
		{
			var rootFrame = EnsureWindowIsInitialized();

			rootFrame?.Navigate(typeof(SplashScreenPage));
		}

		[DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
		public async Task InitializeApplicationAsync(object? activatedEventArgs)
		{
			var rootFrame = EnsureWindowIsInitialized();

			if (rootFrame is null)
				return;

			// Reuse the existing backdrop on resume; rebuilding the Mica controller leaves the window on the fallback color for ~1s
			SystemBackdrop ??= new AppSystemBackdrop();

			switch (activatedEventArgs)
			{
				case ILaunchActivatedEventArgs plainLaunchArgs when string.IsNullOrWhiteSpace(plainLaunchArgs.Arguments)
					&& rootFrame.Content is not null && rootFrame.Content is not SplashScreenPage && MainPageViewModel.AppInstances.Any():
					// F086: a second plain launch only brings the running window forward
					Win32Helper.BringToForegroundEx(new(WindowHandle));
					break;

				case ILaunchActivatedEventArgs launchArgs:
					if (launchArgs.Arguments is not null &&
						(CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"kyrafiles.exe", StringComparison.OrdinalIgnoreCase)
						|| CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith(ProtocolUriParser.ProtocolScheme, StringComparison.OrdinalIgnoreCase)
						|| CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].Equals(Path.Join(Package.Current.InstalledLocation.Path, "KyraFiles.exe"), StringComparison.OrdinalIgnoreCase)))
					{
						// WINUI3: When launching from commandline the argument is not ICommandLineActivatedEventArgs (#10370)
						var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
						if (ppm.IsEmpty())
							rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
						else
							await InitializeFromCmdLineArgsAsync(rootFrame, ppm, Program.ConsumeLaunchCwd());
					}
					else if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						rootFrame.Navigate(typeof(MainPage), launchArgs.Arguments, new SuppressNavigationTransitionInfo());
					}
					else if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances.Count > 0))
					{
						// Bring to foreground (#14730)
						Win32Helper.BringToForegroundEx(new(WindowHandle));

						await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), launchArgs.Arguments, true);
					}
					else
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					}
					break;

				case IProtocolActivatedEventArgs eventArgs:
					var launchInfo = ProtocolUriParser.Parse(eventArgs.Uri);
					if (launchInfo.Action == ProtocolAction.Home || string.IsNullOrWhiteSpace(launchInfo.TargetValue))
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());

						// Bring to foreground (#14730)
						Win32Helper.BringToForegroundEx(new(WindowHandle));

						// Ensure app-level keyboard shortcuts work immediately after Win+E activation.
						_ = EnsureContentHasKeyboardFocusAsync();
					}
					else
					{
						string unescapedValue = launchInfo.TargetValue;
						if (launchInfo.Action == ProtocolAction.Tab && launchInfo.MoveDx is int dx && launchInfo.MoveDy is int dy)
						{
							AppWindow?.Move(new(dx - 100, dy - 16));
						}

						var folderResult = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(unescapedValue).AsTask());
						if (folderResult.Result is { } folder && !string.IsNullOrEmpty(folder.Path))
						{
							// Convert short name to long name (#6190)
							unescapedValue = folder.Path;
						}
						switch (launchInfo.Action)
						{
							case ProtocolAction.Tab:
								rootFrame.Navigate(typeof(MainPage),
									new MainPageNavigationArguments() { Parameter = TabBarItemParameter.Deserialize(unescapedValue), IgnoreStartupSettings = true },
									new SuppressNavigationTransitionInfo());
								break;

							case ProtocolAction.Folder:
								rootFrame.Navigate(typeof(MainPage),
									new MainPageNavigationArguments() { Parameter = unescapedValue, IgnoreStartupSettings = true },
									new SuppressNavigationTransitionInfo());
								break;

							case ProtocolAction.Cmd:
								var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
								if (ppm.IsEmpty())
									rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
								else
									await InitializeFromCmdLineArgsAsync(rootFrame, ppm, Environment.CurrentDirectory);
								break;
							default:
								rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
								break;
						}

						MainWindow.Instance.Activate();
						Win32Helper.BringToForegroundEx(new(WindowHandle));
						_ = EnsureContentHasKeyboardFocusAsync();
					}
					break;

				case ICommandLineActivatedEventArgs cmdLineArgs:
					var operation = cmdLineArgs.Operation;
					var cmdLineString = operation.Arguments;
					var activationPath = operation.CurrentDirectoryPath;

					var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);
					if (parsedCommands is not null && parsedCommands.Count > 0)
					{
						await InitializeFromCmdLineArgsAsync(rootFrame, parsedCommands, activationPath);
					}
					else
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					}
					break;

				case IFileActivatedEventArgs fileArgs:
					try
					{
						if (fileArgs.Files is null || fileArgs.Files.Count == 0)
						{
							break;
						}

						var index = 0;
						if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
						{
							// When the navigation stack isn't restored navigate to the first page,
							// configuring the new page by passing required information as a navigation parameter
							rootFrame.Navigate(typeof(MainPage), fileArgs.Files.First().Path, new SuppressNavigationTransitionInfo());
							index = 1;
						}
						else
						{
							// Bring to foreground (#14730)
							Win32Helper.BringToForegroundEx(new(WindowHandle));
						}

						for (; index < fileArgs.Files.Count; index++)
						{
							await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), fileArgs.Files[index].Path, true);
						}
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, "Failed to open files.");
						if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
							rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
						else
							Win32Helper.BringToForegroundEx(new(WindowHandle));
					}
					break;

				case IStartupTaskActivatedEventArgs startupArgs:
					// Just launch the app with no arguments
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					break;

				default:
					// Just launch the app with no arguments
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					break;
			}

			ShowAndRestoreWindow();
		}

		/// <summary>
		/// KyraFiles: opens a location taken over from an Explorer window (F012 T9).
		/// </summary>
		public async Task OpenFromExplorerAsync(string path, string? selectItem)
		{
			// Wake the instance parked in the background; its window is shown below
			SafetyExtensions.IgnoreExceptions(() => Program.Pool?.Release());

			var rootFrame = EnsureWindowIsInitialized();
			if (rootFrame is null)
				return;

			SystemBackdrop ??= new AppSystemBackdrop();

			// SelectItem alone opens the parent folder with the item selected
			var commands = string.IsNullOrEmpty(selectItem)
				? new ParsedCommands { new() { Type = ParsedCommandType.OpenPath, Args = [path] } }
				: new ParsedCommands { new() { Type = ParsedCommandType.SelectItem, Args = [selectItem] } };

			await InitializeFromCmdLineArgsAsync(rootFrame, commands);
			ShowAndRestoreWindow();
		}

		[DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
		private void ShowAndRestoreWindow()
		{
			// KyraFiles (F035 AC-5): acorda o semaforo de estacionamento caso a instancia tenha sido reativada via AppInstance
			SafetyExtensions.IgnoreExceptions(() => Program.Pool?.Release());

			var appWindow = AppWindow;
			if (appWindow is null)
				return;

			if (!appWindow.IsVisible)
				appWindow.Show();

			if (Windows.Win32.PInvoke.IsIconic(new(WindowHandle)) && appWindow.Presenter is OverlappedPresenter overlapped)
				overlapped.Restore();

			Activate();
			Win32Helper.BringToForegroundEx(new(WindowHandle));
		}

		private async Task EnsureContentHasKeyboardFocusAsync()
		{
			await Task.Delay(100);
			var shellPage = Ioc.Default.GetService<IContentPageContext>()?.ShellPage;
			if (shellPage is not null)
			{
				var paneHolder = shellPage.GetRequiredPaneHolder();
				paneHolder.FocusActivePane();
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Frame))]
		private Frame? EnsureWindowIsInitialized()
		{
			try
			{
				// NOTE:
				//  Do not repeat app initialization when the Window already has content,
				//  just ensure that the window is active
				if (Instance.Content is not Frame rootFrame)
				{
					// Create a Frame to act as the navigation context and navigate to the first page
					rootFrame = new() { CacheSize = 1 };
					rootFrame.NavigationFailed += (s, e) =>
					{
						throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
					};

					// Place the frame in the current Window
					Instance.Content = rootFrame;
					Ioc.Default.GetRequiredService<IUiScaleService>().ApplyScale(Instance, rootFrame);
				}

				return rootFrame;
			}
			catch (COMException)
			{
				return null;
			}
		}

		private async Task InitializeFromCmdLineArgsAsync(Frame rootFrame, ParsedCommands parsedCommands, string activationPath = "")
		{
			async Task PerformNavigationAsync(string? payload, string? selectItem = null)
			{
				if (!string.IsNullOrEmpty(payload))
				{
					payload = ShellHelpers.ResolveShellPath(payload);
					var folderResult = await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(payload).AsTask());
					if (folderResult.Result is { } folder && !string.IsNullOrEmpty(folder.Path))
						payload = folder.Path; // Convert short name to long name (#6190)
				}

				var generalSettingsService = Ioc.Default.GetService<IGeneralSettingsService>();

				double boundsWidth = 0;
				try
				{
					boundsWidth = Bounds.Width;
				}
				catch (Exception ex)
				{
					// Handle exception in case WinUI Windows is closed
					// (see https://github.com/alxchs/KyraFiles/issues/15599)

					App.Logger.LogWarning(ex, ex.Message);
					return;
				}

				var paneNavigationArgs = new PaneNavigationArguments
				{
					LeftPaneNavPathParam = payload,
					LeftPaneSelectItemParam = selectItem,
					RightPaneNavPathParam = boundsWidth > Constants.UI.MultiplePaneWidthThreshold && (generalSettingsService?.AlwaysOpenDualPaneInNewTab ?? false) ? "Home" : null,
				};

				if (rootFrame.Content is MainPage)
				{
					// F035 (AC-1/AC-2): se MainPage acabou de navegar e ainda cria a primeira aba, aguarda StartupTabsLoadedTask
					// antes de decidir entre AddNewTabByParamAsync e Navigate(MainPage), evitando sobrescrever abas concorrentes.
					if (!MainPageViewModel.AppInstances.Any())
						await MainPageViewModel.StartupTabsLoadedTask.WithTimeoutAsync(TimeSpan.FromSeconds(5));

					if (MainPageViewModel.AppInstances.Any())
					{
						// Bring to foreground (#14730)
						Win32Helper.BringToForegroundEx(new(WindowHandle));

						var existingTabIndex = MainPageViewModel.AppInstances
							.Select((tabItem, idx) => new { tabItem, idx })
							.FirstOrDefault(x =>
								x.tabItem.NavigationParameter!.NavigationParameter is PaneNavigationArguments paneArgs &&
								(paneNavigationArgs.LeftPaneNavPathParam == paneArgs.LeftPaneNavPathParam ||
								paneNavigationArgs.LeftPaneNavPathParam == paneArgs.RightPaneNavPathParam))?.idx ?? -1;

						if (existingTabIndex >= 0)
							App.AppModel.TabStripSelectedIndex = existingTabIndex;
						else
							await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), paneNavigationArgs);
					}
					else
						rootFrame.Navigate(typeof(MainPage), paneNavigationArgs, new SuppressNavigationTransitionInfo());
				}
				else
					rootFrame.Navigate(typeof(MainPage), paneNavigationArgs, new SuppressNavigationTransitionInfo());
			}

			// KyraFiles: a file dialog instance must know it is one before its first tab opens (F012 T7)
			if (parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.OutputPath) is { } outputPathCommand)
			{
				App.OutputPath = outputPathCommand.Payload;
				ExplorerWindowRedirector.Stop();
			}

			foreach (var command in parsedCommands)
			{
				switch (command.Type)
				{
					case ParsedCommandType.OpenDirectory:
					case ParsedCommandType.OpenPath:
					case ParsedCommandType.ExplorerShellCommand:
						var selectItemCommand = parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.SelectItem);
						await PerformNavigationAsync(command.Payload, selectItemCommand?.Payload);
						break;

					case ParsedCommandType.SelectItem:
						if (IO.Path.IsPathRooted(command.Payload))
							await PerformNavigationAsync(IO.Path.GetDirectoryName(command.Payload), IO.Path.GetFileName(command.Payload));
						break;

					case ParsedCommandType.TagFiles:
						var tagService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();
						var tag = tagService.GetTagsByName(command.Payload).FirstOrDefault();
						foreach (var file in command.Args.Skip(1))
						{
							var fileFRN = await FilesystemTasks.Wrap(() => StorageHelpers.ToStorageItem<IStorageItem>(file))
								.OnSuccess(item => FileTagsHelper.GetFileFRN(item));
							if (fileFRN is not null)
							{
								var tagUid = tag is not null ? new[] { tag.Uid } : [];
								var dbInstance = FileTagsHelper.GetDbInstance();
								dbInstance.SetTags(file, fileFRN, tagUid);
								await FileTagsHelper.WriteFileTagAsync(file, tagUid);
							}
						}
						break;

					case ParsedCommandType.Unknown:
						if (command.Payload.Equals("."))
						{
							await PerformNavigationAsync(activationPath);
						}
						else
						{
							if (!string.IsNullOrEmpty(command.Payload))
							{
								var target = IO.Path.GetFullPath(IO.Path.Combine(activationPath, command.Payload));
								await PerformNavigationAsync(target);
							}
							else
							{
								await PerformNavigationAsync(null);
							}
						}
						break;

					case ParsedCommandType.OutputPath:
						App.OutputPath = command.Payload;
						break;
				}
			}
		}

		public bool SetCanWindowToFront(bool canWindowToFront)
		{
			lock (_canWindowToFrontLock)
			{
				if (CanWindowToFront != canWindowToFront)
				{
					CanWindowToFront = canWindowToFront;
					return true;
				}
				return false;
			}
		}

		protected override bool OnWindowMessageReceived(uint message, WPARAM wParam, LPARAM lParam, ref LRESULT result)
		{
			if (Views.Settings.ToolbarCustomizationDialog.IsShowing)
			{
				if (message == Windows.Win32.PInvoke.WM_MOUSEACTIVATE || message == Windows.Win32.PInvoke.WM_NCACTIVATE)
				{
					Views.Settings.ToolbarCustomizationDialog.BringToFront();
					result = new(3 /* MA_NOACTIVATEANDEAT */);
					return true;
				}
			}
			else if (Views.Settings.SettingsDialog.IsShowing)
			{
				if (message == Windows.Win32.PInvoke.WM_MOUSEACTIVATE || message == Windows.Win32.PInvoke.WM_NCACTIVATE)
				{
					Views.Settings.SettingsDialog.BringToFront();
					result = new(3 /* MA_NOACTIVATEANDEAT */);
					return true;
				}
			}

			if ((!CanWindowToFront) && message == Windows.Win32.PInvoke.WM_WINDOWPOSCHANGING)
			{
				Win32Helper.ForceWindowPosition(lParam.Value);
				return true;
			}
			else if (message == Windows.Win32.PInvoke.WM_MENUCHAR && (wParam.Value & 0xFFFF) == '\r')
			{
				result = new(Win32PInvoke.MNC_CLOSE << 16);
				return true;
			}

			return false;
		}
		[DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
		private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
		{
			if (args.DidPresenterChange && sender.Presenter is OverlappedPresenter overlapped)
			{
				if (overlapped.State == OverlappedPresenterState.Minimized)
				{
					// Window was minimized: schedule soft working set trim after 2 seconds
					MemoryTrimHelper.ScheduleTrim(delayMs: 2000, forceFullGc: false);
				}
				else
				{
					// Window was restored/maximized: cancel any pending trim immediately
					MemoryTrimHelper.CancelPendingTrim();
				}
			}
			else if (args.DidVisibilityChange && !sender.IsVisible)
			{
				MemoryTrimHelper.ScheduleTrim(delayMs: 2000, forceFullGc: false);
			}
			else if (args.DidVisibilityChange && sender.IsVisible)
			{
				MemoryTrimHelper.CancelPendingTrim();
			}
		}

		private void MainWindow_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
		{
			// A visible window is never trimmed (F063, Q1): losing focus schedules nothing,
			// and returning to the window only cancels a trim scheduled by minimize/hide
			if (args.WindowActivationState != Microsoft.UI.Xaml.WindowActivationState.Deactivated)
				MemoryTrimHelper.CancelPendingTrim();
		}

		private bool _forceClose;

		private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
		{
			if (_forceClose)
				return;

			var statusCenterViewModel = Ioc.Default.GetService<StatusCenterViewModel>();
			if (statusCenterViewModel is null)
				return;

			var activeOrQueuedItems = statusCenterViewModel.StatusCenterItems.Where(i => i.IsInProgress || i.IsQueued).ToList();
			if (activeOrQueuedItems.Count == 0)
				return;

			args.Cancel = true;

			if (Content?.XamlRoot is null)
			{
				_forceClose = true;
				AppWindow.Destroy();
				return;
			}

			var dialog = new ContentDialog
			{
				Title = Strings.ClosingWithActiveOperationsTitle.GetLocalizedResource(),
				Content = Strings.ClosingWithActiveOperationsMessage.GetLocalizedResource(),
				PrimaryButtonText = Strings.ClosingWithActiveOperationsWait.GetLocalizedResource(),
				SecondaryButtonText = Strings.ClosingWithActiveOperationsCancel.GetLocalizedResource(),
				CloseButtonText = Strings.ClosingWithActiveOperationsCloseAnyway.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = Content.XamlRoot
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				// Wait: user chose to keep window open and let operations finish
				return;
			}
			else if (result == ContentDialogResult.Secondary)
			{
				// Cancel operations and close
				foreach (var item in activeOrQueuedItems)
				{
					item.ExecuteCancelCommand();
				}

				_forceClose = true;
				AppWindow.Destroy();
			}
			else
			{
				// Close anyway
				_forceClose = true;
				AppWindow.Destroy();
			}
		}
	}
}
