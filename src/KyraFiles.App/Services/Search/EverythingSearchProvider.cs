// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Data.Items;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using Windows.Storage;
using Windows.Win32;

namespace KyraFiles.App.Services.Search
{
	/// <summary>
	/// Search provider utilizing voidtools Everything index or CLI.
	/// </summary>
	public sealed class EverythingSearchProvider : ISearchProvider
	{
		private static readonly string[] PossibleEsPaths =
		[
			@"C:\Program Files\Everything\es.exe",
			@"C:\Program Files (x86)\Everything\es.exe"
		];

		private readonly NativeFilesSearchProvider _fallbackProvider = new();
		private readonly ILogger? _logger;

		public string Id => "everything";

		public string DisplayName => "Everything";

		public bool IsAvailable
		{
			get
			{
				try
				{
					// Detecção Win32 atômica instantânea (< 0,01 ms) sem varrer processos
					var hwnd = PInvoke.FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", (string?)null);
					if (!hwnd.IsNull)
						return true;

					return GetEsPath() is not null;
				}
				catch
				{
					return false;
				}
			}
		}

		public SearchCapabilities Capabilities { get; } = new()
		{
			SupportsContentSearch = false,
			SupportsRegex = true,
			SupportsWildcards = true,
			SupportsDeepVolumes = true,
			IsInstantStreaming = true
		};

		public EverythingSearchProvider(ILogger<EverythingSearchProvider>? logger = null)
		{
			_logger = logger;
		}

		private static string? GetEsPath()
		{
			// 1. Caminhos tradicionais em Program Files
			foreach (var path in PossibleEsPaths)
			{
				if (File.Exists(path))
					return path;
			}

			// 2. Pasta local da aplicação / AppContext
			string localApp = Path.Combine(AppContext.BaseDirectory, "es.exe");
			if (File.Exists(localApp))
				return localApp;

			// 3. Pasta oficial instalada do KyraFiles (com fallback legado para FilesCustom)
			string kyraFilesDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Programs", "KyraFiles", "es.exe");
			if (File.Exists(kyraFilesDir))
				return kyraFilesDir;

			string filesCustomDir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Programs", "FilesCustom", "es.exe");
			if (File.Exists(filesCustomDir))
				return filesCustomDir;

			// 4. Pasta de utilitários do usuário (~/bin)
			string userBin = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"bin", "es.exe");
			if (File.Exists(userBin))
				return userBin;

			// 5. Pesquisa no PATH do sistema
			var pathEnv = Environment.GetEnvironmentVariable("PATH");
			if (!string.IsNullOrWhiteSpace(pathEnv))
			{
				foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					try
					{
						string candidate = Path.Combine(dir, "es.exe");
						if (File.Exists(candidate))
							return candidate;
					}
					catch { }
				}
			}

			return null;
		}

		public async Task SearchAsync(SearchRequest request, IList<ListedItem> results, CancellationToken cancellationToken)
		{
			if (request.Query.Contains("tag:", StringComparison.OrdinalIgnoreCase))
			{
				var folderSearch = new Utils.Storage.FolderSearch
				{
					Query = request.Query,
					Folder = request.Folder,
					MaxItemCount = request.MaxItemCount
				};
				await folderSearch.SearchAsync(results, cancellationToken);
				return;
			}

			var esPath = GetEsPath();
			if (esPath is not null)
			{
				try
				{
					int maxResults = request.MaxItemCount > 0 ? (int)request.MaxItemCount : 500;
					var arguments = new List<string>
					{
						"-tsv",
						"-no-header",
						"-attrib",
						"-size",
						"-dm",
						"-n", maxResults.ToString()
					};

					string rawQuery = request.Query.Trim();
					string folder = request.Folder?.Trim().TrimEnd('\\', '/') ?? string.Empty;

					// Se a query já contém especificador de caminho (ex: r:\desenv, c:\, d:\, \),
					// NÃO adiciona -path para não criar conflito de diretório no Everything.
					bool queryHasPath = rawQuery.Contains(@":\") || rawQuery.Contains(@":/") || rawQuery.StartsWith(@"\");
					if (!queryHasPath && !string.IsNullOrEmpty(folder) && !folder.Equals("Home", StringComparison.OrdinalIgnoreCase) && Directory.Exists(folder))
					{
						arguments.Add("-path");
						arguments.Add($"\"{folder}\"");
					}

					// Adiciona os termos da query preservando a sintaxe original do Everything (sem aspas duplas envolventes)
					arguments.Add(rawQuery);

					var startInfo = new ProcessStartInfo
					{
						FileName = esPath,
						Arguments = string.Join(" ", arguments),
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						StandardOutputEncoding = Encoding.UTF8,
						WindowStyle = ProcessWindowStyle.Hidden
					};

					using var process = new Process { StartInfo = startInfo };
					process.Start();

					using (cancellationToken.Register(() =>
					{
						try
						{
							if (!process.HasExited)
								process.Kill();
						}
						catch { }
					}))
					{
						using var reader = process.StandardOutput;
						string? line;
						int count = 0;

						while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
						{
							if (!EverythingResultLine.TryParse(line, out var row))
								continue;

							var item = new ListedItem(null!)
							{
								ItemPath = row.FullPath,
								ItemNameRaw = row.Name,
								PrimaryItemAttribute = row.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
								FileSizeBytes = row.SizeBytes,
								FileSize = row.IsFolder ? string.Empty : row.SizeBytes.ToSizeString(),
								ItemDateModifiedReal = row.DateModified ?? DateTime.Now,
								ItemDateCreatedReal = row.DateModified ?? DateTime.Now,
								FileExtension = row.Extension,
								ItemType = row.IsFolder ? "Folder" : (string.IsNullOrEmpty(row.Extension) ? "File" : $"{row.Extension.TrimStart('.').ToUpperInvariant()} File"),
								ItemPropertiesInitialized = true,
								LoadFileIcon = false,
								Opacity = row.IsHidden ? Constants.UI.DimItemOpacity : 1
							};

							results.Add(item);
							count++;

							if (request.OnBatchReceived is not null && (count == 10 || count == 25 || count % 50 == 0))
							{
								request.OnBatchReceived(results.ToList());
							}
						}

						await process.WaitForExitAsync(cancellationToken);

						if (count >= maxResults && request.OnTruncated is not null)
						{
							request.OnTruncated(maxResults);
						}

						request.OnBatchReceived?.Invoke(results.ToList());

						return;
					}
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception ex)
				{
					_logger?.LogWarning(ex, "Everything CLI streaming search failed for query {Query}", request.Query);
				}
			}

			// Fallback to Native Files Search when Everything CLI/IPC is not directly invokable
			await _fallbackProvider.SearchAsync(request, results, cancellationToken);
		}
	}
}
