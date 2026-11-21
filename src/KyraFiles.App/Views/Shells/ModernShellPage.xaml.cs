// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.
#pragma warning disable CS0618 // Omnibar APIs: migrate when feature leaves experimental

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using Windows.System;

namespace KyraFiles.App.Views.Shells
{
	public sealed partial class ModernShellPage : BaseShellPage
	{
		protected override Frame ItemDisplay
			=> ItemDisplayFrame;

		private NavigationInteractionTracker _navigationInteractionTracker;

		private NavigationParams? _NavParams;
		public NavigationParams? NavParams
		{
			get => _NavParams;
			set
			{
				if (value != _NavParams)
				{
					_NavParams = value;

					if (IsLoaded)
						OnNavigationParamsChanged();
				}
			}
		}

		public ModernShellPage() : base(new CurrentInstanceViewModel())
		{
			InitializeComponent();

			ShellViewModel = new ShellViewModel(InstanceViewModel.FolderSettings);
			ShellViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
			ShellViewModel.WorkingDirectoryModified += (_, _) => _ = RefreshProjectActionsBarAsync(ShellViewModel?.WorkingDirectory);
			ShellViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
			ShellViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
			ShellViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			ShellViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
			ShellViewModel.GitDirectoryUpdated += FilesystemViewModel_GitDirectoryUpdated;
			ShellViewModel.FocusFilterHeader += ShellViewModel_FocusFilterHeader;

			ToolbarViewModel.PathControlDisplayText = Strings.Home.GetLocalizedResource();
			ToolbarViewModel.RefreshWidgetsRequested += ModernShellPage_RefreshWidgetsRequested;

			ContentChanged += ModernShellPage_ContentChanged;

			_navigationInteractionTracker = new NavigationInteractionTracker(this, BackIcon, ForwardIcon);
			_navigationInteractionTracker.NavigationRequested += OverscrollNavigationRequested;
		}

		private async void ShellViewModel_FocusFilterHeader(object? sender, EventArgs e)
		{
			// Delay to ensure the UI is ready for focus
			await Task.Delay(100);
			if (FilterTextBox?.IsLoaded ?? false)
				FilterTextBox.Focus(FocusState.Programmatic);
		}

		private void ModernShellPage_RefreshWidgetsRequested(object? sender, EventArgs e)
		{
			if (ItemDisplayFrame?.Content is HomePage currentPage)
				currentPage.ViewModel.RefreshWidgetList();
		}

		private void ModernShellPage_ContentChanged(object? sender, TabBarItemParameter e)
		{
			UpdateStatusBarProperties();
			NotifyPropertyChanged(nameof(IsStatusBarVisible));
		}

		private void UpdateStatusBarProperties()
		{
			var contentPage = SlimContentPage is ColumnsLayoutPage columnsLayoutPage
				? columnsLayoutPage.ActiveColumnShellPage?.SlimContentPage
				: SlimContentPage;

			StatusBar.StatusBarViewModel = contentPage?.StatusBarViewModel;
			StatusBar.SelectedItemsPropertiesViewModel = contentPage?.SelectedItemsPropertiesViewModel;

			// The view model's own triggers can all fire before this page's content is
			// assigned (e.g. restoring a session inside an archive), so re-evaluate the
			// ZIP encoding selector once the content page is wired up
			_ = contentPage?.StatusBarViewModel.UpdateZipEncodingStateAsync();
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			if (eventArgs.Parameter is string navPath)
				NavParams = new NavigationParams { NavPath = navPath };
			else if (eventArgs.Parameter is NavigationParams navParams)
				NavParams = navParams;
		}

		protected override void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				AssociatedTabInstance = this
			},
			new SuppressNavigationTransitionInfo());
		}

		protected override void OnNavigationParamsChanged()
		{
			var navParams = NavParams;
			if (string.IsNullOrEmpty(navParams?.NavPath) || navParams.NavPath == "Home")
			{
				NavigateHome();
			}
			else if (navParams.NavPath == "ReleaseNotes")
			{
				NavigateToReleaseNotes();
			}
			else if (navParams.NavPath == "Settings")
			{
				NavigateToSettings(navParams.SelectItem);
			}
			else
			{
				var isTagSearch = navParams.NavPath.StartsWith("tag:");

				ItemDisplayFrame.Navigate(
					InstanceViewModel.FolderSettings.GetLayoutType(navParams.NavPath),
					new NavigationArguments()
					{
						NavPathParam = navParams.NavPath,
						SelectItems = !string.IsNullOrWhiteSpace(navParams.SelectItem) ? (string[])[navParams.SelectItem] : null,
						IsSearchResultPage = isTagSearch,
						SearchPathParam = isTagSearch ? "Home" : null,
						SearchQuery = isTagSearch ? navParams.NavPath : null,
						AssociatedTabInstance = this
					});
			}
		}

		protected override async void ViewModel_WorkingDirectoryModified(object? sender, WorkingDirectoryModifiedEventArgs e)
		{
			if (e is null || string.IsNullOrWhiteSpace(e.Path))
				return;

			if (e.IsLibrary)
				await UpdatePathUIToWorkingDirectoryAsync(null, e.Name);
			else
				await UpdatePathUIToWorkingDirectoryAsync(e.Path);
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ContentPage = await GetContentOrNullAsync();

			ToolbarViewModel.UpdateAdditionalActions();
			if (ItemDisplayFrame.CurrentSourcePageType == typeof(DetailsLayoutPage) ||
				ItemDisplayFrame.CurrentSourcePageType == typeof(GridLayoutPage))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage!.ResetItemOpacity();
			}

			var parameters = (e.Parameter as NavigationArguments)!;
			var isTagSearch = parameters.NavPathParam is not null && parameters.NavPathParam.StartsWith("tag:");
			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ModernShellPage),
				NavigationParameter = parameters.IsSearchResultPage && !isTagSearch ? parameters.SearchPathParam : parameters.NavPathParam
			};

			if (parameters.IsLayoutSwitch)
				FilesystemViewModel_DirectoryInfoUpdated(sender, EventArgs.Empty);

			// Update the ShellViewModel with the current working directory
			// Fixes https://github.com/alxchs/KyraFiles/issues/17469
			if (parameters.IsSearchResultPage == false)
				ShellViewModel!.IsSearchResults = false;

			_navigationInteractionTracker.CanNavigateBackward = CanNavigateBackward;
			_navigationInteractionTracker.CanNavigateForward = CanNavigateForward;
		}

		private void OverscrollNavigationRequested(object? sender, OverscrollNavigationEventArgs e)
		{
			switch (e)
			{
				case OverscrollNavigationEventArgs.Forward:
					Forward_Click();
					break;

				case OverscrollNavigationEventArgs.Back:
					Back_Click();
					break;
			}
		}

		public override void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (!ItemDisplayFrame.CanGoBack)
				return;

			base.Back_Click();
		}

		public override void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (!ItemDisplayFrame.CanGoForward)
				return;

			base.Forward_Click();
		}

		public override void Up_Click()
		{
			if (!ToolbarViewModel.CanNavigateToParent)
				return;

			ToolbarViewModel.CanNavigateToParent = false;
			if (string.IsNullOrEmpty(ShellViewModel?.WorkingDirectory))
				return;

			bool isPathRooted = string.Equals(ShellViewModel.WorkingDirectory, PathNormalization.GetPathRoot(ShellViewModel.WorkingDirectory), StringComparison.OrdinalIgnoreCase);
			if (isPathRooted)
			{
				ItemDisplayFrame.Navigate(
					typeof(HomePage),
					new NavigationArguments()
					{
						NavPathParam = "Home",
						AssociatedTabInstance = this
					},
					new SuppressNavigationTransitionInfo());
			}
			else
			{
				string parentDirectoryOfPath = ShellViewModel.WorkingDirectory.TrimEnd('\\', '/');

				var lastSlashIndex = parentDirectoryOfPath.LastIndexOf('\\');
				if (lastSlashIndex == -1)
					lastSlashIndex = parentDirectoryOfPath.LastIndexOf('/');
				if (lastSlashIndex != -1)
					parentDirectoryOfPath = ShellViewModel.WorkingDirectory.Remove(lastSlashIndex);
				if (parentDirectoryOfPath.EndsWith(':'))
					parentDirectoryOfPath += '\\';

				SelectSidebarItemFromPath();
				ItemDisplayFrame.Navigate(
					InstanceViewModel.FolderSettings.GetLayoutType(parentDirectoryOfPath),
					new NavigationArguments()
					{
						NavPathParam = parentDirectoryOfPath,
						AssociatedTabInstance = this
					},
					new SuppressNavigationTransitionInfo());
			}
		}

		public override void Dispose()
		{
			Bindings.StopTracking();
			ContentChanged -= ModernShellPage_ContentChanged;
			ToolbarViewModel.RefreshWidgetsRequested -= ModernShellPage_RefreshWidgetsRequested;
			if (ShellViewModel is not null)
				ShellViewModel.FocusFilterHeader -= ShellViewModel_FocusFilterHeader;
			ItemDisplayFrame.Navigated -= ItemDisplayFrame_Navigated;
			_navigationInteractionTracker.NavigationRequested -= OverscrollNavigationRequested;
			_navigationInteractionTracker.Dispose();

			base.Dispose();
		}

		public override void NavigateHome()
		{
			ItemDisplayFrame.Navigate(
				typeof(HomePage),
				new NavigationArguments()
				{
					NavPathParam = "Home",
					AssociatedTabInstance = this
				},
				new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToReleaseNotes()
		{
			ItemDisplayFrame.Navigate(
				typeof(ReleaseNotesPage),
				new NavigationArguments()
				{
					NavPathParam = "ReleaseNotes",
					AssociatedTabInstance = this
				},
				new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToSettings(string? selectItem = null)
		{
			ItemDisplayFrame.Navigate(
				typeof(SettingsPage),
				new NavigationArguments()
				{
					NavPathParam = "Settings",
					SelectItems = !string.IsNullOrWhiteSpace(selectItem) ? new[] { selectItem } : null,
					AssociatedTabInstance = this
				},
				new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToPath(string? navigationPath, Type? sourcePageType, NavigationArguments? navArgs = null)
		{
			var shellViewModel = ShellViewModel!;
			shellViewModel.FilesAndFoldersFilter = null;

			if (sourcePageType is null && !string.IsNullOrEmpty(navigationPath))
				sourcePageType = InstanceViewModel.FolderSettings.GetLayoutType(navigationPath);

			if (navArgs is not null && navArgs.AssociatedTabInstance is not null)
			{
				ItemDisplayFrame.Navigate(
					sourcePageType,
					navArgs,
					new SuppressNavigationTransitionInfo());
			}
			else
			{
				if ((string.IsNullOrEmpty(navigationPath) ||
					string.IsNullOrEmpty(shellViewModel.WorkingDirectory) ||
					navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
						shellViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
						StringComparison.OrdinalIgnoreCase)) &&
					(TabBarItemParameter?.NavigationParameter is not string navArg ||
					string.IsNullOrEmpty(navArg) ||
					!navArg.StartsWith("tag:"))) // Return if already selected
				{
					if (InstanceViewModel?.FolderSettings is LayoutPreferencesManager fsModel)
						fsModel.IsLayoutModeChanging = false;

					return;
				}

				if (string.IsNullOrEmpty(navigationPath))
					return;

				ItemDisplayFrame.Navigate(
					sourcePageType,
					new NavigationArguments()
					{
						NavPathParam = navigationPath,
						AssociatedTabInstance = this
					},
					new SuppressNavigationTransitionInfo());
			}

			ToolbarViewModel.PathControlDisplayText = shellViewModel.WorkingDirectory;
		}

		private CancellationTokenSource? _projectDetectCts;
		private CancellationTokenSource? _projectRunCts;

		private void FilterTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape &&
				SlimContentPage is BaseGroupableLayoutPage { IsLoaded: true } svb)
				SlimContentPage.ItemManipulationModel.FocusFileList();
		}

		// F042: Salvar busca atual na barra lateral
		private async void SaveCurrentSearchButton_Click(object sender, RoutedEventArgs e)
		{
			var savedSearches = Ioc.Default.GetService<ISavedSearchesService>();
			var dialogService = Ioc.Default.GetService<IDialogService>();
			if (savedSearches is null || dialogService is null)
				return;

			var query = InstanceViewModel?.CurrentSearchQuery ?? string.Empty;
			var scope = ShellViewModel?.WorkingDirectory ?? string.Empty;

			// F094 FR-012: with Everything the real scope is the folder typed in the query, or the whole index.
			if (Ioc.Default.GetService<ISearchProviderFactory>()?.GetCurrentProvider() is { Id: "everything" })
			{
				var parsed = KyraFiles.Shared.Helpers.EverythingQueryScope.Parse(query, System.IO.Directory.Exists);
				query = parsed.ScopePath is null ? query : parsed.Terms;
				scope = parsed.ScopePath ?? string.Empty;
			}

			var draft = new KyraFiles.Shared.SavedSearches.SavedSearchDefinition
			{
				Name = string.IsNullOrWhiteSpace(query) ? "Busca salva" : query,
				Query = query,
				ScopePath = string.Equals(scope, "Home", StringComparison.OrdinalIgnoreCase) ? string.Empty : scope,
			};

			var vm = new KyraFiles.App.ViewModels.Dialogs.SavedSearchDialogViewModel(draft);
			if (await dialogService.ShowDialogAsync(vm) == DialogResult.Primary)
				savedSearches.Add(vm.BuildDefinition());
		}

		// F045: Detecta tipo de projeto em segundo plano (sem executar comandos automaticamente — FR-003)
		private async Task RefreshProjectActionsBarAsync(string? folderPath)
		{
			_projectDetectCts?.Cancel();
			_projectDetectCts?.Dispose();
			_projectDetectCts = new CancellationTokenSource();
			var token = _projectDetectCts.Token;

			var projectService = Ioc.Default.GetService<IProjectActionsService>();
			if (projectService is null || string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
			{
				ProjectActionBar.Visibility = Visibility.Collapsed;
				ProjectActionButtonsPanel.Children.Clear();
				return;
			}

			KyraFiles.Shared.ProjectActions.ProjectDetectionResult detection;
			try
			{
				detection = await projectService.DetectAsync(folderPath, token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			if (token.IsCancellationRequested)
				return;

			ProjectActionButtonsPanel.Children.Clear();
			var customCommands = projectService.GetCustomCommands(detection.DetectedKinds);
			if (!detection.HasProject && customCommands.Count == 0)
			{
				ProjectActionBar.Visibility = Visibility.Collapsed;
				return;
			}

			var badge = new TextBlock
			{
				Text = detection.HasProject ? $"Projeto: {string.Join(", ", detection.DetectedKinds)}" : "Comandos",
				VerticalAlignment = VerticalAlignment.Center,
				Opacity = 0.75,
				Margin = new Thickness(0, 0, 6, 0),
			};
			ProjectActionButtonsPanel.Children.Add(badge);

			foreach (var builtIn in detection.BuiltInActions)
			{
				var btn = new Button { Content = builtIn.DisplayName, Padding = new Thickness(8, 3, 8, 3) };
				var captured = builtIn;
				btn.Click += async (_, _) => await RunProjectActionAsync(folderPath, builtIn: captured, custom: null);
				ProjectActionButtonsPanel.Children.Add(btn);
			}

			foreach (var custom in customCommands)
			{
				var btn = new Button { Content = custom.Name, Padding = new Thickness(8, 3, 8, 3) };
				var captured = custom;
				btn.Click += async (_, _) => await RunProjectActionAsync(folderPath, builtIn: null, custom: captured);
				ProjectActionButtonsPanel.Children.Add(btn);
			}

			var cancelBtn = new Button
			{
				Name = "ProjectActionCancelButton",
				Content = "Cancelar",
				Padding = new Thickness(8, 3, 8, 3),
				Visibility = Visibility.Collapsed,
			};
			cancelBtn.Click += (_, _) => _projectRunCts?.Cancel();
			ProjectActionButtonsPanel.Children.Add(cancelBtn);

			var configBtn = new Button { Content = "Comandos...", Padding = new Thickness(8, 3, 8, 3) };
			configBtn.Click += async (_, _) =>
			{
				var dialogService = Ioc.Default.GetService<IDialogService>();
				if (dialogService is not null)
				{
					await dialogService.ShowDialogAsync(new KyraFiles.App.ViewModels.Dialogs.ProjectActionsDialogViewModel(projectService));
					await RefreshProjectActionsBarAsync(folderPath);
				}
			};
			ProjectActionButtonsPanel.Children.Add(configBtn);

			ProjectActionBar.Visibility = Visibility.Visible;
		}

		private async Task RunProjectActionAsync(
			string folderPath,
			KyraFiles.Shared.ProjectActions.BuiltInProjectAction? builtIn,
			KyraFiles.Shared.ProjectActions.CustomProjectCommand? custom)
		{
			var projectService = Ioc.Default.GetService<IProjectActionsService>();
			if (projectService is null)
				return;

			_projectRunCts?.Cancel();
			_projectRunCts?.Dispose();
			_projectRunCts = new CancellationTokenSource();

			var cancelBtn = ProjectActionButtonsPanel.Children.OfType<Button>().FirstOrDefault(b => b.Name == "ProjectActionCancelButton");
			if (cancelBtn is not null)
				cancelBtn.Visibility = Visibility.Visible;

			var selectedPaths = SlimContentPage?.SelectedItems?.Select(i => i.ItemPath).Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).ToList() ?? [];
			GitHelpers.IsRepositoryEx(folderPath, out var gitRoot);
			var firstSln = Directory.EnumerateFiles(folderPath, "*.sln*").FirstOrDefault();

			var ctx = new KyraFiles.Shared.ProjectActions.ProjectActionExecutionContext(folderPath, selectedPaths, gitRoot, firstSln);

			ProjectActionOutputBar.Severity = InfoBarSeverity.Informational;
			ProjectActionOutputBar.Message = $"Executando {(builtIn?.DisplayName ?? custom?.Name)}...";
			ProjectActionOutputBar.IsOpen = true;

			try
			{
				KyraFiles.Shared.ProjectActions.ProjectCommandResult finalResult;
				if (builtIn is not null)
				{
					finalResult = await projectService.RunBuiltInActionAsync(builtIn, ctx, _projectRunCts.Token);
				}
				else
				{
					var results = await projectService.RunCustomCommandAsync(custom!, ctx, _projectRunCts.Token);
					finalResult = results.LastOrDefault() ?? new KyraFiles.Shared.ProjectActions.ProjectCommandResult(0, true, false, false, [], [], TimeSpan.Zero);
				}

				if (finalResult.Cancelled)
				{
					ProjectActionOutputBar.Severity = InfoBarSeverity.Warning;
					ProjectActionOutputBar.Message = "Execucao cancelada pelo usuario.";
				}
				else if (finalResult.Succeeded)
				{
					var tail = string.Join(" | ", finalResult.OutputLines.TakeLast(3));
					ProjectActionOutputBar.Severity = InfoBarSeverity.Success;
					ProjectActionOutputBar.Message = string.IsNullOrEmpty(tail)
						? $"Concluido com sucesso ({finalResult.Duration.TotalSeconds:0.0}s)."
						: $"Concluido ({finalResult.Duration.TotalSeconds:0.0}s): {tail}";
				}
				else
				{
					var errTail = string.Join(" | ", finalResult.ErrorLines.Concat(finalResult.OutputLines).TakeLast(3));
					ProjectActionOutputBar.Severity = InfoBarSeverity.Error;
					ProjectActionOutputBar.Message = $"Falhou (codigo {finalResult.ExitCode}): {errTail}";
				}
			}
			catch (Exception ex)
			{
				ProjectActionOutputBar.Severity = InfoBarSeverity.Error;
				ProjectActionOutputBar.Message = $"Erro ao executar comando: {ex.Message}";
			}
			finally
			{
				if (cancelBtn is not null)
					cancelBtn.Visibility = Visibility.Collapsed;
			}
		}
	}
}
