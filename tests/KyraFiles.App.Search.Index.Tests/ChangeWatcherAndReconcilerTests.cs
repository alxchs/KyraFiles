// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;
using KyraFiles.App.Search.Index.Basic;
using KyraFiles.App.Search.Index.Engine;
using KyraFiles.App.Search.Index.Models;
using KyraFiles.App.Search.Index.Query;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class ChangeWatcherAndReconcilerTests
{
	private static string GetTempTestDirectory()
	{
		string baseDir = Path.Combine(
			Path.GetTempPath(),
			"claude",
			"desenv",
			"utils",
			"KyraFiles",
			"f015",
			"watcher_" + Guid.NewGuid().ToString("N")[..8]);

		Directory.CreateDirectory(baseDir);
		return baseDir;
	}

	private static List<SearchResultItem> QueryAll(VolumeIndex index, string query)
	{
		var results = new List<SearchResultItem>();
		lock (index.SyncRoot)
		{
			new QueryEngine().Execute(index, new QueryMatcher(query), batch => results.AddRange(batch));
		}

		return results;
	}

	[TestMethod]
	public void ChangeWatcher_CreateRenameMoveDelete_ReflectedWithinFiveSeconds_AC10()
	{
		string rootDir = GetTempTestDirectory();
		try
		{
			string dirA = Path.Combine(rootDir, "PastaA");
			string dirB = Path.Combine(rootDir, "PastaB");
			Directory.CreateDirectory(dirA);
			Directory.CreateDirectory(dirB);

			string fileToRename = Path.Combine(dirA, "antes_renomear.txt");
			string fileToMove = Path.Combine(dirA, "arquivo_mover.txt");
			string fileToDelete = Path.Combine(dirB, "arquivo_apagar.txt");

			File.WriteAllText(fileToRename, "1");
			File.WriteAllText(fileToMove, "2");
			File.WriteAllText(fileToDelete, "3");

			var index = DirectoryScanner.ScanDirectory(Guid.NewGuid(), rootDir);
			using var watcher = new ChangeWatcher(index);
			watcher.Start();

			var sw = Stopwatch.StartNew();

			// 1. Cria
			string createdFile = Path.Combine(dirA, "recem_criado_ac10.txt");
			File.WriteAllText(createdFile, "novo");

			// 2. Renomeia
			string renamedFile = Path.Combine(dirA, "depois_renomeado_ac10.txt");
			File.Move(fileToRename, renamedFile);

			// 3. Move entre pastas
			string movedFile = Path.Combine(dirB, "arquivo_mover.txt");
			File.Move(fileToMove, movedFile);

			// 4. Apaga
			File.Delete(fileToDelete);

			// Aguarda reflexo no índice (meta AC-10 <= 5000 ms)
			bool allReflected = false;
			while (sw.ElapsedMilliseconds < 5000)
			{
				var c1 = QueryAll(index, "recem_criado_ac10.txt");
				var rOld = QueryAll(index, "antes_renomear.txt");
				var rNew = QueryAll(index, "depois_renomeado_ac10.txt");
				var m1 = QueryAll(index, "arquivo_mover.txt");
				var d1 = QueryAll(index, "arquivo_apagar.txt");

				if (c1.Count == 1 &&
					rOld.Count == 0 &&
					rNew.Count == 1 &&
					m1.Count == 1 && m1[0].FullPath.Equals(movedFile, StringComparison.OrdinalIgnoreCase) &&
					d1.Count == 0)
				{
					allReflected = true;
					break;
				}

				Thread.Sleep(25);
			}

			sw.Stop();
			Console.WriteLine($"[T9 AC-10] Alterações (criar/renomear/mover/apagar) refletidas em {sw.ElapsedMilliseconds} ms (meta <= 5000 ms).");
			Assert.IsTrue(allReflected, $"Alterações não refletiram em 5 s (elapsed={sw.ElapsedMilliseconds} ms).");
		}
		finally
		{
			try { Directory.Delete(rootDir, recursive: true); } catch { }
		}
	}

	[TestMethod]
	public void Reconciler_WhileClosed_ReconcilesOnlyChangedFoldersWithoutFullReindex_AC12()
	{
		string rootDir = GetTempTestDirectory();
		try
		{
			const int totalSubfolders = 20;
			var folders = new string[totalSubfolders];
			for (int i = 0; i < totalSubfolders; i++)
			{
				folders[i] = Path.Combine(rootDir, $"Pasta_{i:D2}");
				Directory.CreateDirectory(folders[i]);
				File.WriteAllText(Path.Combine(folders[i], $"fixo_{i:D2}.txt"), "conteudo");
			}

			string fileToRename = Path.Combine(folders[5], "antigo_05.txt");
			string fileToMove = Path.Combine(folders[10], "viajante_10.txt");
			string fileToDelete = Path.Combine(folders[15], "condenado_15.txt");
			File.WriteAllText(fileToRename, "r");
			File.WriteAllText(fileToMove, "m");
			File.WriteAllText(fileToDelete, "d");

			// Indexação inicial e salvamento do snapshot ("app fecha")
			Guid volGuid = Guid.NewGuid();
			var initialIndex = DirectoryScanner.ScanDirectory(volGuid, rootDir);
			string snapshotPath = Path.Combine(Path.GetTempPath(), $"f015_snap_{volGuid:N}.fcsn");
			DateTimeOffset initialRootTime = new(Directory.GetLastWriteTimeUtc(rootDir), TimeSpan.Zero);
			VolumeSnapshot.Save(snapshotPath, initialIndex, rootLastWriteTime: initialRootTime);

			// Garante avanço do relógio de arquivo antes de modificar apenas 5 das 20 pastas
			Thread.Sleep(100);

			// Mudanças com o app fechado em apenas 5 pastas (1, 5, 10, 11, 15):
			File.WriteAllText(Path.Combine(folders[1], "criado_fechado_01.txt"), "novo");
			File.Move(fileToRename, Path.Combine(folders[5], "renomeado_fechado_05.txt"));
			string movedDest = Path.Combine(folders[11], "viajante_10.txt");
			File.Move(fileToMove, movedDest);
			File.Delete(fileToDelete);

			// "App reabre": carrega o snapshot e executa Reconciler
			Assert.IsTrue(VolumeSnapshot.TryLoad(snapshotPath, out VolumeSnapshotData? loaded));
			File.Delete(snapshotPath);
			Assert.IsNotNull(loaded);

			DateTimeOffset rootTime = loaded.RootLastWriteTime;
			var sw = Stopwatch.StartNew();
			ReconcileResult result = Reconciler.ReconcileVolume(loaded.Index, ref rootTime);
			sw.Stop();

			Console.WriteLine($"[T9 AC-12] Pastas verificadas: {result.DirectoriesChecked} | Pastas relidas: {result.DirectoriesRescanned} | Tempo: {sw.ElapsedMilliseconds} ms");

			// Apenas as 5 subpastas alteradas (1, 5, 10, 11, 15) devem ser relidas dentre as 21 pastas verificadas
			Assert.AreEqual(21, result.DirectoriesChecked);
			Assert.AreEqual(5, result.DirectoriesRescanned, "Apenas as 5 pastas com data modificada devem ser relidas (sem reindexar as outras 16).");

			Assert.AreEqual(1, QueryAll(loaded.Index, "criado_fechado_01.txt").Count);
			Assert.AreEqual(0, QueryAll(loaded.Index, "antigo_05.txt").Count);
			Assert.AreEqual(1, QueryAll(loaded.Index, "renomeado_fechado_05.txt").Count);

			var movedMatches = QueryAll(loaded.Index, "viajante_10.txt");
			Assert.AreEqual(1, movedMatches.Count);
			Assert.AreEqual(movedDest, movedMatches[0].FullPath, ignoreCase: true);

			Assert.AreEqual(0, QueryAll(loaded.Index, "condenado_15.txt").Count);
		}
		finally
		{
			try { Directory.Delete(rootDir, recursive: true); } catch { }
		}
	}
}
