// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.ViewModels.Dialogs;
using KyraFiles.Shared.FileDiff;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenCompareAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;
		private readonly IDialogService _dialogService;

		public string Label => "Comparar...";

		public string Description => "Compara os arquivos ou pastas selecionados";

		public ActionCategory Category => ActionCategory.FileSystem;

		public bool IsExecutable
		{
			get
			{
				int count = _context.SelectedItems?.Count ?? 0;
				// AC-1 / AC-4: exatamente 2 itens selecionados
				if (count == 2)
					return true;

				// AC-2: 1 item selecionado (tentara comparar com o caminho da area de transferencia)
				if (count == 1)
					return true;

				// AC-3: 3 ou mais itens selecionados -> desabilitado
				return false;
			}
		}

		public OpenCompareAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			_context.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(IContentPageContext.SelectedItems))
				{
					OnPropertyChanged(nameof(IsExecutable));
				}
			};
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var selected = _context.SelectedItems;
			if (selected == null || selected.Count == 0 || selected.Count > 2)
				return;

			string leftPath = string.Empty;
			string rightPath = string.Empty;

			if (selected.Count == 2)
			{
				leftPath = selected[0]?.ItemPath ?? string.Empty;
				rightPath = selected[1]?.ItemPath ?? string.Empty;
			}
			else if (selected.Count == 1)
			{
				leftPath = selected[0]?.ItemPath ?? string.Empty;
				rightPath = await TryGetClipboardPathAsync();

				if (string.IsNullOrWhiteSpace(rightPath) || (!File.Exists(rightPath) && !Directory.Exists(rightPath)))
				{
					var errorDialog = new ContentDialog
					{
						Title = "Comparar com a Área de Transferência",
						Content = "Nenhum arquivo ou pasta válido foi encontrado na área de transferência para comparação.",
						CloseButtonText = "OK",
						XamlRoot = MainWindow.Instance.Content.XamlRoot
					};
					await errorDialog.ShowAsync();
					return;
				}
			}

			// Comparacao de pastas (AC-4)
			if (Directory.Exists(leftPath) && Directory.Exists(rightPath))
			{
				var vm = new FolderCompareDialogViewModel(leftPath, rightPath);
				await _dialogService.ShowDialogAsync(vm);
				return;
			}

			// Comparacao de arquivos (AC-1, AC-6, AC-7)
			if (File.Exists(leftPath) && File.Exists(rightPath))
			{
				// AC-6: arquivo binario
				bool leftIsBin = BinaryDetector.IsBinaryFile(leftPath);
				bool rightIsBin = BinaryDetector.IsBinaryFile(rightPath);

				if (leftIsBin || rightIsBin)
				{
					string leftHash = ComputeSha256(leftPath);
					string rightHash = ComputeSha256(rightPath);
					long leftSize = new FileInfo(leftPath).Length;
					long rightSize = new FileInfo(rightPath).Length;
					bool match = leftHash.Equals(rightHash, StringComparison.OrdinalIgnoreCase);

					var binDialog = new ContentDialog
					{
						Title = "Comparação de Arquivo Binário (AC-6)",
						Content = $"Um ou ambos os arquivos são binários.\r\n\r\n" +
								  $"Esquerda: {Path.GetFileName(leftPath)} ({leftSize:N0} bytes)\r\nSHA-256: {leftHash}\r\n\r\n" +
								  $"Direita: {Path.GetFileName(rightPath)} ({rightSize:N0} bytes)\r\nSHA-256: {rightHash}\r\n\r\n" +
								  $"Resultado: {(match ? "Arquivos IDÊNTICOS" : "Arquivos DIFERENTES")}",
						CloseButtonText = "Fechar",
						XamlRoot = MainWindow.Instance.Content.XamlRoot
					};
					await binDialog.ShowAsync();
					return;
				}

				// Ferramenta configurada / detectada
				var tools = DiffToolDetector.DetectInstalledTools();
				if (tools.Count == 0)
				{
					// AC-5: ferramenta ausente
					var noToolDialog = new ContentDialog
					{
						Title = "Ferramenta de Comparação Não Encontrada (AC-5)",
						Content = "Nenhuma ferramenta externa de diff (VS Code, WinMerge, Beyond Compare, Git Diff ou fc.exe) foi encontrada no sistema.",
						CloseButtonText = "OK",
						XamlRoot = MainWindow.Instance.Content.XamlRoot
					};
					await noToolDialog.ShowAsync();
					return;
				}

				var chosenTool = tools.FirstOrDefault();
				if (chosenTool != null)
				{
					var launchResult = FileDiffLauncher.LaunchDiff(chosenTool, leftPath, rightPath);
					if (!launchResult.Success)
					{
						var launchError = new ContentDialog
						{
							Title = "Erro ao Abrir Comparador",
							Content = launchResult.Message,
							CloseButtonText = "OK",
							XamlRoot = MainWindow.Instance.Content.XamlRoot
						};
						await launchError.ShowAsync();
					}
				}
			}
		}

		private static async Task<string> TryGetClipboardPathAsync()
		{
			try
			{
				var dataPackage = Clipboard.GetContent();
				if (dataPackage == null) return string.Empty;

				if (dataPackage.Contains(StandardDataFormats.StorageItems))
				{
					var items = await dataPackage.GetStorageItemsAsync();
					var first = items.FirstOrDefault();
					if (first != null && !string.IsNullOrWhiteSpace(first.Path))
						return first.Path;
				}

				if (dataPackage.Contains(StandardDataFormats.Text))
				{
					string text = await dataPackage.GetTextAsync();
					text = text?.Trim().Trim('\"', '\'') ?? string.Empty;
					if (File.Exists(text) || Directory.Exists(text))
						return text;
				}
			}
			catch { }

			return string.Empty;
		}

		private static string ComputeSha256(string path)
		{
			try
			{
				using var stream = File.OpenRead(path);
				using var sha = SHA256.Create();
				byte[] hash = sha.ComputeHash(stream);
				return Convert.ToHexString(hash);
			}
			catch (Exception ex)
			{
				return $"Erro ao calcular hash: {ex.Message}";
			}
		}
	}
}
