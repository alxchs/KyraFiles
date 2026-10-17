// Copyright (c) Files Community. Licensed under the MIT License.

using Files.App.Search.Index.Interop;
using Files.App.Search.Index.Storage;

namespace Files.App.Search.Index.Basic;

/// <summary>
/// Um reconciliador por volume, dono da reconferência da abertura e da que segue um estouro do
/// <see cref="ChangeWatcher"/> (F015 T17c — NFR-005, FR-030, FR-032).
/// </summary>
/// <remarks>
/// Um pedido só marca "pendente"; a reconferência roda numa thread própria em prioridade de segundo plano,
/// depois de um intervalo sem novos pedidos. Pedidos que chegam durante a execução geram uma única nova rodada.
/// Antes, cada estouro revarria o volume inteiro na thread do watcher, e os estouros se acumulavam sem fim.
/// </remarks>
public sealed class VolumeReconciler : IDisposable
{
	private readonly VolumeIndex _index;
	private readonly TimeSpan _quietPeriod;
	private readonly Action<VolumeReconciler, ReconcileResult>? _onCompleted;
	private readonly Lock _gate = new();
	private readonly AutoResetEvent _signal = new(false);
	private readonly CancellationTokenSource _cts = new();
	private readonly Thread _thread;

	private DateTimeOffset _rootLastWriteTime;
	private long _dueTicks;
	private bool _pending;
	private volatile bool _running;
	private int _requestCount;
	private int _runCount;
	private bool _disposed;

	public VolumeReconciler(
		VolumeIndex index,
		DateTimeOffset rootLastWriteTime,
		TimeSpan quietPeriod,
		Action<VolumeReconciler, ReconcileResult>? onCompleted = null)
	{
		ArgumentNullException.ThrowIfNull(index);
		_index = index;
		_rootLastWriteTime = rootLastWriteTime;
		_quietPeriod = quietPeriod;
		_onCompleted = onCompleted;

		_thread = new Thread(Run)
		{
			IsBackground = true,
			Name = $"FilesIndexReconciler:{index.VolumeRoot}"
		};
		_thread.Start();
	}

	public VolumeIndex Index => _index;

	/// <summary>Há reconferência pedida e ainda não iniciada.</summary>
	public bool IsPending
	{
		get { lock (_gate) return _pending; }
	}

	/// <summary>Uma reconferência está em andamento.</summary>
	public bool IsRunning => _running;

	public int RequestCount => Volatile.Read(ref _requestCount);

	public int RunCount => Volatile.Read(ref _runCount);

	public DateTimeOffset RootLastWriteTime
	{
		get { lock (_gate) return _rootLastWriteTime; }
	}

	/// <summary>
	/// Pede uma reconferência depois do intervalo de silêncio (cada novo pedido adia o início).
	/// </summary>
	public void Request() => Schedule(_quietPeriod);

	/// <summary>
	/// Pede uma reconferência sem esperar (abertura com snapshot).
	/// </summary>
	public void RequestNow() => Schedule(TimeSpan.Zero);

	private void Schedule(TimeSpan delay)
	{
		if (_disposed)
			return;

		lock (_gate)
		{
			_pending = true;
			_dueTicks = Environment.TickCount64 + (long)delay.TotalMilliseconds;
			_requestCount++;
		}

		_signal.Set();
	}

	private void Run()
	{
		// Thread dedicada: fica em segundo plano (CPU e disco) a vida toda
		NativeMethods.SetThreadPriority(NativeMethods.GetCurrentThread(), NativeMethods.THREAD_MODE_BACKGROUND_BEGIN);
		CancellationToken token = _cts.Token;
		WaitHandle[] waits = [_signal, token.WaitHandle];

		try
		{
			while (!token.IsCancellationRequested)
			{
				WaitHandle.WaitAny(waits);

				// Espera o intervalo de silêncio; um pedido novo empurra o início para frente
				while (!token.IsCancellationRequested)
				{
					long remaining;
					lock (_gate)
					{
						if (!_pending)
							break;

						remaining = _dueTicks - Environment.TickCount64;
						if (remaining <= 0)
						{
							_pending = false;
							_running = true;
							break;
						}
					}

					token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(Math.Min(remaining, int.MaxValue)));
				}

				if (!_running || token.IsCancellationRequested)
					continue;

				try
				{
					DateTimeOffset rootTime = RootLastWriteTime;
					ReconcileResult result = Reconciler.ReconcileVolume(_index, ref rootTime, token);
					lock (_gate)
						_rootLastWriteTime = rootTime;

					Interlocked.Increment(ref _runCount);

					// Continua "em andamento" durante o retorno (gravação do snapshot): o status só diz pronto depois dele
					if (!token.IsCancellationRequested)
						_onCompleted?.Invoke(this, result);
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"[VolumeReconciler] {_index.VolumeRoot}: {ex.Message}");
				}
				finally
				{
					_running = false;
				}
			}
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_cts.Cancel();
		_thread.Join(TimeSpan.FromSeconds(5));
		_signal.Dispose();
		_cts.Dispose();
	}
}
