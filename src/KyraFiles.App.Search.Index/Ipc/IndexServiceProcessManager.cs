// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;

namespace KyraFiles.App.Search.Index.Ipc;

/// <summary>
/// Inicia o processo <c>KyraFiles.App.IndexService.exe</c> fora do processo do Files (visível no Gerenciador de Tarefas)
/// e o reinicia automaticamente se cair (F015 T10 — FR-040, NFR-003).
/// </summary>
public sealed class IndexServiceProcessManager : IDisposable
{
	private readonly string _executablePath;
	private readonly string _pipeName;
	private readonly string? _cacheDirectory;
	private readonly string[] _customRoots;
	private readonly IndexPipeClient _client;
	private readonly CancellationTokenSource _cts = new();
	private readonly Lock _sync = new();

	private Process? _managedProcess;
	private Task? _supervisionTask;
	private int _restartCount;
	private bool _disposed;

	public string PipeName => _pipeName;
	public IndexPipeClient Client => _client;
	public int RestartCount => Volatile.Read(ref _restartCount);
	public int CurrentProcessId => _managedProcess is { HasExited: false } p ? p.Id : 0;

	public IndexServiceProcessManager(
		string? executablePath = null,
		string? pipeName = null,
		string? cacheDirectory = null,
		string[]? customRoots = null)
	{
		_executablePath = string.IsNullOrWhiteSpace(executablePath)
			? ResolveDefaultExecutablePath()
			: executablePath;
		_pipeName = string.IsNullOrWhiteSpace(pipeName)
			? IndexPipeProtocol.GetBasicPipeName()
			: pipeName;
		_cacheDirectory = cacheDirectory;
		_customRoots = customRoots ?? [];
		_client = new IndexPipeClient(_pipeName);
	}

	public static string ResolveDefaultExecutablePath()
	{
		string baseDir = AppContext.BaseDirectory;
		string candidateSub = Path.Combine(baseDir, "KyraFiles.App.IndexService", "KyraFiles.App.IndexService.exe");
		if (File.Exists(candidateSub))
			return candidateSub;

		string candidateRoot = Path.Combine(baseDir, "KyraFiles.App.IndexService.exe");
		if (File.Exists(candidateRoot))
			return candidateRoot;

		return candidateSub;
	}

	/// <summary>
	/// Garante que o processo <c>KyraFiles.App.IndexService.exe</c> está rodando e respondendo no pipe,
	/// e ativa a supervisão automática para reiniciá-lo caso caia.
	/// </summary>
	public async Task<bool> EnsureRunningAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		var (isAlive, _, _) = await _client.PingAsync(cancellationToken).ConfigureAwait(false);
		if (!isAlive)
		{
			if (!StartProcess())
				return false;

			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < 5000 && !cancellationToken.IsCancellationRequested)
			{
				(isAlive, _, _) = await _client.PingAsync(cancellationToken).ConfigureAwait(false);
				if (isAlive)
					break;
				await Task.Delay(50, cancellationToken).ConfigureAwait(false);
			}
		}

		lock (_sync)
		{
			_supervisionTask ??= Task.Run(SuperviseLoopAsync);
		}

		return isAlive;
	}

	private bool StartProcess()
	{
		if (!File.Exists(_executablePath))
			return false;

		lock (_sync)
		{
			if (_managedProcess is { HasExited: false })
				return true;

			var args = new StringBuilder($"--mode Basic --pipe \"{_pipeName}\"");
			if (!string.IsNullOrWhiteSpace(_cacheDirectory))
			{
				args.Append($" --cache \"{_cacheDirectory}\"");
			}

			foreach (string root in _customRoots)
			{
				args.Append($" --root \"{root}\"");
			}

			var psi = new ProcessStartInfo(_executablePath, args.ToString())
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Path.GetDirectoryName(_executablePath) ?? AppContext.BaseDirectory
			};

			_managedProcess = Process.Start(psi);
			return _managedProcess is not null;
		}
	}

	private async Task SuperviseLoopAsync()
	{
		while (!_cts.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(300, _cts.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				break;
			}

			if (_cts.IsCancellationRequested)
				break;

			bool exited = false;
			lock (_sync)
			{
				if (_managedProcess is not null && _managedProcess.HasExited)
				{
					exited = true;
					_managedProcess.Dispose();
					_managedProcess = null;
				}
			}

			if (exited)
			{
				var (alive, _, _) = await _client.PingAsync(_cts.Token).ConfigureAwait(false);
				if (!alive && !_cts.IsCancellationRequested)
				{
					if (StartProcess())
					{
						Interlocked.Increment(ref _restartCount);
					}
				}
			}
		}
	}

	public void StopAndDisposeProcess()
	{
		_cts.Cancel();

		try
		{
			_ = _client.ShutdownServerAsync().GetAwaiter().GetResult();
		}
		catch
		{
		}

		lock (_sync)
		{
			if (_managedProcess is not null)
			{
				try
				{
					if (!_managedProcess.WaitForExit(1000))
					{
						_managedProcess.Kill(entireProcessTree: true);
					}
				}
				catch
				{
				}
				finally
				{
					_managedProcess.Dispose();
					_managedProcess = null;
				}
			}
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_cts.Cancel();
		_cts.Dispose();
	}
}

