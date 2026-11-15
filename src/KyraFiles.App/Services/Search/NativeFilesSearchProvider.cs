// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Data.Items;
using KyraFiles.App.Extensions;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Enumeration;
using FileAttributes = System.IO.FileAttributes;
using StorageItemTypes = Windows.Storage.StorageItemTypes;

namespace KyraFiles.App.Services.Search
{
	/// <summary>
	/// Provedor nativo autônomo de busca de alta velocidade, baseado em System.IO.Enumeration Win32 puro e streaming assíncrono.
	/// Totalmente independente de ferramentas de terceiros (Everything, es.exe) e desacoplado do Windows Search (WSearch).
	/// </summary>
	public sealed class NativeFilesSearchProvider : ISearchProvider
	{
		private readonly ILogger? _logger;

		public string Id => "native";

		public string DisplayName => "Files Native (Win32 Fast)";

		public bool IsAvailable => true;

		public SearchCapabilities Capabilities { get; } = new()
		{
			SupportsContentSearch = false,
			SupportsRegex = false,
			SupportsWildcards = true,
			SupportsDeepVolumes = true,
			IsInstantStreaming = true
		};

		public NativeFilesSearchProvider(ILogger<NativeFilesSearchProvider>? logger = null)
		{
			_logger = logger;
		}

		public async Task SearchAsync(SearchRequest request, IList<ListedItem> results, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(request.Query))
				return;

			string searchRoot = request.Folder;
			if (string.IsNullOrWhiteSpace(searchRoot) || searchRoot.Equals("Home", StringComparison.OrdinalIgnoreCase))
			{
				var homeDirs = new[]
				{
					Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
					Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
					Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
					Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
				}.Where(d => !string.IsNullOrEmpty(d) && Directory.Exists(d)).Distinct();

				int maxResults = request.MaxItemCount > 0 ? (int)request.MaxItemCount : 500;
				foreach (var dir in homeDirs)
				{
					if (cancellationToken.IsCancellationRequested || results.Count >= maxResults)
						return;

					await SearchDirectoryAsync(dir, request, results, cancellationToken);
				}
				return;
			}

			if (!Directory.Exists(searchRoot))
				return;

			await SearchDirectoryAsync(searchRoot, request, results, cancellationToken);
		}

		private Task SearchDirectoryAsync(string rootPath, SearchRequest request, IList<ListedItem> results, CancellationToken cancellationToken)
		{
			return Task.Run(() =>
			{
				int maxResults = request.MaxItemCount > 0 ? (int)request.MaxItemCount : 500;
				string query = request.Query.Trim();
				bool hasWildcards = query.Contains('*') || query.Contains('?');

				var enumOptions = new EnumerationOptions
				{
					RecurseSubdirectories = request.SearchSubfolders,
					IgnoreInaccessible = true,
					AttributesToSkip = FileAttributes.ReparsePoint,
					MatchCasing = MatchCasing.CaseInsensitive,
					ReturnSpecialDirectories = false
				};

				try
				{
					var enumerable = new FileSystemEnumerable<ListedItem>(
						rootPath,
						(ref FileSystemEntry entry) =>
						{
							bool isDir = entry.IsDirectory;
							string fullPath = entry.ToSpecifiedFullPath();
							string fileName = entry.FileName.ToString();
							var attrs = entry.Attributes;
							bool isHidden = (attrs & FileAttributes.Hidden) != 0;
							long size = isDir ? 0 : entry.Length;
							string ext = isDir ? string.Empty : Path.GetExtension(fileName);

							return new ListedItem(null!)
							{
								ItemPath = fullPath,
								ItemNameRaw = fileName,
								PrimaryItemAttribute = isDir ? StorageItemTypes.Folder : StorageItemTypes.File,
								FileSizeBytes = size,
								FileSize = isDir ? string.Empty : size.ToSizeString(),
								ItemDateModifiedReal = entry.LastWriteTimeUtc.LocalDateTime,
								ItemDateCreatedReal = entry.CreationTimeUtc.LocalDateTime,
								IsHiddenItem = isHidden,
								Opacity = isHidden ? Constants.UI.DimItemOpacity : 1,
								FileExtension = ext,
								ItemType = isDir ? "Folder" : (string.IsNullOrEmpty(ext) ? "File" : $"{ext.TrimStart('.').ToUpperInvariant()} File"),
								ItemPropertiesInitialized = true,
								LoadFileIcon = false
							};
						},
						enumOptions)
					{
						ShouldRecursePredicate = (ref FileSystemEntry entry) =>
						{
							if (cancellationToken.IsCancellationRequested)
								return false;

							ReadOnlySpan<char> dirName = entry.FileName;

							if (dirName.Equals("AppData", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals(".git", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals(".nuget", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals(".cargo", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals(".rustup", StringComparison.OrdinalIgnoreCase) ||
								dirName.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase))
							{
								return false;
							}

							return true;
						},
						ShouldIncludePredicate = (ref FileSystemEntry entry) =>
						{
							if (cancellationToken.IsCancellationRequested)
								return false;

							ReadOnlySpan<char> fileName = entry.FileName;

							if (hasWildcards)
							{
								return FileSystemName.MatchesSimpleExpression(query, fileName, ignoreCase: true);
							}

							return fileName.Contains(query, StringComparison.OrdinalIgnoreCase);
						}
					};

					int count = results.Count;
					foreach (var item in enumerable)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						results.Add(item);
						count++;

						if (request.OnBatchReceived is not null && (count == 10 || count == 25 || count % 50 == 0))
						{
							request.OnBatchReceived(results.ToList());
						}

						if (count >= maxResults)
						{
							request.OnTruncated?.Invoke(maxResults);
							break;
						}
					}

					request.OnBatchReceived?.Invoke(results.ToList());
				}
				catch (OperationCanceledException) { }
				catch (Exception ex)
				{
					_logger?.LogWarning(ex, "Native fast search failed for folder {Folder} and query {Query}", rootPath, request.Query);
				}
			}, cancellationToken);
		}
	}
}
