using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KyraFiles.Shared.FileDiff;

namespace KyraFiles.App.ViewModels.Dialogs
{
    public sealed partial class FolderCompareDialogViewModel : ObservableObject
    {
        private readonly string _leftDir;
        private readonly string _rightDir;
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private string _title = "Comparar pastas";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _hasStatusMessage;

        public ObservableCollection<FolderDiffItem> DiffItems { get; } = new();

        public FolderCompareDialogViewModel(string leftDir, string rightDir)
        {
            _leftDir = leftDir ?? string.Empty;
            _rightDir = rightDir ?? string.Empty;
            Title = $"Comparando: {System.IO.Path.GetFileName(_leftDir)} vs {System.IO.Path.GetFileName(_rightDir)}";
            _ = LoadComparisonAsync();
        }

        [RelayCommand]
        public async Task LoadComparisonAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            IsLoading = true;
            DiffItems.Clear();
            HasStatusMessage = false;

            try
            {
                var items = await FolderComparer.CompareFoldersAsync(_leftDir, _rightDir, _cts.Token);
                foreach (var item in items)
                {
                    DiffItems.Add(item);
                }

                StatusMessage = $"Comparação concluída: {DiffItems.Count} item(ns) analisado(s).";
                HasStatusMessage = true;
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Comparação cancelada.";
                HasStatusMessage = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro: {ex.Message}";
                HasStatusMessage = true;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
