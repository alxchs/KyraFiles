// Copyright (c) Files Community. Licensed under the MIT License.

using Files.App.Search.Index.Basic;
using Files.App.Search.Index.Ipc;

namespace Files.App.IndexService;

internal static class Program
{
	public static async Task<int> Main(string[] args)
	{
		string mode = "Basic";
		string? pipeName = null;
		string? cacheDir = null;
		var customRoots = new List<string>();

		for (int i = 0; i < args.Length; i++)
		{
			string arg = args[i];
			if (arg.Equals("--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				mode = args[++i];
			}
			else if (arg.Equals("--pipe", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				pipeName = args[++i];
			}
			else if (arg.Equals("--cache", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				cacheDir = args[++i];
			}
			else if (arg.Equals("--root", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
			{
				customRoots.Add(args[++i]);
			}
		}

		string effectivePipe = pipeName ?? IndexPipeProtocol.GetBasicPipeName();

		// Garante instância única por nome de pipe usando Mutex nomeado local
		string mutexName = $"Local\\Mutex_{effectivePipe}";
		using var singleInstanceMutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
		if (!createdNew)
		{
			return 0;
		}

		using var server = new IndexPipeServer(effectivePipe, mode, cacheDir);

		if (customRoots.Count > 0)
		{
			// Modo direcionado (ex.: testes de integração ou escopo específico). As pastas são registradas antes
			// de abrir o pipe: quem recebe o primeiro "ping" já encontra os volumes no "status".
			foreach (string root in customRoots)
			{
				if (Directory.Exists(root))
				{
					var idx = DirectoryScanner.ScanDirectory(Guid.NewGuid(), root);
					server.RegisterVolume(idx, "NTFS", startWatcher: true, state: "Ready");
				}
			}
		}
		else
		{
			server.StartVolumeIndexingInBackground();
		}

		server.Start();

		try
		{
			await Task.Delay(Timeout.Infinite, server.ShutdownToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
		}

		return 0;
	}
}
