using Files.App.Data.Enums;
using Files.App.Services.Search;

namespace Files.App.ViewModels.Settings
{
	public sealed partial class SearchIndexVolumeItemViewModel : ObservableObject
	{
		public Guid VolumeGuid { get; set; }
		public string DisplayRoot { get; set; } = string.Empty;
		public string FileSystem { get; set; } = string.Empty;
		public string ModeDisplay { get; set; } = string.Empty;
		public string ItemCountDisplay { get; set; } = string.Empty;
		public string StateDisplay { get; set; } = string.Empty;
		public bool IsExcluded { get; set; }

		public string TitleLine => $"{DisplayRoot} ({FileSystem})";
		public string DetailLine => $"Modo {ModeDisplay} • {ItemCountDisplay} • {StateDisplay}";
		public string ToggleExcludeLabel => IsExcluded ? "Incluir volume" : "Excluir volume";
	}

	public sealed partial class FoldersViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IndexedSearchProvider? IndexedSearchProvider => Ioc.Default.GetService<IndexedSearchProvider>();

		public Dictionary<SizeUnitTypes, string> SizeUnitsOptions { get; private set; } = [];
		public Dictionary<SingleClickOpenMode, string> SingleClickOpenModeOptions { get; private set; } = [];

		public ObservableCollection<SearchIndexVolumeItemViewModel> SearchIndexVolumes { get; } = [];
		public ObservableCollection<string> SearchIndexExcludedPaths { get; } = [];

		public IAsyncRelayCommand RefreshSearchIndexStatusCommand { get; }
		public IAsyncRelayCommand ReindexAllVolumesCommand { get; }
		public IAsyncRelayCommand<Guid> ReindexVolumeCommand { get; }
		public IAsyncRelayCommand<SearchIndexVolumeItemViewModel> ToggleExcludeVolumeCommand { get; }
		public IAsyncRelayCommand AddExcludedPathCommand { get; }
		public IAsyncRelayCommand<string> RemoveExcludedPathCommand { get; }

		public FoldersViewModel()
		{
			SelectedDeleteConfirmationPolicyIndex = (int)DeleteConfirmationPolicy;

			// Size unit format
			SizeUnitsOptions.Add(SizeUnitTypes.BinaryUnits, Strings.Binary.GetLocalizedResource());
			SizeUnitsOptions.Add(SizeUnitTypes.DecimalUnits, Strings.Decimal.GetLocalizedResource());
			SizeUnitFormat = SizeUnitsOptions[UserSettingsService.FoldersSettingsService.SizeUnitFormat];

			SingleClickOpenModeOptions.Add(SingleClickOpenMode.Never, Strings.Never.GetLocalizedResource());
			SingleClickOpenModeOptions.Add(SingleClickOpenMode.OnlyForTouch, Strings.OnlyForTouch.GetLocalizedResource());
			SingleClickOpenModeOptions.Add(SingleClickOpenMode.OnlyForMouse, Strings.OnlyForMouse.GetLocalizedResource());
			SingleClickOpenModeOptions.Add(SingleClickOpenMode.Always, Strings.Always.GetLocalizedResource());
			SelectedOpenFilesWithSingleClickOption = SingleClickOpenModeOptions[UserSettingsService.FoldersSettingsService.OpenFilesWithSingleClick];
			SelectedOpenFoldersWithSingleClickOption = SingleClickOpenModeOptions[UserSettingsService.FoldersSettingsService.OpenFoldersWithSingleClick];
			SelectedOpenFoldersInColumnsViewWithSingleClickOption = SingleClickOpenModeOptions[UserSettingsService.FoldersSettingsService.OpenFoldersInColumnsViewWithSingleClick];

			var savedExcluded = UserSettingsService.FoldersSettingsService.SearchIndexExcludedPaths;
			if (savedExcluded is not null)
			{
				foreach (var p in savedExcluded.Where(x => !string.IsNullOrWhiteSpace(x)))
					SearchIndexExcludedPaths.Add(p);
			}

			RefreshSearchIndexStatusCommand = new AsyncRelayCommand(RefreshSearchIndexStatusAsync);
			ReindexAllVolumesCommand = new AsyncRelayCommand(() => ReindexVolumeAsync(null));
			ReindexVolumeCommand = new AsyncRelayCommand<Guid>(guid => ReindexVolumeAsync(guid));
			ToggleExcludeVolumeCommand = new AsyncRelayCommand<SearchIndexVolumeItemViewModel>(ToggleExcludeVolumeAsync);
			AddExcludedPathCommand = new AsyncRelayCommand(AddExcludedPathAsync);
			RemoveExcludedPathCommand = new AsyncRelayCommand<string>(RemoveExcludedPathAsync);

			_ = RefreshSearchIndexStatusAsync();
		}

		// Properties



		private int selectedDeleteConfirmationPolicyIndex;
		public int SelectedDeleteConfirmationPolicyIndex
		{
			get => selectedDeleteConfirmationPolicyIndex;
			set
			{
				if (SetProperty(ref selectedDeleteConfirmationPolicyIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDeleteConfirmationPolicyIndex));
					DeleteConfirmationPolicy = (DeleteConfirmationPolicies)value;
				}
			}
		}

		public bool ShowHiddenItems
		{
			get => UserSettingsService.FoldersSettingsService.ShowHiddenItems;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowHiddenItems)
				{
					UserSettingsService.FoldersSettingsService.ShowHiddenItems = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowProtectedSystemFiles
		{
			get => UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles)
				{
					UserSettingsService.FoldersSettingsService.ShowProtectedSystemFiles = value;

					OnPropertyChanged();
				}
			}
		}

		public bool AreAlternateStreamsVisible
		{
			get => UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
				{
					UserSettingsService.FoldersSettingsService.AreAlternateStreamsVisible = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDotFiles
		{
			get => UserSettingsService.FoldersSettingsService.ShowDotFiles;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowDotFiles)
				{
					UserSettingsService.FoldersSettingsService.ShowDotFiles = value;

					OnPropertyChanged();
				}
			}
		}

		private string selectedOpenFilesWithSingleClickOption = null!;
		public string SelectedOpenFilesWithSingleClickOption
		{
			get => selectedOpenFilesWithSingleClickOption;
			set
			{
				if (SetProperty(ref selectedOpenFilesWithSingleClickOption, value))
				{
					UserSettingsService.FoldersSettingsService.OpenFilesWithSingleClick = SingleClickOpenModeOptions.First(e => e.Value == value).Key;
				}
			}
		}

		private string selectedOpenFoldersWithSingleClickOption = null!;
		public string SelectedOpenFoldersWithSingleClickOption
		{
			get => selectedOpenFoldersWithSingleClickOption;
			set
			{
				if (SetProperty(ref selectedOpenFoldersWithSingleClickOption, value))
				{
					UserSettingsService.FoldersSettingsService.OpenFoldersWithSingleClick = SingleClickOpenModeOptions.First(e => e.Value == value).Key;
				}
			}
		}

		private string selectedOpenFoldersInColumnsViewWithSingleClickOption = null!;
		public string SelectedOpenFoldersInColumnsViewWithSingleClickOption
		{
			get => selectedOpenFoldersInColumnsViewWithSingleClickOption;
			set
			{
				if (SetProperty(ref selectedOpenFoldersInColumnsViewWithSingleClickOption, value))
				{
					UserSettingsService.FoldersSettingsService.OpenFoldersInColumnsViewWithSingleClick = SingleClickOpenModeOptions.First(e => e.Value == value).Key;
				}
			}
		}

		public bool OpenFoldersNewTab
		{
			get => UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab)
				{
					UserSettingsService.FoldersSettingsService.OpenFoldersInNewTab = value;

					OnPropertyChanged();
				}
			}
		}

		public bool CalculateFolderSizes
		{
			get => UserSettingsService.FoldersSettingsService.CalculateFolderSizes;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.CalculateFolderSizes)
				{
					UserSettingsService.FoldersSettingsService.CalculateFolderSizes = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ScrollToPreviousFolderWhenNavigatingUp
		{
			get => UserSettingsService.FoldersSettingsService.ScrollToPreviousFolderWhenNavigatingUp;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ScrollToPreviousFolderWhenNavigatingUp)
				{
					UserSettingsService.FoldersSettingsService.ScrollToPreviousFolderWhenNavigatingUp = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileExtensions
		{
			get => UserSettingsService.FoldersSettingsService.ShowFileExtensions;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					UserSettingsService.FoldersSettingsService.ShowFileExtensions = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowThumbnails
		{
			get => UserSettingsService.FoldersSettingsService.ShowThumbnails;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowThumbnails)
				{
					UserSettingsService.FoldersSettingsService.ShowThumbnails = value;

					OnPropertyChanged();
				}
			}
		}

		public DeleteConfirmationPolicies DeleteConfirmationPolicy
		{
			get => UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy)
				{
					UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy = value;

					OnPropertyChanged();
				}
			}
		}

		public bool SelectFilesOnHover
		{
			get => UserSettingsService.FoldersSettingsService.SelectFilesOnHover;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				{
					UserSettingsService.FoldersSettingsService.SelectFilesOnHover = value;

					OnPropertyChanged();
				}
			}
		}

		public bool DoubleClickToGoUp
		{
			get => UserSettingsService.FoldersSettingsService.DoubleClickToGoUp;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				{
					UserSettingsService.FoldersSettingsService.DoubleClickToGoUp = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileExtensionWarning
		{
			get => UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning)
				{
					UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowCheckboxesWhenSelectingItems
		{
			get => UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems;
			set
			{
				if (value != UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems)
				{
					UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems = value;

					OnPropertyChanged();
				}
			}
		}

		private string sizeUnitFormat = null!;
		public string SizeUnitFormat
		{
			get => sizeUnitFormat;
			set
			{
				if (SetProperty(ref sizeUnitFormat, value))
				{
					UserSettingsService.FoldersSettingsService.SizeUnitFormat = SizeUnitsOptions.First(e => e.Value == value).Key;
				}
			}
		}

		private string searchIndexServiceSummary = "Consultando serviço de índice...";
		public string SearchIndexServiceSummary
		{
			get => searchIndexServiceSummary;
			set => SetProperty(ref searchIndexServiceSummary, value);
		}

		private string newExcludedPathText = string.Empty;
		public string NewExcludedPathText
		{
			get => newExcludedPathText;
			set => SetProperty(ref newExcludedPathText, value);
		}

		public async Task RefreshSearchIndexStatusAsync()
		{
			var provider = IndexedSearchProvider;
			if (provider is null)
			{
				SearchIndexServiceSummary = "Provedor de índice não registrado";
				return;
			}

			try
			{
				bool alive = await provider.EnsureReadyAndConfiguredAsync();
				if (!alive)
				{
					SearchIndexServiceSummary = "Serviço de índice indisponível (usando busca direta de reserva)";
					return;
				}

				var status = await provider.Client.GetStatusAsync();
				if (status is null)
				{
					SearchIndexServiceSummary = "Não foi possível obter o estado do serviço de índice";
					return;
				}

				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					string modePt = status.Mode == "Full" ? "Completo (MFT + USN)" : "Básico (sem administrador)";
					int totalItems = status.Volumes?.Sum(v => v.ItemCount) ?? 0;
					SearchIndexServiceSummary = $"Modo {modePt} ativo (PID {status.ServerProcessId}) • {totalItems:N0} itens indexados";

					SearchIndexVolumes.Clear();
					if (status.Volumes is not null)
					{
						foreach (var vol in status.Volumes)
						{
							string volMode = vol.Mode == "Full" ? "Completo" : "Básico";
							string statePt = vol.State switch
							{
								"Ready" => string.IsNullOrWhiteSpace(vol.StatusDetail) ? "Pronto" : vol.StatusDetail,
								"Indexing" => "Indexando em segundo plano...",
								"Excluded" => "Excluído pelo usuário",
								"BitLockerLocked" => "BitLocker bloqueado",
								_ => vol.State
							};

							SearchIndexVolumes.Add(new SearchIndexVolumeItemViewModel
							{
								VolumeGuid = vol.VolumeGuid,
								DisplayRoot = vol.DisplayRoot,
								FileSystem = vol.FileSystem,
								ModeDisplay = volMode,
								ItemCountDisplay = $"{vol.ItemCount:N0} itens",
								StateDisplay = statePt,
								IsExcluded = vol.State == "Excluded" || SearchIndexExcludedPaths.Any(p => p.Equals(vol.DisplayRoot, StringComparison.OrdinalIgnoreCase))
							});
						}
					}
				});
			}
			catch (Exception ex)
			{
				SearchIndexServiceSummary = $"Erro ao consultar índice: {ex.Message}";
			}
		}

		public async Task ReindexVolumeAsync(Guid? volumeGuid)
		{
			var provider = IndexedSearchProvider;
			if (provider is null)
				return;

			try
			{
				await provider.EnsureReadyAndConfiguredAsync();
				await provider.Client.ReindexAsync(volumeGuid);
				await Task.Delay(250);
				await RefreshSearchIndexStatusAsync();
			}
			catch
			{
			}
		}

		public async Task ToggleExcludeVolumeAsync(SearchIndexVolumeItemViewModel? item)
		{
			if (item is null || string.IsNullOrWhiteSpace(item.DisplayRoot))
				return;

			var existing = SearchIndexExcludedPaths.FirstOrDefault(p => p.Equals(item.DisplayRoot, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
				SearchIndexExcludedPaths.Remove(existing);
			else
				SearchIndexExcludedPaths.Add(item.DisplayRoot);

			await SaveAndApplyExcludedPathsAsync();
		}

		public async Task AddExcludedPathAsync()
		{
			string candidate = NewExcludedPathText?.Trim().Trim('"') ?? string.Empty;
			if (string.IsNullOrWhiteSpace(candidate))
				return;

			if (!SearchIndexExcludedPaths.Any(p => p.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
			{
				SearchIndexExcludedPaths.Add(candidate);
				NewExcludedPathText = string.Empty;
				await SaveAndApplyExcludedPathsAsync();
			}
		}

		public async Task RemoveExcludedPathAsync(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			var existing = SearchIndexExcludedPaths.FirstOrDefault(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
			if (existing is not null)
			{
				SearchIndexExcludedPaths.Remove(existing);
				await SaveAndApplyExcludedPathsAsync();
			}
		}

		private async Task SaveAndApplyExcludedPathsAsync()
		{
			var list = SearchIndexExcludedPaths.ToList();
			UserSettingsService.FoldersSettingsService.SearchIndexExcludedPaths = list;

			var provider = IndexedSearchProvider;
			if (provider is not null)
			{
				try
				{
					await provider.EnsureReadyAndConfiguredAsync();
					await provider.Client.ConfigureAsync(list.ToArray());
				}
				catch
				{
				}
			}

			await RefreshSearchIndexStatusAsync();
		}
	}
}
