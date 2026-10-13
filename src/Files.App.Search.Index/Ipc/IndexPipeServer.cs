// Copyright (c) Files Community. Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Files.App.Search.Index.Basic;
using Files.App.Search.Index.Engine;
using Files.App.Search.Index.Models;
using Files.App.Search.Index.Query;
using Files.App.Search.Index.Storage;
using Files.App.Search.Index.Volumes;

namespace Files.App.Search.Index.Ipc;

/// <summary>
/// Representa um volume gerenciado pelo servidor de índice em memória, com seu <see cref="ChangeWatcher"/> ativo.
/// </summary>
public sealed class ManagedVolume : IDisposable
{
	public Guid VolumeGuid { get; }
	public string DisplayRoot { get; set; }
	public string FileSystem { get; set; }
	public string Mode { get; set; }
	public volatile string State = "Indexing";
	public string? StatusDetail { get; set; }
	public long ScanningItemCount;
	public VolumeIndex? Index { get; set; }
	public DateTimeOffset RootLastWriteTime;
	public DateTimeOffset LastIndexedUtc;
	public ChangeWatcher? Watcher { get; set; }

	public ManagedVolume(Guid volumeGuid, string displayRoot, string fileSystem, string mode = "Basic")
	{
		VolumeGuid = volumeGuid;
		DisplayRoot = displayRoot.TrimEnd('\\') + '\\';
		FileSystem = fileSystem;
		Mode = mode;
	}

	public void Dispose()
	{
		Watcher?.Dispose();
		Watcher = null;
	}
}

internal sealed class DirectScanProgress(Action<ScanProgress> callback) : IProgress<ScanProgress>
{
	public void Report(ScanProgress value) => callback(value);
}

/// <summary>
/// Servidor de Named Pipe que hospeda o motor de índice (<c>Files.App.IndexService.exe</c>)
/// e atende consultas progressivas em lotes, cancelamento, status e configuração (F015 T10 — FR-040, FR-042).
/// </summary>
public sealed class IndexPipeServer : IDisposable
{
	private readonly string _pipeName;
	private readonly string _mode;
	private readonly string _cacheDirectory;
	private readonly ConcurrentDictionary<Guid, ManagedVolume> _volumes = new();
	private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeQueries = new(StringComparer.Ordinal);
	private readonly Lock _configLock = new();
	private readonly CancellationTokenSource _serverCts = new();

	private HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase);
	private VolumeTable? _volumeTable;
	private Task? _acceptLoopTask;
	private bool _disposed;

	public string PipeName => _pipeName;
	public CancellationToken ShutdownToken => _serverCts.Token;

	public IndexPipeServer(
		string? pipeName = null,
		string mode = "Basic",
		string? cacheDirectory = null)
	{
		_pipeName = string.IsNullOrWhiteSpace(pipeName)
			? IndexPipeProtocol.GetBasicPipeName()
			: pipeName;
		_mode = mode;
		_cacheDirectory = string.IsNullOrWhiteSpace(cacheDirectory)
			? VolumeSnapshot.GetDefaultBasicCacheDirectory()
			: cacheDirectory;

		Directory.CreateDirectory(_cacheDirectory);
		LoadExcludedPathsConfig();
	}

	/// <summary>
	/// Registra um volume já indexado (útil para testes de integração rápidos e para adição dinâmica de volumes).
	/// </summary>
	public void RegisterVolume(
		VolumeIndex index,
		string fileSystem = "NTFS",
		bool startWatcher = true,
		string state = "Ready")
	{
		ArgumentNullException.ThrowIfNull(index);

		var mv = new ManagedVolume(index.VolumeGuid, index.VolumeRoot, fileSystem, _mode)
		{
			Index = index,
			State = state,
			LastIndexedUtc = DateTimeOffset.UtcNow,
			RootLastWriteTime = TryGetRootWriteTime(index.VolumeRoot)
		};

		if (startWatcher && Directory.Exists(index.VolumeRoot))
		{
			try
			{
				var watcher = new ChangeWatcher(index);
				watcher.Start();
				mv.Watcher = watcher;
			}
			catch
			{
				// Ignora falha ao iniciar watcher em pastas sintéticas sem suporte
			}
		}

		if (_volumes.TryGetValue(index.VolumeGuid, out ManagedVolume? old))
			old.Dispose();

		_volumes[index.VolumeGuid] = mv;
	}

	/// <summary>
	/// Inicia a descoberta e indexação em segundo plano de todos os volumes locais (modo básico).
	/// Carrega snapshots existentes (<c>&lt; 2 s</c>) e reconcilia pastas alteradas, ou varre do zero.
	/// </summary>
	public void StartVolumeIndexingInBackground()
	{
		_ = Task.Run(async () =>
		{
			try
			{
				_volumeTable = new VolumeTable();
				var candidates = new List<VolumeInfo>();

				foreach (var vol in _volumeTable.Volumes)
				{
					if (_serverCts.IsCancellationRequested)
						return;

					string fsName = GetVolumeFileSystemName(vol.DisplayRoot);
					var mv = new ManagedVolume(vol.VolumeGuid, vol.DisplayRoot, fsName, _mode);
					_volumes[vol.VolumeGuid] = mv;

					if (IsPathExcluded(vol.DisplayRoot))
					{
						mv.State = "Excluded";
						mv.StatusDetail = "Excluído pelo usuário";
						continue;
					}

					// Verifica se há snapshot válido em disco
					string snapPath = VolumeSnapshot.GetSnapshotFilePath(_cacheDirectory, vol.VolumeGuid);
					uint serial = VolumeSnapshot.GetVolumeSerialNumber(vol.DisplayRoot);

					if (VolumeSnapshot.TryLoad(snapPath, out VolumeSnapshotData? snapData, expectedVolumeSerialNumber: serial) &&
						snapData is not null)
					{
						snapData.Index.VolumeRoot = vol.DisplayRoot;
						mv.Index = snapData.Index;
						mv.RootLastWriteTime = snapData.RootLastWriteTime;
						mv.LastIndexedUtc = DateTimeOffset.UtcNow;
						mv.State = "Ready";
						mv.StatusDetail = "Carregado do snapshot; reconciliando em segundo plano";

						StartWatcherSafe(mv);

						// Reconcilia pastas modificadas enquanto o app esteve fechado (T9 — FR-032)
						_ = Task.Run(() =>
						{
							try
							{
								DateTimeOffset rt = mv.RootLastWriteTime;
								var rec = Reconciler.ReconcileVolume(snapData.Index, ref rt, _serverCts.Token);
								mv.RootLastWriteTime = rt;
								mv.LastIndexedUtc = DateTimeOffset.UtcNow;
								mv.StatusDetail = $"Pronto ({rec.DirectoriesRescanned} pastas reconciliadas)";
								if (rec.DirectoriesRescanned > 0)
								{
									VolumeSnapshot.Save(snapPath, snapData.Index, serial, rt);
								}
							}
							catch
							{
							}
						});
					}
					else
					{
						candidates.Add(vol);
					}
				}

				if (candidates.Count > 0)
				{
					var scanProgress = new DirectScanProgress(p =>
					{
						if (_volumes.TryGetValue(p.VolumeGuid, out ManagedVolume? mv) && mv.State == "Indexing")
						{
							Interlocked.Exchange(ref mv.ScanningItemCount, p.TotalItems);
							mv.StatusDetail = $"Indexando ({p.TotalItems:N0} itens)...";
						}
					});

					await DirectoryScanner.ScanVolumesAsync(
						candidates,
						progress: scanProgress,
						cancellationToken: _serverCts.Token,
						onVolumeCompleted: (vol, idx) =>
						{
							if (_volumes.TryGetValue(vol.VolumeGuid, out ManagedVolume? mv))
							{
								mv.Index = idx;
								Interlocked.Exchange(ref mv.ScanningItemCount, idx.Count);
								mv.RootLastWriteTime = TryGetRootWriteTime(mv.DisplayRoot);
								mv.LastIndexedUtc = DateTimeOffset.UtcNow;
								mv.State = "Ready";
								mv.StatusDetail = "Indexado";

								StartWatcherSafe(mv);

								try
								{
									string snapPath = VolumeSnapshot.GetSnapshotFilePath(_cacheDirectory, mv.VolumeGuid);
									VolumeSnapshot.Save(snapPath, idx, rootLastWriteTime: mv.RootLastWriteTime);
								}
								catch
								{
								}
							}
						}).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[IndexPipeServer] Erro ao indexar volumes: {ex.Message}");
			}
		});
	}

	/// <summary>
	/// Inicia o loop assíncrono de aceitação de clientes no Named Pipe.
	/// </summary>
	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_acceptLoopTask is not null)
			return;

		_acceptLoopTask = Task.Run(AcceptClientsLoopAsync);
	}

	private async Task AcceptClientsLoopAsync()
	{
		while (!_serverCts.IsCancellationRequested)
		{
			NamedPipeServerStream? serverStream = null;
			try
			{
				serverStream = new NamedPipeServerStream(
					_pipeName,
					PipeDirection.InOut,
					NamedPipeServerStream.MaxAllowedServerInstances,
					PipeTransmissionMode.Byte,
					PipeOptions.Asynchronous);

				await serverStream.WaitForConnectionAsync(_serverCts.Token).ConfigureAwait(false);

				var connectedStream = serverStream;
				serverStream = null;
				_ = Task.Run(() => HandleClientConnectionAsync(connectedStream));
			}
			catch (OperationCanceledException)
			{
				serverStream?.Dispose();
				break;
			}
			catch
			{
				serverStream?.Dispose();
				await Task.Delay(50).ConfigureAwait(false);
			}
		}
	}

	private async Task HandleClientConnectionAsync(NamedPipeServerStream pipeStream)
	{
		using (pipeStream)
		using (var reader = new StreamReader(pipeStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true))
		using (var writer = new StreamWriter(pipeStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 16384, leaveOpen: true) { AutoFlush = true })
		{
			var writeLock = new SemaphoreSlim(1, 1);

			async Task SendResponseAsync(IndexPipeResponse response)
			{
				string json = JsonSerializer.Serialize(response, IndexPipeJsonContext.Default.IndexPipeResponse);
				await writeLock.WaitAsync().ConfigureAwait(false);
				try
				{
					if (pipeStream.IsConnected)
					{
						await writer.WriteLineAsync(json).ConfigureAwait(false);
					}
				}
				finally
				{
					writeLock.Release();
				}
			}

			while (pipeStream.IsConnected && !_serverCts.IsCancellationRequested)
			{
				string? line;
				try
				{
					line = await reader.ReadLineAsync(_serverCts.Token).ConfigureAwait(false);
				}
				catch
				{
					break;
				}

				if (line is null)
					break;

				if (string.IsNullOrWhiteSpace(line))
					continue;

				IndexPipeRequest? request;
				try
				{
					request = JsonSerializer.Deserialize(line, IndexPipeJsonContext.Default.IndexPipeRequest);
				}
				catch (Exception ex)
				{
					await SendResponseAsync(new IndexPipeResponse
					{
						Type = "error",
						ErrorMessage = $"JSON inválido: {ex.Message}"
					}).ConfigureAwait(false);
					continue;
				}

				if (request is null)
					continue;

				if (request.Version != IndexPipeProtocol.CurrentVersion)
				{
					await SendResponseAsync(new IndexPipeResponse
					{
						Type = "error",
						RequestId = request.RequestId,
						FallbackReason = "VersionMismatch",
						ErrorMessage = $"Versão de protocolo incompatível (cliente={request.Version}, servidor={IndexPipeProtocol.CurrentVersion})."
					}).ConfigureAwait(false);
					continue;
				}

				string cmd = request.Command.ToLowerInvariant();
				switch (cmd)
				{
					case "ping":
						await SendResponseAsync(new IndexPipeResponse
						{
							Type = "pong",
							RequestId = request.RequestId,
							Mode = _mode,
							ServerProcessId = Environment.ProcessId
						}).ConfigureAwait(false);
						break;

					case "status":
						await SendResponseAsync(BuildStatusResponse(request.RequestId)).ConfigureAwait(false);
						break;

					case "cancel":
						if (!string.IsNullOrEmpty(request.RequestId) &&
							_activeQueries.TryRemove(request.RequestId, out CancellationTokenSource? qCts))
						{
							qCts.Cancel();
							qCts.Dispose();
						}
						break;

					case "configure":
						ApplyConfiguration(request);
						await SendResponseAsync(BuildStatusResponse(request.RequestId, type: "configured")).ConfigureAwait(false);
						break;

					case "reindex":
						TriggerReindex(request.VolumeGuid);
						await SendResponseAsync(BuildStatusResponse(request.RequestId, type: "configured")).ConfigureAwait(false);
						break;

					case "shutdown":
						await SendResponseAsync(new IndexPipeResponse
						{
							Type = "configured",
							RequestId = request.RequestId,
							ServerProcessId = Environment.ProcessId
						}).ConfigureAwait(false);
						_serverCts.Cancel();
						return;

					case "query":
						// Executa a consulta em background nesta conexão para permitir recepção de comando "cancel" na mesma conexão ou em outra
						_ = Task.Run(() => ExecuteQueryRequestAsync(request, SendResponseAsync));
						break;

					default:
						await SendResponseAsync(new IndexPipeResponse
						{
							Type = "error",
							RequestId = request.RequestId,
							ErrorMessage = $"Comando desconhecido: {request.Command}"
						}).ConfigureAwait(false);
						break;
				}
			}
		}
	}

	private async Task ExecuteQueryRequestAsync(
		IndexPipeRequest request,
		Func<IndexPipeResponse, Task> sendResponseAsync)
	{
		string reqId = request.RequestId ?? Guid.NewGuid().ToString("N");
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_serverCts.Token);
		_activeQueries[reqId] = linkedCts;

		var sw = Stopwatch.StartNew();
		int totalMatches = 0;
		bool isTruncated = false;
		bool isCancelled = false;

		try
		{
			var matcher = new QueryMatcher(request.Query ?? string.Empty);
			var engine = new QueryEngine();
			int maxResults = request.MaxResults > 0 ? request.MaxResults : 100_000;

			string? scopePath = request.ScopePath;
			bool isGlobalScope = string.IsNullOrWhiteSpace(scopePath) ||
				scopePath.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
				scopePath.Equals("ThisPC", StringComparison.OrdinalIgnoreCase) ||
				scopePath.Equals("Este Computador", StringComparison.OrdinalIgnoreCase);

			if (isGlobalScope)
			{
				bool anyReady = false;
				bool anyIndexing = false;

				foreach (var mv in _volumes.Values)
				{
					if (linkedCts.Token.IsCancellationRequested)
					{
						isCancelled = true;
						break;
					}

					if (mv.State == "Indexing")
						anyIndexing = true;

					if (mv.State != "Ready" || mv.Index is null || IsPathExcluded(mv.DisplayRoot))
						continue;

					anyReady = true;
					int remaining = maxResults - totalMatches;
					if (remaining <= 0)
					{
						isTruncated = true;
						break;
					}

					ExecuteOnVolume(
						engine,
						mv.Index,
						matcher,
						scopeParentIndex: VolumeIndex.RootParentIndex,
						rewriteScope: null,
						remaining,
						reqId,
						sendResponseAsync,
						ref totalMatches,
						ref isTruncated,
						ref isCancelled,
						linkedCts.Token);
				}

				string? fallback = !anyReady
					? (anyIndexing ? "VolumeIndexing" : "NoVolumesReady")
					: null;

				sw.Stop();
				await sendResponseAsync(new IndexPipeResponse
				{
					Type = "completed",
					RequestId = reqId,
					TotalMatches = totalMatches,
					ElapsedMs = sw.ElapsedMilliseconds,
					IsTruncated = isTruncated,
					IsCancelled = isCancelled,
					FallbackReason = fallback
				}).ConfigureAwait(false);
				return;
			}

			// Escopo específico de pasta
			if (IsPathExcluded(scopePath!))
			{
				await sendResponseAsync(new IndexPipeResponse
				{
					Type = "completed",
					RequestId = reqId,
					FallbackReason = "PathExcluded"
				}).ConfigureAwait(false);
				return;
			}

			if (!TryResolveVolumeAndSubtree(scopePath!, out ManagedVolume? targetVol, out int scopeParentIdx, out ResolvedScope? rewriteScope))
			{
				await sendResponseAsync(new IndexPipeResponse
				{
					Type = "completed",
					RequestId = reqId,
					FallbackReason = "VolumeNotIndexed"
				}).ConfigureAwait(false);
				return;
			}

			if (targetVol!.State != "Ready" || targetVol.Index is null)
			{
				await sendResponseAsync(new IndexPipeResponse
				{
					Type = "completed",
					RequestId = reqId,
					FallbackReason = targetVol.State == "Indexing" ? "VolumeIndexing" : $"Volume{targetVol.State}"
				}).ConfigureAwait(false);
				return;
			}

			if (scopeParentIdx == VolumeIndex.DeletedParentIndex)
			{
				// A subpasta pedida não existe no índice deste volume
				await sendResponseAsync(new IndexPipeResponse
				{
					Type = "completed",
					RequestId = reqId,
					TotalMatches = 0,
					ElapsedMs = sw.ElapsedMilliseconds
				}).ConfigureAwait(false);
				return;
			}

			ExecuteOnVolume(
				engine,
				targetVol.Index,
				matcher,
				scopeParentIdx,
				rewriteScope,
				maxResults,
				reqId,
				sendResponseAsync,
				ref totalMatches,
				ref isTruncated,
				ref isCancelled,
				linkedCts.Token);

			sw.Stop();
			await sendResponseAsync(new IndexPipeResponse
			{
				Type = "completed",
				RequestId = reqId,
				TotalMatches = totalMatches,
				ElapsedMs = sw.ElapsedMilliseconds,
				IsTruncated = isTruncated,
				IsCancelled = isCancelled
			}).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			await sendResponseAsync(new IndexPipeResponse
			{
				Type = "error",
				RequestId = reqId,
				ErrorMessage = ex.Message
			}).ConfigureAwait(false);
		}
		finally
		{
			_activeQueries.TryRemove(reqId, out _);
		}
	}

	private void ExecuteOnVolume(
		QueryEngine engine,
		VolumeIndex index,
		QueryMatcher matcher,
		int scopeParentIndex,
		ResolvedScope? rewriteScope,
		int maxResults,
		string requestId,
		Func<IndexPipeResponse, Task> sendResponseAsync,
		ref int totalMatches,
		ref bool isTruncated,
		ref bool isCancelled,
		CancellationToken cancellationToken)
	{
		int localMatches = 0;

		QueryExecutionResult res = engine.Execute(
			index,
			matcher,
			batch =>
			{
				var dtoList = new List<SearchResultDto>(batch.Count);
				foreach (var item in batch)
				{
					string fullPath = rewriteScope.HasValue
						? ScopeResolver.RewriteResultPath(item.FullPath, rewriteScope.Value)
						: item.FullPath;

					if (IsPathExcluded(fullPath))
						continue;

					dtoList.Add(new SearchResultDto
					{
						FileReference = item.FileReference,
						Name = item.Name,
						FullPath = fullPath,
						FileSize = item.FileSize,
						CreationTimeUtcTicks = item.CreationTime.UtcTicks,
						LastWriteTimeUtcTicks = item.LastWriteTime.UtcTicks,
						FileAttributes = item.FileAttributes,
						IsDirectory = item.IsDirectory
					});
				}

				if (dtoList.Count > 0)
				{
					localMatches += dtoList.Count;
					sendResponseAsync(new IndexPipeResponse
					{
						Type = "batch",
						RequestId = requestId,
						Items = dtoList.ToArray()
					}).GetAwaiter().GetResult();
				}
			},
			scopeParentIndex: scopeParentIndex,
			maxResults: maxResults,
			cancellationToken: cancellationToken);

		totalMatches += localMatches;
		if (res.IsTruncated)
			isTruncated = true;
		if (res.IsCancelled)
			isCancelled = true;
	}

	private bool TryResolveVolumeAndSubtree(
		string scopePath,
		out ManagedVolume? matchedVolume,
		out int scopeParentIndex,
		out ResolvedScope? rewriteScope)
	{
		matchedVolume = null;
		scopeParentIndex = VolumeIndex.RootParentIndex;
		rewriteScope = null;

		string normalizedScope = scopePath.TrimEnd('\\') + '\\';

		// 1. Tenta resolver via VolumeTable + ScopeResolver (atravessa subst, junções e symlinks — FR-015)
		if (_volumeTable is not null)
		{
			var resolved = ScopeResolver.Resolve(scopePath, _volumeTable);
			if (resolved.Volume is not null &&
				_volumes.TryGetValue(resolved.Volume.VolumeGuid, out ManagedVolume? mv) &&
				mv.Index is not null)
			{
				matchedVolume = mv;
				rewriteScope = resolved;
				string realScope = resolved.RealScopePath.TrimEnd('\\') + '\\';
				string rel = realScope.StartsWith(mv.DisplayRoot, StringComparison.OrdinalIgnoreCase)
					? realScope[mv.DisplayRoot.Length..].Trim('\\')
					: string.Empty;
				scopeParentIndex = mv.Index.FindDirectoryIndexByRelativePath(rel);
				return true;
			}
		}

		// 2. Fallback por prefixo direto nos volumes registrados (suporta também diretórios de teste sintéticos)
		ManagedVolume? bestMatch = null;
		foreach (var mv in _volumes.Values)
		{
			if (normalizedScope.StartsWith(mv.DisplayRoot, StringComparison.OrdinalIgnoreCase))
			{
				if (bestMatch is null || mv.DisplayRoot.Length > bestMatch.DisplayRoot.Length)
					bestMatch = mv;
			}
		}

		if (bestMatch?.Index is not null)
		{
			matchedVolume = bestMatch;
			string rel = normalizedScope[bestMatch.DisplayRoot.Length..].Trim('\\');
			scopeParentIndex = bestMatch.Index.FindDirectoryIndexByRelativePath(rel);
			return true;
		}

		return false;
	}

	private static string GetVolumeFileSystemName(string rootPath)
	{
		try
		{
			var di = new DriveInfo(rootPath);
			if (di.IsReady)
				return di.DriveFormat;
		}
		catch
		{
		}

		return "NTFS";
	}

	private IndexPipeResponse BuildStatusResponse(string? requestId, string type = "status")
	{
		var volList = new List<VolumeStatusDto>(_volumes.Count);
		foreach (var mv in _volumes.Values.OrderBy(v => v.DisplayRoot, StringComparer.OrdinalIgnoreCase))
		{
			volList.Add(new VolumeStatusDto
			{
				VolumeGuid = mv.VolumeGuid,
				DisplayRoot = mv.DisplayRoot,
				FileSystem = mv.FileSystem,
				Mode = mv.Mode,
				State = mv.State,
				ItemCount = mv.Index?.Count ?? (int)Interlocked.Read(ref mv.ScanningItemCount),
				LastIndexedUtcTicks = mv.LastIndexedUtc.UtcTicks,
				StatusDetail = mv.StatusDetail
			});
		}

		string[] excluded;
		lock (_configLock)
		{
			excluded = _excludedPaths.ToArray();
		}

		return new IndexPipeResponse
		{
			Type = type,
			RequestId = requestId,
			Mode = _mode,
			ServerProcessId = Environment.ProcessId,
			Volumes = volList.ToArray(),
			ExcludedPaths = excluded
		};
	}

	private void ApplyConfiguration(IndexPipeRequest request)
	{
		if (request.ExcludedPaths is null)
			return;

		lock (_configLock)
		{
			_excludedPaths = new HashSet<string>(
				request.ExcludedPaths
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.Select(p => p.TrimEnd('\\')),
				StringComparer.OrdinalIgnoreCase);

			SaveExcludedPathsConfig();
		}

		foreach (var mv in _volumes.Values)
		{
			if (IsPathExcluded(mv.DisplayRoot))
			{
				mv.State = "Excluded";
				mv.StatusDetail = "Excluído pelo usuário";
				mv.Watcher?.Dispose();
				mv.Watcher = null;
			}
			else if (mv.State == "Excluded")
			{
				TriggerReindex(mv.VolumeGuid);
			}
		}
	}

	private void TriggerReindex(Guid? volumeGuid)
	{
		var targets = volumeGuid.HasValue
			? _volumes.Values.Where(v => v.VolumeGuid == volumeGuid.Value).ToList()
			: _volumes.Values.ToList();

		foreach (var mv in targets)
		{
			if (IsPathExcluded(mv.DisplayRoot))
				continue;

			mv.State = "Indexing";
			mv.StatusDetail = "Reindexando...";
			mv.Index = null;
			Interlocked.Exchange(ref mv.ScanningItemCount, 0);
			mv.Watcher?.Dispose();
			mv.Watcher = null;

			_ = Task.Run(() =>
			{
				try
				{
					var progress = new DirectScanProgress(p =>
					{
						Interlocked.Exchange(ref mv.ScanningItemCount, p.TotalItems);
						mv.StatusDetail = $"Indexando ({p.TotalItems:N0} itens)...";
					});
					var idx = DirectoryScanner.ScanDirectory(mv.VolumeGuid, mv.DisplayRoot, progress, _serverCts.Token);
					mv.Index = idx;
					Interlocked.Exchange(ref mv.ScanningItemCount, idx.Count);
					mv.RootLastWriteTime = TryGetRootWriteTime(mv.DisplayRoot);
					mv.LastIndexedUtc = DateTimeOffset.UtcNow;
					mv.State = "Ready";
					mv.StatusDetail = "Indexado";
					StartWatcherSafe(mv);

					string snapPath = VolumeSnapshot.GetSnapshotFilePath(_cacheDirectory, mv.VolumeGuid);
					VolumeSnapshot.Save(snapPath, idx, rootLastWriteTime: mv.RootLastWriteTime);
				}
				catch (Exception ex)
				{
					mv.State = "Error";
					mv.StatusDetail = ex.Message;
				}
			});
		}
	}

	private bool IsPathExcluded(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return false;

		string clean = path.TrimEnd('\\');
		lock (_configLock)
		{
			foreach (string excl in _excludedPaths)
			{
				if (clean.Equals(excl, StringComparison.OrdinalIgnoreCase) ||
					(clean.StartsWith(excl, StringComparison.OrdinalIgnoreCase) &&
					 clean.Length > excl.Length &&
					 clean[excl.Length] == '\\'))
				{
					return true;
				}
			}
		}

		return false;
	}

	private void LoadExcludedPathsConfig()
	{
		try
		{
			string cfgFile = Path.Combine(_cacheDirectory, "excluded_paths.txt");
			if (File.Exists(cfgFile))
			{
				var lines = File.ReadAllLines(cfgFile)
					.Where(l => !string.IsNullOrWhiteSpace(l))
					.Select(l => l.Trim().TrimEnd('\\'));
				_excludedPaths = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
			}
		}
		catch
		{
		}
	}

	private void SaveExcludedPathsConfig()
	{
		try
		{
			string cfgFile = Path.Combine(_cacheDirectory, "excluded_paths.txt");
			File.WriteAllLines(cfgFile, _excludedPaths);
		}
		catch
		{
		}
	}

	private static void StartWatcherSafe(ManagedVolume mv)
	{
		if (mv.Index is null || !Directory.Exists(mv.DisplayRoot))
			return;

		try
		{
			mv.Watcher?.Dispose();
			var watcher = new ChangeWatcher(mv.Index, mv.DisplayRoot);
			watcher.Start();
			mv.Watcher = watcher;
		}
		catch
		{
		}
	}

	private static DateTimeOffset TryGetRootWriteTime(string rootPath)
	{
		try
		{
			return Directory.Exists(rootPath)
				? new DateTimeOffset(Directory.GetLastWriteTimeUtc(rootPath), TimeSpan.Zero)
				: DateTimeOffset.UtcNow;
		}
		catch
		{
			return DateTimeOffset.UtcNow;
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_serverCts.Cancel();

		foreach (var q in _activeQueries.Values)
		{
			try { q.Cancel(); q.Dispose(); } catch { }
		}
		_activeQueries.Clear();

		foreach (var mv in _volumes.Values)
		{
			mv.Dispose();
		}
		_volumes.Clear();

		_serverCts.Dispose();
	}
}
