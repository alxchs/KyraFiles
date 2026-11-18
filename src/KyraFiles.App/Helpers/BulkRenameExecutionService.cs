// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.App.Utils.Storage;

namespace KyraFiles.App.Helpers
{
	public sealed class BulkRenameResult
	{
		public int TotalRequested { get; set; }
		public int SuccessCount { get; set; }
		public int FailureCount { get; set; }
		public List<string> FailedItems { get; } = new();
		public List<string> Errors { get; } = new();
		public StorageHistory? HistoryRecord { get; set; }
	}

	public static class BulkRenameExecutionService
	{
		public static async Task<BulkRenameResult> ExecuteAsync(
			IReadOnlyList<RenamePreviewItem> previewItems,
			CancellationToken cancellationToken = default)
		{
			var result = new BulkRenameResult();
			var itemsToRename = previewItems
				.Where(p => p.IsChanged && !p.HasConflict)
				.ToList();

			result.TotalRequested = itemsToRename.Count;
			if (itemsToRename.Count == 0)
				return result;

			var historySources = new List<IStorageItemWithPath>();
			var historyDestinations = new List<IStorageItemWithPath>();

			// Mapa para rastrear o caminho intermediário de cada item
			var currentPaths = new Dictionary<RenamePreviewItem, string>();
			foreach (var item in itemsToRename)
			{
				currentPaths[item] = item.Source.OriginalPath;
			}

			var sourcePathsSet = new HashSet<string>(
				itemsToRename.Select(p => p.Source.OriginalPath),
				StringComparer.OrdinalIgnoreCase);

			// Fase 1: Itens cujo destino colide com a origem de outro item do mesmo lote (ex: troca A <-> B)
			// Movemos temporariamente para um nome unico transitorio
			var tempRenamed = new List<(RenamePreviewItem Item, string TempPath)>();
			foreach (var item in itemsToRename)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Se o destino deste item é a origem de outro arquivo no lote, precisamos liberar o caminho
				if (sourcePathsSet.Contains(item.NewPath) && !string.Equals(item.Source.OriginalPath, item.NewPath, StringComparison.OrdinalIgnoreCase))
				{
					string dir = Path.GetDirectoryName(item.Source.OriginalPath) ?? string.Empty;
					string tempFileName = $"__kyra_tmp_{Guid.NewGuid():N}.tmp";
					string tempPath = Path.Combine(dir, tempFileName);

					try
					{
						if (item.Source.IsDirectory)
							Directory.Move(item.Source.OriginalPath, tempPath);
						else
							File.Move(item.Source.OriginalPath, tempPath);

						currentPaths[item] = tempPath;
						tempRenamed.Add((item, tempPath));
					}
					catch (Exception ex)
					{
						result.FailureCount++;
						result.FailedItems.Add(item.Source.OriginalPath);
						result.Errors.Add($"Falha ao alocar nome temporário para '{item.OriginalFullName}': {ex.Message}");
					}
				}
			}

			// Fase 2: Renomear para o destino final (NewPath)
			foreach (var item in itemsToRename)
			{
				cancellationToken.ThrowIfCancellationRequested();

				// Se já falhou na fase 1, ignora
				if (result.FailedItems.Contains(item.Source.OriginalPath))
					continue;

				string currentPath = currentPaths[item];
				string finalPath = item.NewPath;

				if (string.Equals(currentPath, finalPath, StringComparison.OrdinalIgnoreCase))
					continue;

				try
				{
					if (item.Source.IsDirectory)
						Directory.Move(currentPath, finalPath);
					else
						File.Move(currentPath, finalPath);

					var itemType = item.Source.IsDirectory ? FilesystemItemType.Directory : FilesystemItemType.File;
					historySources.Add(StorageHelpers.FromPathAndType(item.Source.OriginalPath, itemType));
					historyDestinations.Add(StorageHelpers.FromPathAndType(finalPath, itemType));
					result.SuccessCount++;
				}
				catch (Exception ex)
				{
					result.FailureCount++;
					result.FailedItems.Add(item.Source.OriginalPath);
					result.Errors.Add($"Falha ao renomear '{item.OriginalFullName}' para '{item.NewFullName}': {ex.Message}");

					// Se estava em caminho temporário, tenta restaurar para o original
					if (tempRenamed.Any(t => t.Item == item))
					{
						try
						{
							if (item.Source.IsDirectory)
								Directory.Move(currentPath, item.Source.OriginalPath);
							else
								File.Move(currentPath, item.Source.OriginalPath);
						}
						catch { }
					}
				}
			}

			// Registra no histórico atômico para suporte a Undo (Ctrl+Z)
			if (historySources.Count > 0)
			{
				var history = new StorageHistory(FileOperationType.Rename, historySources, historyDestinations);
				result.HistoryRecord = history;
				try
				{
					App.HistoryWrapper?.AddHistory(history);
					Ioc.Default.GetService<IOperationHistoryService>()?.RecordFromStorageHistory(history);
				}
				catch { }
			}

			await Task.Yield();
			return result;
		}
	}
}
