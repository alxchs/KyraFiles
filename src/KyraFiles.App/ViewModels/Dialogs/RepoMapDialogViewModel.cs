// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KyraFiles.Shared.RepoMap;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class RepoMapDialogViewModel : ObservableObject, IDisposable
	{
		private readonly DispatcherQueue? _dispatcherQueue;
		private readonly RepoMapAnalyzer _analyzer = new();
		private CancellationTokenSource? _analyzeCts;
		private RepoMapResult? _lastResult;

		public string TargetFolder { get; }

		public ObservableCollection<LanguageInfoViewModel> Languages { get; } = [];
		public ObservableCollection<RepoFileInfoViewModel> SelectedLanguageFiles { get; } = [];
		public ObservableCollection<RepoFileInfoViewModel> LargestFiles { get; } = [];
		public ObservableCollection<RepoFileInfoViewModel> HotSpots { get; } = [];

		[ObservableProperty]
		private bool respectGitIgnore = true;

		[ObservableProperty]
		private bool includeGitHotSpots = true;

		[ObservableProperty]
		private bool isAnalyzing;

		[ObservableProperty]
		private bool hasResults;

		[ObservableProperty]
		private string statusMessage = string.Empty;

		[ObservableProperty]
		private LanguageInfoViewModel? selectedLanguage;

		[ObservableProperty]
		private RepoFileInfoViewModel? selectedFile;

		[ObservableProperty]
		private bool isGitRepository;

		[ObservableProperty]
		private string gitStatusMessage = string.Empty;

		[ObservableProperty]
		private string totalSummaryText = string.Empty;

		[ObservableProperty]
		private string elapsedText = string.Empty;

		public RepoMapDialogViewModel(string targetFolder)
		{
			TargetFolder = targetFolder;
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			// Start analysis automatically upon dialog open
			_ = AnalyzeAsync();
		}

		partial void OnSelectedLanguageChanged(LanguageInfoViewModel? value)
		{
			SelectedLanguageFiles.Clear();
			if (value is not null)
			{
				foreach (var file in value.Files)
				{
					SelectedLanguageFiles.Add(new RepoFileInfoViewModel(file));
				}
			}
		}

		[RelayCommand]
		public async Task AnalyzeAsync()
		{
			if (IsAnalyzing)
				return;

			_analyzeCts?.Cancel();
			_analyzeCts?.Dispose();
			_analyzeCts = new CancellationTokenSource();
			var token = _analyzeCts.Token;

			IsAnalyzing = true;
			StatusMessage = "Analisando estrutura e métricas do repositório...";

			Languages.Clear();
			SelectedLanguageFiles.Clear();
			LargestFiles.Clear();
			HotSpots.Clear();
			HasResults = false;

			try
			{
				var options = new RepoMapOptions
				{
					RootPath = TargetFolder,
					RespectGitIgnore = RespectGitIgnore,
					IncludeGitHotSpots = IncludeGitHotSpots
				};

				var result = await _analyzer.AnalyzeAsync(options, cancellationToken: token);

				if (token.IsCancellationRequested || result.WasCancelled)
				{
					StatusMessage = "Análise cancelada pelo usuário.";
					return;
				}

				_lastResult = result;
				IsGitRepository = result.IsGitRepository;
				GitStatusMessage = result.GitStatusMessage ?? string.Empty;

				TotalSummaryText = $"Total: {result.TotalFiles:N0} arquivos | {result.TotalLines:N0} linhas | {result.FormattedTotalBytes}";
				ElapsedText = $"Concluído em {result.Elapsed.TotalSeconds:F2}s";

				foreach (var lang in result.Languages)
				{
					Languages.Add(new LanguageInfoViewModel(lang));
				}

				foreach (var file in result.AllFiles.Take(30))
				{
					LargestFiles.Add(new RepoFileInfoViewModel(file));
				}

				foreach (var hot in result.HotSpots.Take(30))
				{
					HotSpots.Add(new RepoFileInfoViewModel(hot));
				}

				if (Languages.Count > 0)
				{
					SelectedLanguage = Languages[0];
				}

				HasResults = true;
				StatusMessage = $"Análise concluída ({result.TotalFiles:N0} arquivos mapeados).";
			}
			catch (OperationCanceledException)
			{
				StatusMessage = "Análise cancelada.";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Erro durante a análise: {ex.Message}";
			}
			finally
			{
				IsAnalyzing = false;
			}
		}

		[RelayCommand]
		public void Cancel()
		{
			_analyzeCts?.Cancel();
			StatusMessage = "Cancelando análise...";
		}

		[RelayCommand]
		public void CopyMarkdown()
		{
			if (_lastResult is null)
				return;

			var markdown = _lastResult.ToMarkdownSummary();
			SetClipboardText(markdown);
			StatusMessage = "Resumo em Markdown copiado para a área de transferência!";
		}

		[RelayCommand]
		public void CopyCsv()
		{
			if (_lastResult is null)
				return;

			var csv = _lastResult.ToCsvLanguages() + "\n" + _lastResult.ToCsvFiles();
			SetClipboardText(csv);
			StatusMessage = "Métricas em CSV copiadas para a área de transferência!";
		}

		[RelayCommand]
		public void OpenFile(RepoFileInfoViewModel? fileVm)
		{
			var target = fileVm ?? SelectedFile;
			if (target is null || string.IsNullOrWhiteSpace(target.FilePath) || !File.Exists(target.FilePath))
				return;

			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = target.FilePath,
					UseShellExecute = true
				});
			}
			catch
			{
				// Ignore launcher errors
			}
		}

		private static void SetClipboardText(string text)
		{
			try
			{
				var dataPackage = new DataPackage();
				dataPackage.SetText(text);
				Clipboard.SetContent(dataPackage);
			}
			catch
			{
				// Ignore clipboard lock
			}
		}

		public void Dispose()
		{
			_analyzeCts?.Cancel();
			_analyzeCts?.Dispose();
		}
	}
}
