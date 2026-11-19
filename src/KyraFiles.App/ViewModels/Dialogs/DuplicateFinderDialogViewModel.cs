// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KyraFiles.App.Extensions;
using KyraFiles.App.Helpers;
using KyraFiles.App.Utils.Storage.Operations;
using KyraFiles.Shared.DuplicateFinder;
using Microsoft.UI.Dispatching;

namespace KyraFiles.App.ViewModels.Dialogs
{
	/// <summary>
	/// ViewModel for DuplicateFinderDialog (US1, US2, US3; AC-1 a AC-7).
	/// </summary>
	public sealed partial class DuplicateFinderDialogViewModel : ObservableObject, IDisposable
	{
		private readonly DispatcherQueue? _dispatcherQueue;
		private CancellationTokenSource? _scanCts;

		public List<string> TargetFolders { get; } = [];

		public string TargetFoldersText => string.Join(", ", TargetFolders);

		public ObservableCollection<DuplicateGroupViewModel> Groups { get; } = [];

		public ObservableCollection<UnverifiedFileItem> UnverifiedFiles { get; } = [];

		[ObservableProperty]
		private int selectedModeIndex = 0; // 0 = SHA-256, 1 = Quick, 2 = Name/Size

		[ObservableProperty]
		private bool ignoreDevFolders = true;

		[ObservableProperty]
		private bool isScanning;

		[ObservableProperty]
		private bool isDeleting;

		[ObservableProperty]
		private bool hasGroups;

		[ObservableProperty]
		private bool hasUnverifiedFiles;

		[ObservableProperty]
		private bool canDelete;

		[ObservableProperty]
		private string summaryText = string.Empty;

		[ObservableProperty]
		private string statusMessage = string.Empty;

		[ObservableProperty]
		private string progressText = string.Empty;

		public IAsyncRelayCommand StartScanCommand { get; }

		public IRelayCommand CancelScanCommand { get; }

		public IRelayCommand KeepOldestAllCommand { get; }

		public IRelayCommand KeepNewestAllCommand { get; }

		public IRelayCommand ClearAllSelectionsCommand { get; }

		public IAsyncRelayCommand DeleteSelectedCommand { get; }

		public DuplicateFinderDialogViewModel(string initialFolder)
			: this([initialFolder])
		{
		}

		public DuplicateFinderDialogViewModel(IEnumerable<string> initialFolders)
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			if (initialFolders != null)
			{
				foreach (var folder in initialFolders.Where(Directory.Exists))
				{
					TargetFolders.Add(folder);
				}
			}

			StartScanCommand = new AsyncRelayCommand(StartScanAsync);
			CancelScanCommand = new RelayCommand(CancelScan);
			KeepOldestAllCommand = new RelayCommand(ApplyKeepOldestAll);
			KeepNewestAllCommand = new RelayCommand(ApplyKeepNewestAll);
			ClearAllSelectionsCommand = new RelayCommand(ClearAllSelections);
			DeleteSelectedCommand = new AsyncRelayCommand(ExecuteDeleteAsync);

			UpdateSummary();

			// Inicia a varredura inicial automaticamente ao abrir o diálogo
			_ = StartScanAsync();
		}

		public async Task StartScanAsync()
		{
			if (IsScanning || TargetFolders.Count == 0)
				return;

			_scanCts?.Cancel();
			_scanCts?.Dispose();
			_scanCts = new CancellationTokenSource();
			var token = _scanCts.Token;

			IsScanning = true;
			StatusMessage = Strings.DuplicateFinderScanning.GetLocalizedResource();
			ProgressText = string.Empty;
			Groups.Clear();
			UnverifiedFiles.Clear();
			HasGroups = false;
			HasUnverifiedFiles = false;
			UpdateCanDelete();

			var compareMode = SelectedModeIndex switch
			{
				1 => DuplicateCompareMode.QuickHashAndSize,
				2 => DuplicateCompareMode.NameAndSizeUnverified,
				_ => DuplicateCompareMode.ContentHashSha256
			};

			var options = new DuplicateFinderOptions
			{
				TargetFolders = TargetFolders,
				CompareMode = compareMode,
				IgnoreSystemFiles = true,
				MinFileSizeBytes = 1
			};
			if (!IgnoreDevFolders)
			{
				options.IgnoredFolderNames.Clear();
			}

			try
			{
				var progress = new Progress<DuplicateFinderProgress>(p =>
				{
					RunOnUIThread(() => ProgressText = p.CurrentStatus);
				});

				var result = await Task.Run(() => DuplicateFinderEngine.FindDuplicatesAsync(options, progress, token), token);

				if (!token.IsCancellationRequested)
				{
					foreach (var grp in result.Groups)
					{
						Groups.Add(new DuplicateGroupViewModel(grp, this));
					}

					foreach (var unv in result.UnverifiedFiles)
					{
						UnverifiedFiles.Add(unv);
					}

					HasGroups = Groups.Count > 0;
					HasUnverifiedFiles = UnverifiedFiles.Count > 0;

					if (Groups.Count == 0)
					{
						StatusMessage = Strings.DuplicateFinderNoDuplicates.GetLocalizedResource();
					}
					else
					{
						// Por padrão, aplica a regra segura "manter o mais antigo" (AC-3)
						ApplyKeepOldestAll();
						StatusMessage = $"{Groups.Count} grupos de duplicados encontrados ({result.TotalDuplicatesFound} cópias).";
					}
				}
			}
			catch (OperationCanceledException)
			{
				StatusMessage = "Varredura cancelada pelo usuário.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Erro durante a varredura: {ex.Message}";
			}
			finally
			{
				IsScanning = false;
				ProgressText = string.Empty;
				UpdateSummary();
				UpdateCanDelete();
			}
		}

		public void CancelScan()
		{
			_scanCts?.Cancel();
		}

		public void ApplyKeepOldestAll()
		{
			foreach (var grp in Groups)
			{
				grp.ApplyKeepOldest();
			}
			UpdateSummary();
			UpdateCanDelete();
		}

		public void ApplyKeepNewestAll()
		{
			foreach (var grp in Groups)
			{
				grp.ApplyKeepNewest();
			}
			UpdateSummary();
			UpdateCanDelete();
		}

		public void ClearAllSelections()
		{
			foreach (var grp in Groups)
			{
				grp.ClearSelection();
			}
			UpdateSummary();
			UpdateCanDelete();
		}

		internal void OnSelectionChanged()
		{
			UpdateSummary();
			UpdateCanDelete();
		}

		private void UpdateSummary()
		{
			var selectedItems = Groups
				.SelectMany(g => g.Items)
				.Where(i => i.IsSelectedForDeletion && !i.IsDeleted)
				.ToList();

			long totalBytes = selectedItems.Sum(i => i.FileSize);

			if (selectedItems.Count == 0)
			{
				SummaryText = Groups.Count > 0
					? $"{Groups.Count} grupos de duplicados encontrados (nenhuma cópia selecionada para exclusão)"
					: "Nenhum arquivo duplicado";
			}
			else
			{
				SummaryText = $"{selectedItems.Count} arquivos marcados ({totalBytes.ToSizeString()} a liberar na Lixeira)";
			}
		}

		private void UpdateCanDelete()
		{
			CanDelete = !IsScanning && !IsDeleting && Groups.Any(g => g.Items.Any(i => i.IsSelectedForDeletion && !i.IsDeleted));
		}

		public async Task ExecuteDeleteAsync()
		{
			var itemsToDelete = Groups
				.SelectMany(g => g.Items)
				.Where(i => i.IsSelectedForDeletion && !i.IsDeleted)
				.ToList();

			if (itemsToDelete.Count == 0)
				return;

			IsDeleting = true;
			UpdateCanDelete();

			try
			{
				long hwnd = MainWindow.Instance?.WindowHandle.ToInt64() ?? 0;
				var paths = itemsToDelete.Select(i => i.FilePath).ToArray();

				var (shellSuccess, _) = await FileOperationsHelpers.DeleteItemAsync(paths, permanently: false, hwnd, asAdmin: false, null);

				long freedBytes = 0;
				int deletedCount = 0;

				foreach (var item in itemsToDelete)
				{
					if (!File.Exists(item.FilePath))
					{
						item.IsDeleted = true;
						item.IsSelectedForDeletion = false;
						item.Status = Strings.CleanBuildFoldersDeleted.GetLocalizedResource();
						freedBytes += item.FileSize;
						deletedCount++;
					}
					else
					{
						item.Status = "Não foi possível mover para a Lixeira";
					}
				}

				StatusMessage = $"{deletedCount} cópias movidas para a Lixeira ({freedBytes.ToSizeString()} liberados).";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Falha na exclusão: {ex.Message}";
			}
			finally
			{
				IsDeleting = false;
				UpdateSummary();
				UpdateCanDelete();
			}
		}

		private void RunOnUIThread(Action action)
		{
			if (_dispatcherQueue is not null && !_dispatcherQueue.HasThreadAccess)
			{
				_dispatcherQueue.TryEnqueue(() => action());
			}
			else
			{
				action();
			}
		}

		public void Dispose()
		{
			_scanCts?.Cancel();
			_scanCts?.Dispose();
		}
	}

	/// <summary>
	/// ViewModel de um grupo de arquivos duplicados idênticos.
	/// </summary>
	public sealed partial class DuplicateGroupViewModel : ObservableObject
	{
		private readonly DuplicateFinderDialogViewModel _parentDialog;

		public DuplicateGroup Group { get; }
		public string GroupId => Group.GroupId;
		public long FileSize => Group.FileSize;
		public string FormattedSize => FileSize.ToSizeString();
		public ObservableCollection<DuplicateFileItemViewModel> Items { get; } = [];

		public string PrimaryFileName => Items.FirstOrDefault()?.FileName ?? "Arquivo";
		public string GroupHeader => $"{PrimaryFileName} • {Items.Count} cópias ({FormattedSize} cada)";

		public long ReclaimableBytes => Items.Where(i => i.IsSelectedForDeletion && !i.IsDeleted).Sum(i => i.FileSize);
		public string FormattedReclaimable => ReclaimableBytes.ToSizeString();

		public DuplicateGroupViewModel(DuplicateGroup group, DuplicateFinderDialogViewModel parentDialog)
		{
			Group = group;
			_parentDialog = parentDialog;
			foreach (var item in group.Items)
			{
				Items.Add(new DuplicateFileItemViewModel(item, this));
			}
		}

		public bool CanSelectItem(DuplicateFileItemViewModel itemVm)
		{
			return DuplicateSelectionRules.CanSelectForDeletion(Group, itemVm.Item);
		}

		public void OnItemSelectionChanged(DuplicateFileItemViewModel itemVm)
		{
			OnPropertyChanged(nameof(ReclaimableBytes));
			OnPropertyChanged(nameof(FormattedReclaimable));
			_parentDialog.OnSelectionChanged();
		}

		public void ApplyKeepOldest()
		{
			DuplicateSelectionRules.ApplyKeepOldest(Group);
			RefreshAllItems();
		}

		public void ApplyKeepNewest()
		{
			DuplicateSelectionRules.ApplyKeepNewest(Group);
			RefreshAllItems();
		}

		public void ClearSelection()
		{
			DuplicateSelectionRules.ClearSelection(Group);
			RefreshAllItems();
		}

		private void RefreshAllItems()
		{
			foreach (var vm in Items)
			{
				vm.NotifySelectionChanged();
			}
			OnPropertyChanged(nameof(ReclaimableBytes));
			OnPropertyChanged(nameof(FormattedReclaimable));
		}
	}

	/// <summary>
	/// ViewModel de uma cópia de arquivo dentro de um grupo.
	/// </summary>
	public sealed partial class DuplicateFileItemViewModel : ObservableObject
	{
		private readonly DuplicateGroupViewModel _parentGroup;

		public DuplicateFileItem Item { get; }

		public string FilePath => Item.FilePath;
		public string FileName => Item.FileName;
		public string FolderPath => Item.FolderPath;
		public long FileSize => Item.FileSize;
		public string FormattedSize => FileSize.ToSizeString();
		public DateTimeOffset ModifiedAt => Item.ModifiedAt;
		public DateTimeOffset CreatedAt => Item.CreatedAt;

		public string ModifiedAtFormatted => ModifiedAt.LocalDateTime.ToString("g");
		public string CreatedAtFormatted => CreatedAt.LocalDateTime.ToString("g");

		[ObservableProperty]
		private bool isDeleted;

		[ObservableProperty]
		private string status = string.Empty;

		public bool IsSelectedForDeletion
		{
			get => Item.IsSelectedForDeletion;
			set
			{
				if (Item.IsSelectedForDeletion == value)
					return;

				if (value)
				{
					// Invariante rígida (FR-002 / AC-4): Nunca permitir marcar a última cópia do grupo
					if (!_parentGroup.CanSelectItem(this))
					{
						OnPropertyChanged(nameof(IsSelectedForDeletion));
						return;
					}
					Item.IsSelectedForDeletion = true;
				}
				else
				{
					Item.IsSelectedForDeletion = false;
				}

				OnPropertyChanged();
				_parentGroup.OnItemSelectionChanged(this);
			}
		}

		public bool CanToggleSelection => !IsDeleted;

		public DuplicateFileItemViewModel(DuplicateFileItem item, DuplicateGroupViewModel parentGroup)
		{
			Item = item;
			_parentGroup = parentGroup;
		}

		internal void NotifySelectionChanged()
		{
			OnPropertyChanged(nameof(IsSelectedForDeletion));
		}
	}
}
