// Copyright (c) Files Community. Licensed under the MIT License.

using System.IO.Pipes;
using System.Text.Json;

namespace KyraFiles.App.Search.Index.Ipc;

/// <summary>
/// Cliente assíncrono de Named Pipe para comunicação entre o Files e o <c>KyraFiles.App.IndexService.exe</c>
/// (F015 T10 — FR-040, FR-042).
/// </summary>
public sealed class IndexPipeClient
{
	private readonly string _pipeName;
	private readonly int _connectTimeoutMs;

	public string PipeName => _pipeName;

	public IndexPipeClient(string? pipeName = null, int connectTimeoutMs = 1500)
	{
		_pipeName = string.IsNullOrWhiteSpace(pipeName)
			? IndexPipeProtocol.GetBasicPipeName()
			: pipeName;
		_connectTimeoutMs = connectTimeoutMs;
	}

	public async Task<(bool IsAlive, int ServerProcessId, string? Mode)> PingAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var resp = await SendSingleCommandAsync(new IndexPipeRequest
			{
				Command = "ping",
				RequestId = Guid.NewGuid().ToString("N")
			}, cancellationToken).ConfigureAwait(false);

			if (resp is { Type: "pong" })
				return (true, resp.ServerProcessId, resp.Mode);
		}
		catch
		{
		}

		return (false, 0, null);
	}

	public Task<IndexPipeResponse?> GetStatusAsync(CancellationToken cancellationToken = default)
		=> SendSingleCommandAsync(new IndexPipeRequest
		{
			Command = "status",
			RequestId = Guid.NewGuid().ToString("N")
		}, cancellationToken);

	public Task<IndexPipeResponse?> ConfigureAsync(string[] excludedPaths, CancellationToken cancellationToken = default)
		=> SendSingleCommandAsync(new IndexPipeRequest
		{
			Command = "configure",
			RequestId = Guid.NewGuid().ToString("N"),
			ExcludedPaths = excludedPaths
		}, cancellationToken);

	public Task<IndexPipeResponse?> ReindexAsync(Guid? volumeGuid = null, CancellationToken cancellationToken = default)
		=> SendSingleCommandAsync(new IndexPipeRequest
		{
			Command = "reindex",
			RequestId = Guid.NewGuid().ToString("N"),
			VolumeGuid = volumeGuid
		}, cancellationToken);

	public Task<IndexPipeResponse?> ShutdownServerAsync(CancellationToken cancellationToken = default)
		=> SendSingleCommandAsync(new IndexPipeRequest
		{
			Command = "shutdown",
			RequestId = Guid.NewGuid().ToString("N")
		}, cancellationToken);

	/// <summary>
	/// Executa uma consulta com entrega progressiva de lotes pelo Named Pipe e suporte a cancelamento em voo.
	/// </summary>
	public async Task<IndexPipeResponse> QueryAsync(
		string query,
		string? scopePath,
		int maxResults,
		Action<IReadOnlyList<SearchResultDto>> onBatch,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(onBatch);

		string requestId = Guid.NewGuid().ToString("N");

		using var clientStream = new NamedPipeClientStream(
			".",
			_pipeName,
			PipeDirection.InOut,
			PipeOptions.Asynchronous);

		await clientStream.ConnectAsync(_connectTimeoutMs, cancellationToken).ConfigureAwait(false);

		using var reader = new StreamReader(clientStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 16384, leaveOpen: true);
		using var writer = new StreamWriter(clientStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4096, leaveOpen: true) { AutoFlush = true };

		var req = new IndexPipeRequest
		{
			Command = "query",
			RequestId = requestId,
			Query = query,
			ScopePath = scopePath,
			MaxResults = maxResults
		};

		string reqJson = JsonSerializer.Serialize(req, IndexPipeJsonContext.Default.IndexPipeRequest);
		await writer.WriteLineAsync(reqJson).ConfigureAwait(false);

		// Se o token cancelar no meio da leitura, envia comando "cancel" na conexão
		await using var cancelReg = cancellationToken.Register(() =>
		{
			try
			{
				if (clientStream.IsConnected)
				{
					string cancelJson = JsonSerializer.Serialize(new IndexPipeRequest
					{
						Command = "cancel",
						RequestId = requestId
					}, IndexPipeJsonContext.Default.IndexPipeRequest);
					writer.WriteLine(cancelJson);
				}
			}
			catch
			{
			}
		});

		while (clientStream.IsConnected)
		{
			string? line = await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false);
			if (line is null)
				break;

			if (string.IsNullOrWhiteSpace(line))
				continue;

			IndexPipeResponse? resp = JsonSerializer.Deserialize(line, IndexPipeJsonContext.Default.IndexPipeResponse);
			if (resp is null)
				continue;

			if (resp.Type == "batch" && resp.Items is { Length: > 0 })
			{
				onBatch(resp.Items);
			}
			else if (resp.Type is "completed" or "error")
			{
				return resp;
			}
		}

		return new IndexPipeResponse
		{
			Type = "completed",
			RequestId = requestId,
			IsCancelled = cancellationToken.IsCancellationRequested
		};
	}

	private async Task<IndexPipeResponse?> SendSingleCommandAsync(IndexPipeRequest request, CancellationToken cancellationToken)
	{
		using var clientStream = new NamedPipeClientStream(
			".",
			_pipeName,
			PipeDirection.InOut,
			PipeOptions.Asynchronous);

		await clientStream.ConnectAsync(_connectTimeoutMs, cancellationToken).ConfigureAwait(false);

		using var reader = new StreamReader(clientStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
		using var writer = new StreamWriter(clientStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 4096, leaveOpen: true) { AutoFlush = true };

		string json = JsonSerializer.Serialize(request, IndexPipeJsonContext.Default.IndexPipeRequest);
		await writer.WriteLineAsync(json).ConfigureAwait(false);

		string? respLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
		if (string.IsNullOrWhiteSpace(respLine))
			return null;

		return JsonSerializer.Deserialize(respLine, IndexPipeJsonContext.Default.IndexPipeResponse);
	}
}
