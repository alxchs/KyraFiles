// Copyright (c) Files Community. Licensed under the MIT License.

using System.Diagnostics;
using Files.App.Search.Index.Ipc;

namespace Files.App.Search.Index.Tests;

[TestClass]
public sealed class IndexServiceIpcTests
{
	private static string GetTempTestDirectory()
	{
		string baseDir = Path.Combine(
			Path.GetTempPath(),
			"claude",
			"desenv",
			"utils",
			"FilesApp",
			"f015",
			"ipc_" + Guid.NewGuid().ToString("N")[..8]);

		Directory.CreateDirectory(baseDir);
		return baseDir;
	}

	[TestMethod]
	public async Task IndexServiceExe_StartsOutOfProcess_QueriesConfiguresAndRestartsOnCrash_T10()
	{
		string rootDir = GetTempTestDirectory();
		string cacheDir = Path.Combine(rootDir, "_cache");
		string subInclude = Path.Combine(rootDir, "Incluida");
		string subExclude = Path.Combine(rootDir, "Excluida");
		Directory.CreateDirectory(subInclude);
		Directory.CreateDirectory(subExclude);

		File.WriteAllText(Path.Combine(subInclude, "documento_visivel_t10.txt"), "ok");
		File.WriteAllText(Path.Combine(subExclude, "documento_oculto_t10.txt"), "excluir");

		string pipeName = $"FilesCustom.TestPipe.{Guid.NewGuid():N}";
		string exePath = Path.Combine(AppContext.BaseDirectory, "KyraFiles.App.IndexService.exe");
		Assert.IsTrue(File.Exists(exePath), $"Executável não encontrado em {exePath}");

		using var manager = new IndexServiceProcessManager(
			executablePath: exePath,
			pipeName: pipeName,
			cacheDirectory: cacheDir,
			customRoots: [rootDir]);

		try
		{
			bool started = await manager.EnsureRunningAsync();
			Assert.IsTrue(started, "KyraFiles.App.IndexService.exe deve iniciar e responder no pipe.");

			var (alive, serverPid, mode) = await manager.Client.PingAsync();
			Assert.IsTrue(alive);
			Assert.AreEqual("Basic", mode);
			Assert.AreNotEqual(Environment.ProcessId, serverPid, "O serviço deve rodar em processo separado.");

			using (var proc = Process.GetProcessById(serverPid))
			{
				Assert.AreEqual("KyraFiles.App.IndexService", proc.ProcessName);
			}

			// 1. Status (FR-042)
			var status = await manager.Client.GetStatusAsync();
			Assert.IsNotNull(status);
			Assert.IsNotNull(status.Volumes);
			Assert.AreEqual(1, status.Volumes.Length);
			Assert.AreEqual("Ready", status.Volumes[0].State);

			// 2. Consulta por lote pelo pipe (FR-040)
			var items = new List<SearchResultDto>();
			var queryResp = await manager.Client.QueryAsync(
				"documento",
				rootDir,
				100,
				batch => items.AddRange(batch));

			Assert.AreEqual("completed", queryResp.Type);
			Assert.AreEqual(2, items.Count);

			// 3. Configuração de exclusão de pasta pelo pipe (FR-003)
			await manager.Client.ConfigureAsync([subExclude]);

			var filteredItems = new List<SearchResultDto>();
			await manager.Client.QueryAsync(
				"documento",
				rootDir,
				100,
				batch => filteredItems.AddRange(batch));

			Assert.AreEqual(1, filteredItems.Count, "A pasta excluída não deve aparecer nos resultados.");
			Assert.AreEqual("documento_visivel_t10.txt", filteredItems[0].Name);

			// 4. Reinício automático se o processo cair (FR-040)
			using (var procToKill = Process.GetProcessById(serverPid))
			{
				procToKill.Kill();
				procToKill.WaitForExit(2000);
			}

			var sw = Stopwatch.StartNew();
			int newPid = 0;
			while (sw.ElapsedMilliseconds < 4000)
			{
				var (aliveAfter, pidAfter, _) = await manager.Client.PingAsync();
				if (aliveAfter && pidAfter != 0 && pidAfter != serverPid)
				{
					newPid = pidAfter;
					break;
				}
				await Task.Delay(100);
			}

			Assert.AreNotEqual(0, newPid, "O processo KyraFiles.App.IndexService.exe deve ser reiniciado automaticamente após queda.");
			Assert.IsTrue(manager.RestartCount >= 1);
		}
		finally
		{
			manager.StopAndDisposeProcess();
			try { Directory.Delete(rootDir, recursive: true); } catch { }
		}
	}
}
