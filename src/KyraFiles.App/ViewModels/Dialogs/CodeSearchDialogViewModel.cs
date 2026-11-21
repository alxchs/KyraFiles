// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Extensions;
using KyraFiles.App.Helpers;
using KyraFiles.Shared.CodeSearch;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class CodeSearchDialogViewModel : ObservableObject, IDisposable
	{
		private readonly DispatcherQueue? _dispatcherQueue;
		private readonly IDevToolsSettingsService? _devToolsSettingsService;
		private CancellationTokenSource? _searchCts;

		public string TargetFolder { get; }

		public ObservableCollection<CodeSearchResultItemViewModel> Matches { get; } = [];

		[ObservableProperty]
		private string query = string.Empty;

		[ObservableProperty]
		private bool isRegex;

		[ObservableProperty]
		private bool matchCase;

		[ObservableProperty]
		private bool matchWholeWord;

		[ObservableProperty]
		private bool respectGitIgnore = true;

		[ObservableProperty]
		private string fileExtensions = "*.cs, *.xaml, *.json, *.xml, *.md, *.ts, *.js, *.py";

		[ObservableProperty]
		private bool isSearching;

		[ObservableProperty]
		private bool hasMatches;

		[ObservableProperty]
		private string statusMessage = string.Empty;

		[ObservableProperty]
		private CodeSearchResultItemViewModel? selectedMatch;

		public bool CanOpenInIDE => SelectedMatch is not null;

		public CodeSearchDialogViewModel(string targetFolder)
		{
			TargetFolder = targetFolder ?? string.Empty;
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			try
			{
				_devToolsSettingsService = Ioc.Default.GetService<IDevToolsSettingsService>();
			}
			catch
			{
				// DevTools service optional
			}

			StatusMessage = string.IsNullOrEmpty(TargetFolder)
				? "Nenhuma pasta selecionada."
				: $"Pronto para buscar em: {TargetFolder}";
		}

		partial void OnSelectedMatchChanged(CodeSearchResultItemViewModel? value)
		{
			OnPropertyChanged(nameof(CanOpenInIDE));
		}

		[RelayCommand]
		public async Task StartSearchAsync()
		{
			if (string.IsNullOrWhiteSpace(Query))
			{
				StatusMessage = "Informe o texto ou expressão regular para busca.";
				return;
			}

			CancelSearch();
			_searchCts = new CancellationTokenSource();
			var token = _searchCts.Token;

			IsSearching = true;
			HasMatches = false;
			Matches.Clear();
			StatusMessage = Strings.CodeSearchScanning.GetLocalizedResource();

			var options = new CodeSearchOptions
			{
				SearchDirectory = TargetFolder,
				Query = Query,
				IsRegex = IsRegex,
				MatchCase = MatchCase,
				MatchWholeWord = MatchWholeWord,
				RespectGitIgnore = RespectGitIgnore,
				FileExtensions = string.IsNullOrWhiteSpace(FileExtensions) ? null : FileExtensions
			};

			var progress = new Progress<CodeSearchProgress>(p =>
			{
				_dispatcherQueue?.TryEnqueue(() =>
				{
					StatusMessage = $"{p.FilesScanned} arquivos varridos, {p.MatchesFound} ocorrências encontradas...";
				});
			});

			void OnMatch(CodeSearchResultItem match)
			{
				_dispatcherQueue?.TryEnqueue(() =>
				{
					Matches.Add(new CodeSearchResultItemViewModel(match));
					HasMatches = true;
				});
			}

			try
			{
				var result = await CodeSearchEngine.SearchAsync(options, progress, OnMatch, token);

				if (!string.IsNullOrEmpty(result.ErrorMessage))
				{
					StatusMessage = result.ErrorMessage;
				}
				else if (result.WasCancelled)
				{
					StatusMessage = $"Busca cancelada ({Matches.Count} ocorrências encontradas).";
				}
				else if (Matches.Count == 0)
				{
					StatusMessage = Strings.CodeSearchNoMatches.GetLocalizedResource();
				}
				else
				{
					StatusMessage = $"Concluído em {result.Elapsed.TotalSeconds:N1}s ({result.TotalFilesScanned} arquivos varridos, {Matches.Count} ocorrências).";
				}
			}
			catch (OperationCanceledException)
			{
				StatusMessage = $"Busca cancelada ({Matches.Count} ocorrências encontradas).";
			}
			catch (Exception ex)
			{
				StatusMessage = $"Erro durante a busca: {ex.Message}";
			}
			finally
			{
				IsSearching = false;
			}
		}

		[RelayCommand]
		public void CancelSearch()
		{
			if (_searchCts is not null && !_searchCts.IsCancellationRequested)
			{
				_searchCts.Cancel();
			}
		}

		[RelayCommand]
		public async Task OpenInIDEAsync(CodeSearchResultItemViewModel? item = null)
		{
			var target = item ?? SelectedMatch;
			if (target is null)
				return;

			string idePath = _devToolsSettingsService?.IDEPath ?? string.Empty;
			string filePath = target.FilePath;
			int line = target.LineNumber;
			int col = target.ColumnNumber;

			// If no configured IDE path, fallback to VS Code command or shell launch
			if (string.IsNullOrWhiteSpace(idePath))
			{
				var codeCmd = $"code -g {Win32Helper.ToPowerShellStringLiteral($"{filePath}:{line}:{col}")}";
				var launched = await Win32Helper.RunPowershellCommandAsync(codeCmd, PowerShellExecutionOptions.Hidden);
				if (!launched)
				{
					OpenFile(target);
				}
				return;
			}

			// Format command based on known IDE signatures
			string cmd;
			var ideLower = idePath.ToLowerInvariant();
			if (ideLower.Contains("code"))
			{
				cmd = $"& {Win32Helper.ToPowerShellStringLiteral(idePath)} -g {Win32Helper.ToPowerShellStringLiteral($"{filePath}:{line}:{col}")}";
			}
			else if (ideLower.Contains("devenv"))
			{
				cmd = $"& {Win32Helper.ToPowerShellStringLiteral(idePath)} /Command \"Edit.GoTo {line}\" {Win32Helper.ToPowerShellStringLiteral(filePath)}";
			}
			else if (ideLower.Contains("rider"))
			{
				cmd = $"& {Win32Helper.ToPowerShellStringLiteral(idePath)} --line {line} {Win32Helper.ToPowerShellStringLiteral(filePath)}";
			}
			else if (ideLower.Contains("notepad++"))
			{
				cmd = $"& {Win32Helper.ToPowerShellStringLiteral(idePath)} -n{line} -c{col} {Win32Helper.ToPowerShellStringLiteral(filePath)}";
			}
			else
			{
				cmd = $"& {Win32Helper.ToPowerShellStringLiteral(idePath)} {Win32Helper.ToPowerShellStringLiteral(filePath)}";
			}

			var success = await Win32Helper.RunPowershellCommandAsync(cmd, PowerShellExecutionOptions.Hidden);
			if (!success)
			{
				OpenFile(target);
			}
		}

		[RelayCommand]
		public void OpenFile(CodeSearchResultItemViewModel? item = null)
		{
			var target = item ?? SelectedMatch;
			if (target is null || !File.Exists(target.FilePath))
				return;

			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = target.FilePath,
					UseShellExecute = true
				};
				Process.Start(psi);
			}
			catch
			{
				// Best effort launch
			}
		}

		[RelayCommand]
		public void CopyLocation(CodeSearchResultItemViewModel? item = null)
		{
			var target = item ?? SelectedMatch;
			if (target is null)
				return;

			try
			{
				var text = $"{target.FilePath}:{target.LineNumber}:{target.ColumnNumber}";
				var data = new DataPackage();
				data.SetText(text);
				Clipboard.SetContent(data);
				Clipboard.Flush();
			}
			catch
			{
				// Ignore clipboard errors
			}
		}

		public void Dispose()
		{
			CancelSearch();
			_searchCts?.Dispose();
			_searchCts = null;
		}
	}
}
