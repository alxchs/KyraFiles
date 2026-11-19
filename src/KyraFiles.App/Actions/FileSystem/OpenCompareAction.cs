// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Helpers;
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

		public RichGlyph Glyph => new(themedIconStyle: "App.ThemedIcons.Properties");

		public bool IsExecutable
		{
			get
			{
				if (_context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings)
					return false;

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
				if (e.PropertyName is nameof(IContentPageContext.SelectedItems) or nameof(IContentPageContext.PageType))
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
						CloseButtonText = "OK"
					};
					await errorDialog.TryShowAsync();
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
						CloseButtonText = "Fechar"
					};
					await binDialog.TryShowAsync();
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
						CloseButtonText = "OK"
					};
					await noToolDialog.TryShowAsync();
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
							CloseButtonText = "OK"
						};
						await launchError.TryShowAsync();
					}
				}
			}
		}

		private static async Task<string> TryGetClipboardPathAsync()
		{
			try
			{
				var dataPackage = Clipboard.GetContent();
				if (dataPackage.Contains(StandardDataFormats.StorageItems))
				{
					var items = await dataPackage.GetStorageItemsAsync();
					if (items.Count > 0)
					{
						return items[0].Path;
					}
				}

				if (dataPackage.Contains(StandardDataFormats.Text))
				{
					string text = await dataPackage.GetTextAsync();
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text.Trim('"', ' ', '\r', '\n');
					}
				}
			}
			catch
			{
			}

			return string.Empty;
		}

		private static string ComputeSha256(string filePath)
		{
			try
			{
				using var sha256 = SHA256.Create();
				using var stream = File.OpenRead(filePath);
				byte[] hash = sha256.ComputeHash(stream);
				return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			}
			catch
			{
				return "Erro ao calcular hash";
			}
		}
	}
}
