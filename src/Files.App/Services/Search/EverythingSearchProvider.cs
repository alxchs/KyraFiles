// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Data.Items;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Text;
using Windows.Storage;
using Windows.Win32;

namespace Files.App.Services.Search
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

			// 3. Pasta oficial instalada do Files Custom
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
							if (string.IsNullOrWhiteSpace(line))
								continue;

							// Formato TSV: <Caminho>\t<Atributos>\t<TamanhoEmBytes>\t<DataModificada>
							var parts = line.Split('\t');
							string path = parts.Length > 0 ? parts[0].Trim().Trim('"') : string.Empty;
							if (string.IsNullOrEmpty(path))
								continue;

							string attrStr = parts.Length > 1 ? parts[1].Trim() : string.Empty;
							string sizeStr = parts.Length > 2 ? parts[2].Trim() : string.Empty;
							string dateStr = parts.Length > 3 ? parts[3].Trim() : string.Empty;

							bool isFolder = path.EndsWith('\\') || path.EndsWith('/');
							if (!isFolder && int.TryParse(attrStr, out int attrVal))
							{
								isFolder = (attrVal & 16) != 0;
							}

							string cleanPath = isFolder ? path.TrimEnd('\\', '/') : path;
							string fileName = Path.GetFileName(cleanPath);
							if (string.IsNullOrEmpty(fileName))
								fileName = cleanPath;

							long size = 0;
							if (!isFolder && long.TryParse(sizeStr, out long parsedSize))
								size = parsedSize;

							DateTime? dateModified = null;
							if (DateTime.TryParse(dateStr, out var parsedDate))
								dateModified = parsedDate;

							string ext = isFolder ? string.Empty : Path.GetExtension(fileName);
							var item = new ListedItem(null!)
							{
								ItemPath = cleanPath,
								ItemNameRaw = fileName,
								PrimaryItemAttribute = isFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
								FileSizeBytes = size,
								FileSize = isFolder ? string.Empty : size.ToSizeString(),
								ItemDateModifiedReal = dateModified ?? DateTime.Now,
								ItemDateCreatedReal = dateModified ?? DateTime.Now,
								FileExtension = ext,
								ItemType = isFolder ? "Folder" : (string.IsNullOrEmpty(ext) ? "File" : $"{ext.TrimStart('.').ToUpperInvariant()} File"),
								ItemPropertiesInitialized = true,
								LoadFileIcon = false
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
