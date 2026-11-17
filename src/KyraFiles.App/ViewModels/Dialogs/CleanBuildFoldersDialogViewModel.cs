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
using KyraFiles.Shared.BuildFolder;
using Microsoft.UI.Dispatching;

namespace KyraFiles.App.ViewModels.Dialogs
{
	/// <summary>
	/// ViewModel for the Clean Build Folders dialog (US1, US2, US3; AC-1 a AC-6).
	/// </summary>
	public sealed partial class CleanBuildFoldersDialogViewModel : ObservableObject
	{
		private readonly DispatcherQueue? _dispatcherQueue;
		private CancellationTokenSource? _scanCts;
		private CancellationTokenSource? _calcCts;

		public string RootPath { get; }

		public ObservableCollection<BuildFolderItemViewModel> Items { get; } = [];

		[ObservableProperty]
		private bool isScanning;

		[ObservableProperty]
		private bool isCalculatingSize;

		[ObservableProperty]
		private bool isDeleting;

		[ObservableProperty]
		private bool deletePermanently;

		[ObservableProperty]
		private string summaryText = string.Empty;

		[ObservableProperty]
		private string statusMessage = string.Empty;

		[ObservableProperty]
		private bool hasItems;

		[ObservableProperty]
		private bool canDelete;

		[ObservableProperty]
		private bool hasInUseItems;

		[ObservableProperty]
		private string primaryButtonText = string.Empty;

		public IRelayCommand SelectAllCommand { get; }

		public IRelayCommand DeselectAllCommand { get; }

		public IAsyncRelayCommand DeleteSelectedCommand { get; }

		public CleanBuildFoldersDialogViewModel(string rootPath)
		{
			RootPath = rootPath;
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			UpdatePrimaryButtonText();

			SelectAllCommand = new RelayCommand(SelectAll);
			DeselectAllCommand = new RelayCommand(DeselectAll);
			DeleteSelectedCommand = new AsyncRelayCommand(ExecuteDeleteAsync);

			_ = InitializeAsync();
		}

		private void UpdatePrimaryButtonText()
		{
			if (HasInUseItems)
			{
				PrimaryButtonText = "Tentar novamente";
			}
			else if (DeletePermanently)
			{
				PrimaryButtonText = Strings.CleanBuildFoldersDeletePermanently.GetLocalizedResource();
			}
			else
			{
				PrimaryButtonText = Strings.CleanBuildFoldersMoveToTrash.GetLocalizedResource();
			}
		}

		partial void OnDeletePermanentlyChanged(bool value)
		{
			UpdatePrimaryButtonText();
		}

		partial void OnHasInUseItemsChanged(bool value)
		{
			UpdatePrimaryButtonText();
		}

		public async Task InitializeAsync()
		{
			if (string.IsNullOrWhiteSpace(RootPath) || !Directory.Exists(RootPath))
			{
				StatusMessage = Strings.CleanBuildFoldersNoFoldersFound.GetLocalizedResource();
				return;
			}

			IsScanning = true;
			StatusMessage = "Localizando pastas de compilação...";
			_scanCts = new CancellationTokenSource();

			try
			{
				var candidates = await Task.Run(() =>
				{
					var checker = new CliGitTrackingChecker();
					var scanner = new BuildFolderScanner(gitTracker: checker);
					return scanner.Scan(RootPath, _scanCts.Token);
				}, _scanCts.Token);

				Items.Clear();
				foreach (var candidate in candidates)
				{
					var itemVm = new BuildFolderItemViewModel(candidate);
					itemVm.PropertyChanged += ItemVm_PropertyChanged;
					Items.Add(itemVm);
				}

				HasItems = Items.Count > 0;
				if (!HasItems)
				{
					StatusMessage = Strings.CleanBuildFoldersNoFoldersFound.GetLocalizedResource();
					SummaryText = string.Empty;
				}
				else
				{
					StatusMessage = string.Empty;
					UpdateSummary();
					StartSizeCalculation();
				}
			}
			catch (OperationCanceledException)
			{
				StatusMessage = "Varredura cancelada.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Erro na varredura: {ex.Message}";
			}
			finally
			{
				IsScanning = false;
				UpdateCanDelete();
			}
		}

		private void StartSizeCalculation()
		{
			_calcCts?.Cancel();
			_calcCts = new CancellationTokenSource();
			var token = _calcCts.Token;

			IsCalculatingSize = true;

			_ = Task.Run(() =>
			{
				try
				{
					foreach (var item in Items.ToList())
					{
						if (token.IsCancellationRequested)
							break;

						if (item.SizeBytes < 0 && Directory.Exists(item.FolderPath))
						{
							long size = 0;
							try
							{
								var dirInfo = new DirectoryInfo(item.FolderPath);
								foreach (var file in dirInfo.EnumerateFiles("*", new EnumerationOptions
								{
									RecurseSubdirectories = true,
									IgnoreInaccessible = true,
									AttributesToSkip = FileAttributes.ReparsePoint
								}))
								{
									if (token.IsCancellationRequested)
										break;
									size += file.Length;
								}
							}
							catch
							{
							}

							RunOnUIThread(() =>
							{
								item.UpdateSize(size);
								UpdateSummary();
							});
						}
					}
				}
				finally
				{
					RunOnUIThread(() =>
					{
						IsCalculatingSize = false;
						UpdateSummary();
					});
				}
			}, token);
		}

		private void ItemVm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(BuildFolderItemViewModel.IsSelected) or nameof(BuildFolderItemViewModel.SizeBytes))
			{
				UpdateSummary();
				UpdateCanDelete();
			}
		}

		public void SelectAll()
		{
			foreach (var item in Items)
			{
				if (item.CanToggleSelection)
					item.IsSelected = true;
			}
			UpdateSummary();
			UpdateCanDelete();
		}

		public void DeselectAll()
		{
			foreach (var item in Items)
			{
				if (item.CanToggleSelection)
					item.IsSelected = false;
			}
			UpdateSummary();
			UpdateCanDelete();
		}

		private void UpdateSummary()
		{
			var selected = Items.Where(i => i.IsSelected && !i.IsDeleted && !i.IsInUse).ToList();
			long totalSelectedBytes = selected.Where(i => i.SizeBytes > 0).Sum(i => i.SizeBytes);

			if (selected.Count == 0)
			{
				SummaryText = "Nenhuma pasta selecionada";
			}
			else
			{
				SummaryText = $"{selected.Count} pastas selecionadas ({totalSelectedBytes.ToSizeString()} a liberar)";
			}
		}

		private void UpdateCanDelete()
		{
			CanDelete = !IsDeleting && !IsScanning && Items.Any(i => i.IsSelected && !i.IsDeleted && !i.IsInUse);
		}

		public async Task ExecuteDeleteAsync()
		{
			var itemsToDelete = Items.Where(i => i.IsSelected && !i.IsDeleted && !i.IsInUse).ToList();
			if (itemsToDelete.Count == 0)
				return;

			IsDeleting = true;
			UpdateCanDelete();

			long freedBytes = 0;
			long inUseBytes = 0;
			bool foundInUse = false;

			try
			{
				foreach (var item in itemsToDelete)
				{
					var (succeeded, inUse) = await BuildFolderCleanerHelper.DeleteBuildFolderAsync(item.FolderPath, DeletePermanently);

					if (succeeded)
					{
						item.IsDeleted = true;
						item.IsInUse = false;
						item.IsSelected = false;
						item.Status = Strings.CleanBuildFoldersDeleted.GetLocalizedResource();
						if (item.SizeBytes > 0)
							freedBytes += item.SizeBytes;
					}
					else if (inUse)
					{
						item.IsInUse = true;
						item.IsSelected = false;
						item.Status = Strings.CleanBuildFoldersInUse.GetLocalizedResource();
						foundInUse = true;
						if (item.SizeBytes > 0)
							inUseBytes += item.SizeBytes;
					}
					else
					{
						item.Status = "Falha ao excluir";
					}
				}

				HasInUseItems = foundInUse;

				if (foundInUse)
				{
					SummaryText = $"Liberado: {freedBytes.ToSizeString()} • Em uso / pulado: {inUseBytes.ToSizeString()}";
					StatusMessage = "Alguns itens estão em uso e foram mantidos.";
				}
				else
				{
					SummaryText = $"Liberado: {freedBytes.ToSizeString()}";
					StatusMessage = "Limpeza concluída com sucesso.";
				}
			}
			finally
			{
				IsDeleting = false;
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
			_calcCts?.Cancel();
			_calcCts?.Dispose();
		}
	}
}
