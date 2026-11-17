// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Search.Index.Ipc;
using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;
using FileAttributes = System.IO.FileAttributes;

namespace KyraFiles.App.Services.Search
{
	/// <summary>
	/// Search provider backed by the out-of-process <c>KyraFiles.App.IndexService.exe</c> engine (F015 T11 — FR-001..FR-042).
	/// </summary>
	public sealed partial class IndexedSearchProvider : ISearchProvider, IDisposable
	{
		private static readonly string FolderTypeTextLocalized = Strings.Folder.GetLocalizedResource();

		private readonly NativeFilesSearchProvider _fallbackProvider = new();
		private readonly IUserSettingsService? _userSettingsService;
		private readonly ILogger<IndexedSearchProvider>? _logger;
		private readonly IndexServiceProcessManager _processManager;

		public string Id => "index";

		public string DisplayName => "Files Index";

		// F021: Bloqueio emergencial de recursos solicitado pelo Alexandre após diagnóstico do relatório "Lenovo em Repouso".
		// Mantém o processo KyraFiles.App.IndexService.exe 100% inativo e permite uso de Everything / Native.
		public bool IsAvailable => false;

		public SearchCapabilities Capabilities { get; } = new()
		{
			SupportsContentSearch = false,
			SupportsRegex = true,
			SupportsWildcards = true,
			SupportsDeepVolumes = true,
			IsInstantStreaming = true
		};

		public IndexServiceProcessManager ProcessManager => _processManager;

		public IndexPipeClient Client => _processManager.Client;

		public IndexedSearchProvider(
			IUserSettingsService? userSettingsService = null,
			ILogger<IndexedSearchProvider>? logger = null)
		{
			_userSettingsService = userSettingsService;
			_logger = logger;

			string cacheDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"KyraFiles",
				"SearchIndex");

			_processManager = new IndexServiceProcessManager(cacheDirectory: cacheDir);

			// F021: Desarmado o arranque em background do processo de índice para preservar CPU e memória.
		}

		public Task<bool> EnsureReadyAndConfiguredAsync(CancellationToken cancellationToken = default)
		{
			// F021: Bloqueado enquanto durar a contenção de recursos.
			return Task.FromResult(false);
		}

		public async Task SearchAsync(SearchRequest request, IList<ListedItem> results, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(request.Query))
				return;

			// Delegate tag queries, raw AQS ($...), and virtual library/RecycleBin scopes to FolderSearch
			if (IsTagOrRawAqsQuery(request.Query) || IsVirtualScope(request.Folder))
			{
				await _fallbackProvider.SearchAsync(request, results, cancellationToken);
				return;
			}

			// Network paths (UNC or mapped network drives) are not automatically indexed (FR-017)
			if (IsNetworkScope(request.Folder))
			{
				request.OnFallbackNotice?.Invoke("Caminho de rede não indexado — usando busca direta");
				await _fallbackProvider.SearchAsync(request, results, cancellationToken);
				return;
			}

			bool serviceAlive = await EnsureReadyAndConfiguredAsync(cancellationToken).ConfigureAwait(false);
			if (!serviceAlive)
			{
				request.OnFallbackNotice?.Invoke("Serviço de índice indisponível — usando busca direta");
				await _fallbackProvider.SearchAsync(request, results, cancellationToken);
				return;
			}

			string? scopePath = NormalizeScopePath(request.Folder);
			int maxResults = request.MaxItemCount > 0 ? (int)Math.Min(request.MaxItemCount, 100_000u) : 100_000;
			bool loadSmallIcons = request.MaxItemCount > 0 && request.MaxItemCount <= 50;

			var foldersSettings = _userSettingsService?.FoldersSettingsService;
			bool showHidden = foldersSettings?.ShowHiddenItems ?? false;
			bool showSystem = foldersSettings?.ShowProtectedSystemFiles ?? false;
			bool showDotFiles = foldersSettings?.ShowDotFiles ?? true;

			try
			{
				var response = await _processManager.Client.QueryAsync(
					request.Query,
					scopePath,
					maxResults,
					batch =>
					{
						var batchListed = new List<ListedItem>(batch.Count);
						foreach (var dto in batch)
						{
							if (cancellationToken.IsCancellationRequested)
								break;

							var attrs = (FileAttributes)dto.FileAttributes;
							bool isHidden = (attrs & FileAttributes.Hidden) == FileAttributes.Hidden;
							bool isSystem = (attrs & FileAttributes.System) == FileAttributes.System;
							bool startsWithDot = dto.Name.StartsWith('.');

							if ((isHidden && (!showHidden || (isSystem && !showSystem))) ||
								(startsWithDot && !showDotFiles))
							{
								continue;
							}

							var listedItem = CreateListedItem(dto, isHidden, loadSmallIcons);
							results.Add(listedItem);
							batchListed.Add(listedItem);

							if (results.Count >= maxResults)
								break;
						}

						if (batchListed.Count > 0)
							request.OnBatchReceived?.Invoke(batchListed);
					},
					cancellationToken).ConfigureAwait(false);

				if (!string.IsNullOrEmpty(response.FallbackReason) && results.Count == 0)
				{
					string notice = response.FallbackReason switch
					{
						"VolumeIndexing" => "Indexando disco em segundo plano — usando busca direta temporariamente",
						"UnsupportedContentOperator" => "Busca por conteúdo — usando busca tradicional",
						"NetworkPathNotIndexed" => "Caminho de rede não indexado — usando busca direta",
						"VolumeExcluded" => "Volume excluído do índice — usando busca direta",
						"ScopeExcluded" => "Pasta excluída do índice — usando busca direta",
						_ => $"Usando busca direta ({response.FallbackReason})"
					};

					request.OnFallbackNotice?.Invoke(notice);
					await _fallbackProvider.SearchAsync(request, results, cancellationToken);
					return;
				}

				if (response.IsTruncated)
				{
					request.OnTruncated?.Invoke(results.Count);
				}
			}
			catch (OperationCanceledException)
			{
				// Search cancelled by user typing or navigation
			}
			catch (Exception ex)
			{
				_logger?.LogWarning(ex, "Indexed search failed for query '{Query}'; falling back to FolderSearch", request.Query);
				if (results.Count == 0 && !cancellationToken.IsCancellationRequested)
				{
					request.OnFallbackNotice?.Invoke("Usando busca direta temporariamente");
					await _fallbackProvider.SearchAsync(request, results, cancellationToken);
				}
			}
		}

		private static ListedItem CreateListedItem(SearchResultDto dto, bool isHidden, bool loadSmallIcon)
		{
			DateTimeOffset modified = dto.LastWriteTimeUtcTicks > 0
				? new DateTimeOffset(dto.LastWriteTimeUtcTicks, TimeSpan.Zero).ToLocalTime()
				: default;
			DateTimeOffset created = dto.CreationTimeUtcTicks > 0
				? new DateTimeOffset(dto.CreationTimeUtcTicks, TimeSpan.Zero).ToLocalTime()
				: default;

			ListedItem item;
			if (dto.IsDirectory)
			{
				item = new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemNameRaw = dto.Name,
					ItemPath = dto.FullPath,
					ItemDateModifiedReal = modified,
					ItemDateCreatedReal = created,
					IsHiddenItem = isHidden,
					LoadFileIcon = false,
					ItemType = FolderTypeTextLocalized,
					Opacity = isHidden ? Constants.UI.DimItemOpacity : 1
				};
			}
			else
			{
				string? ext = null;
				string? itemType = null;
				if (dto.Name.Contains('.', StringComparison.Ordinal))
				{
					ext = Path.GetExtension(dto.Name);
					if (!string.IsNullOrEmpty(ext))
						itemType = ext.Trim('.');
				}

				item = new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.File,
					ItemNameRaw = dto.Name,
					ItemPath = dto.FullPath,
					ItemDateModifiedReal = modified,
					ItemDateCreatedReal = created,
					IsHiddenItem = isHidden,
					LoadFileIcon = false,
					FileExtension = ext,
					ItemType = itemType,
					Opacity = isHidden ? Constants.UI.DimItemOpacity : 1,
					FileSizeBytes = dto.FileSize,
					FileSize = dto.FileSize.ToSizeString()
				};
			}

			if (loadSmallIcon)
			{
				_ = FileThumbnailHelper.GetIconAsync(
					item.ItemPath,
					Constants.ShellIconSizes.Small,
					dto.IsDirectory,
					IconOptions.ReturnIconOnly)
					.ContinueWith(t =>
					{
						if (t.IsCompletedSuccessfully && t.Result is not null)
						{
							_ = FilesystemTasks.Wrap(() => MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								var bitmapImage = await t.Result.ToBitmapAsync();
								if (bitmapImage is not null)
									item.FileImage = bitmapImage;
							}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low));
						}
					});
			}

			return item;
		}

		private static bool IsTagOrRawAqsQuery(string query)
		{
			if (query.StartsWith('$'))
				return true;
			if (query.Contains("tag:", StringComparison.OrdinalIgnoreCase))
				return true;
			if (query.Contains("System.", StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}

		private static bool IsVirtualScope(string? folder)
		{
			if (string.IsNullOrWhiteSpace(folder) || folder == "Home" || folder == "ThisPC")
				return false;

			if (folder.StartsWith("RecycleBin", StringComparison.OrdinalIgnoreCase) ||
				folder.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				return true;

			if (App.LibraryManager is not null && App.LibraryManager.TryGetLibrary(folder, out _))
				return true;

			return false;
		}

		private static bool IsNetworkScope(string? folder)
		{
			if (string.IsNullOrWhiteSpace(folder) || folder == "Home" || folder == "ThisPC")
				return false;

			if (folder.StartsWith(@"\\", StringComparison.Ordinal) &&
				!folder.StartsWith(@"\\?\", StringComparison.Ordinal))
				return true;

			try
			{
				string? root = Path.GetPathRoot(folder);
				if (!string.IsNullOrEmpty(root) && root.Length >= 2 && root[1] == ':')
				{
					var driveInfo = new DriveInfo(root[..1]);
					return driveInfo.DriveType == System.IO.DriveType.Network;
				}
			}
			catch
			{
			}

			return false;
		}

		private static string? NormalizeScopePath(string? folder)
		{
			if (string.IsNullOrWhiteSpace(folder) || folder == "Home" || folder == "ThisPC")
				return null;

			return folder;
		}

		public void Dispose()
		{
			_processManager.Dispose();
		}
	}
}

