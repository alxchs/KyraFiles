// Copyright (c) Files Community. Licensed under the MIT License.

using System.Diagnostics;
using KyraFiles.App.Search.Index.Basic;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Tests;

/// <summary>
/// F015 T17a (listas de filhos, apagamento e busca de filho sem varrer o índice) e T17b (data das subpastas).
/// </summary>
[TestClass]
public sealed class ChildLinksAndReconcileTests
{
	private const uint DirAttr = 0x10;
	private const uint FileAttr = 0x20;

	[TestMethod]
	public void T17a_MarkDeleted_SameAliveSetAsFullScanReference()
	{
		for (int seed = 1; seed <= 20; seed++)
		{
			var random = new Random(seed);
			VolumeIndex linked = BuildRandomTree(seed, out List<int> dirs);
			VolumeIndex reference = BuildRandomTree(seed, out _);

			// Monta as listas de filhos cedo, para que as mudanças seguintes passem pela manutenção incremental
			_ = linked.FindByParentAndName(VolumeIndex.RootParentIndex, "x");

			for (int op = 0; op < 60; op++)
			{
				int kind = random.Next(4);
				if (kind == 0)
				{
					// Move uma pasta para outra (inclusive para pasta de índice maior); evita ciclos
					int moved = dirs[random.Next(dirs.Count)];
					int target = dirs[random.Next(dirs.Count)];
					if (linked.IsDeleted(moved) || linked.IsDeleted(target) || IsAncestorOrSelf(linked, moved, target))
						continue;

					linked.UpdateEntry(moved, target, linked.GetName(moved), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DirAttr);
					reference.UpdateEntry(moved, target, reference.GetName(moved), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DirAttr);
				}
				else if (kind == 1)
				{
					// Pasta nova com um arquivo, sob pasta existente
					int parent = dirs[random.Next(dirs.Count)];
					if (linked.IsDeleted(parent))
						continue;

					string name = $"novo_{seed}_{op}";
					int d1 = linked.Add(0, parent, name, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DirAttr);
					int d2 = reference.Add(0, parent, name, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DirAttr);
					Assert.AreEqual(d1, d2);
					dirs.Add(d1);
					linked.Add(0, d1, name + ".txt", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FileAttr);
					reference.Add(0, d2, name + ".txt", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, FileAttr);
				}
				else
				{
					int victim = random.Next(linked.Count);
					int a = linked.MarkDeleted(victim);
					int b = ReferenceMarkDeleted(reference, victim);
					Assert.AreEqual(b, a, $"seed {seed} op {op}: quantidade apagada difere");
				}
			}

			CollectionAssert.AreEqual(AliveSet(reference), AliveSet(linked), $"seed {seed}: itens vivos diferem");

			// A busca por filho dá o mesmo que a varredura linear
			for (int i = 0; i < linked.Count; i++)
			{
				if (linked.IsDeleted(i))
					continue;

				int parent = linked.GetParentIndex(i);
				int expected = ReferenceFindByParentAndName(reference, parent, reference.GetName(i));
				int actual = linked.FindByParentAndName(parent, linked.GetName(i).ToUpperInvariant());
				Assert.AreEqual(expected >= 0, actual >= 0, $"seed {seed}: filho {i} não encontrado");
				Assert.AreEqual(parent, linked.GetParentIndex(actual));
			}
		}
	}

	[TestMethod]
	public void T17a_Benchmark_TwoMillionItems_LocalRemovalAndDeletion()
	{
		const int Dirs = 100_000;
		const int FilesPerDir = 19;
		var now = DateTimeOffset.UtcNow;
		var index = new VolumeIndex(Guid.NewGuid(), "C:\\", initialCapacity: Dirs * (FilesPerDir + 1));

		var dirIdx = new int[Dirs];
		for (int d = 0; d < Dirs; d++)
		{
			int parent = d < 1000 ? VolumeIndex.RootParentIndex : dirIdx[d % 1000];
			dirIdx[d] = index.Add((ulong)(d + 1), parent, $"pasta_{d}", 0, now, now, DirAttr);
			for (int f = 0; f < FilesPerDir; f++)
				index.Add(0, dirIdx[d], $"arquivo_{d}_{f}.txt", f, now, now, FileAttr);
		}

		index.TrimExcess();
		Assert.AreEqual(Dirs * (FilesPerDir + 1), index.Count);

		var sw = Stopwatch.StartNew();

		// 50 mil pastas "revarridas": percorre os filhos de cada uma e apaga o primeiro arquivo (passada 2)
		int removed = 0;
		for (int d = 1000; d < 51_000; d++)
		{
			for (int c = index.GetFirstChild(dirIdx[d]); c != -1; c = index.GetNextSibling(c))
			{
				if (!index.IsDirectory(c))
				{
					removed += index.MarkDeleted(c);
					break;
				}
			}
		}

		// 1 mil pastas apagadas com a subárvore
		int deleted = 0;
		for (int d = 60_000; d < 61_000; d++)
			deleted += index.MarkDeleted(dirIdx[d]);

		sw.Stop();
		Console.WriteLine($"[T17a] 2 M itens: passada 2 em 50 mil pastas + 1 mil pastas apagadas = {sw.ElapsedMilliseconds} ms (removidos {removed}, apagados {deleted})");

		Assert.AreEqual(50_000, removed);
		Assert.AreEqual(1000 * (FilesPerDir + 1), deleted);
		Assert.IsTrue(sw.ElapsedMilliseconds < 2000, $"levou {sw.ElapsedMilliseconds} ms (meta < 2 s)");
	}

	[TestMethod]
	public void T17b_AC12_ChangeInParentAndChild_BothReflectedAfterReconcile()
	{
		string rootDir = Path.Combine(Path.GetTempPath(), "claude", "desenv", "utils", "FilesApp", "f015", "t17b_" + Guid.NewGuid().ToString("N")[..8]);
		string dirA = Path.Combine(rootDir, "A");
		string dirB = Path.Combine(dirA, "B");
		Directory.CreateDirectory(dirB);
		File.WriteAllText(Path.Combine(dirB, "fixo.txt"), "x");

		try
		{
			VolumeIndex index = DirectoryScanner.ScanDirectory(Guid.NewGuid(), rootDir);
			DateTimeOffset rootTime = new(Directory.GetLastWriteTimeUtc(rootDir), TimeSpan.Zero);

			// "App fechado": muda a pasta-mãe e a filha
			Thread.Sleep(100);
			File.WriteAllText(Path.Combine(dirA, "novo_em_A.txt"), "a");
			File.WriteAllText(Path.Combine(dirB, "novo_em_B.txt"), "b");

			ReconcileResult result = Reconciler.ReconcileVolume(index, ref rootTime);
			Console.WriteLine($"[T17b] verificadas {result.DirectoriesChecked}, relidas {result.DirectoriesRescanned}");

			Assert.AreEqual(1, CountAlive(index, "novo_em_A.txt"), "arquivo novo na pasta-mãe");
			Assert.AreEqual(1, CountAlive(index, "novo_em_B.txt"), "arquivo novo na subpasta (perdido antes da T17b)");
			Assert.AreEqual(1, CountAlive(index, "fixo.txt"));
		}
		finally
		{
			try { Directory.Delete(rootDir, recursive: true); } catch { }
		}
	}

	[TestMethod]
	public void T17a_Reconcile_FolderMovedIntoLaterFolderAndDeletedFolder()
	{
		string rootDir = Path.Combine(Path.GetTempPath(), "claude", "desenv", "utils", "FilesApp", "f015", "t17a_" + Guid.NewGuid().ToString("N")[..8]);
		Directory.CreateDirectory(Path.Combine(rootDir, "Origem", "Movida", "Funda"));
		Directory.CreateDirectory(Path.Combine(rootDir, "Destino"));
		Directory.CreateDirectory(Path.Combine(rootDir, "Condenada", "Sub"));
		File.WriteAllText(Path.Combine(rootDir, "Origem", "Movida", "Funda", "dentro.txt"), "x");
		File.WriteAllText(Path.Combine(rootDir, "Condenada", "Sub", "some.txt"), "x");

		try
		{
			VolumeIndex index = DirectoryScanner.ScanDirectory(Guid.NewGuid(), rootDir);
			DateTimeOffset rootTime = new(Directory.GetLastWriteTimeUtc(rootDir), TimeSpan.Zero);

			Thread.Sleep(100);
			Directory.Move(Path.Combine(rootDir, "Origem", "Movida"), Path.Combine(rootDir, "Destino", "Movida"));
			Directory.Delete(Path.Combine(rootDir, "Condenada"), recursive: true);

			Reconciler.ReconcileVolume(index, ref rootTime);

			Assert.AreEqual(0, CountAlive(index, "some.txt"));
			Assert.AreEqual(0, CountAlive(index, "Condenada"));
			Assert.AreEqual(1, CountAlive(index, "dentro.txt"));

			int dentro = FindAlive(index, "dentro.txt");
			Assert.AreEqual(Path.Combine(rootDir, "Destino", "Movida", "Funda", "dentro.txt"), index.GetFullPath(dentro), ignoreCase: true);
		}
		finally
		{
			try { Directory.Delete(rootDir, recursive: true); } catch { }
		}
	}

	[TestMethod]
	public void T17c_VolumeReconciler_BurstOfRequests_RunsOnceAfterQuietPeriod()
	{
		string rootDir = Path.Combine(Path.GetTempPath(), "claude", "desenv", "utils", "FilesApp", "f015", "t17c_" + Guid.NewGuid().ToString("N")[..8]);
		Directory.CreateDirectory(Path.Combine(rootDir, "Sub"));

		try
		{
			VolumeIndex index = DirectoryScanner.ScanDirectory(Guid.NewGuid(), rootDir);
			DateTimeOffset rootTime = new(Directory.GetLastWriteTimeUtc(rootDir), TimeSpan.Zero);
			using var completed = new ManualResetEventSlim(false);
			int callbacks = 0;

			using var reconciler = new VolumeReconciler(index, rootTime, TimeSpan.FromMilliseconds(300), (_, _) =>
			{
				Interlocked.Increment(ref callbacks);
				completed.Set();
			});

			// Rajada de estouros: um pedido a cada 50 ms durante 500 ms. A pausa antes do arquivo garante que a data
			// da pasta mude (a data de arquivo anda em tiques de ~15,6 ms; no mesmo tique a reconferência não vê nada)
			Thread.Sleep(100);
			File.WriteAllText(Path.Combine(rootDir, "Sub", "novo.txt"), "x");
			for (int i = 0; i < 10; i++)
			{
				reconciler.Request();
				Thread.Sleep(50);
			}

			Assert.IsTrue(completed.Wait(TimeSpan.FromSeconds(10)), "a reconferência deveria ter rodado");
			Thread.Sleep(600);

			Console.WriteLine($"[T17c] pedidos {reconciler.RequestCount}, rodadas {reconciler.RunCount}");
			Assert.AreEqual(10, reconciler.RequestCount);
			Assert.AreEqual(1, reconciler.RunCount, "dez pedidos seguidos devem gerar uma única reconferência");
			Assert.AreEqual(1, Volatile.Read(ref callbacks), "uma única notificação de fim");
			Assert.IsFalse(reconciler.IsPending || reconciler.IsRunning);
			Assert.AreEqual(1, CountAlive(index, "novo.txt"), "o arquivo novo deve entrar pela reconferência");
		}
		finally
		{
			try { Directory.Delete(rootDir, recursive: true); } catch { }
		}
	}

	private static VolumeIndex BuildRandomTree(int seed, out List<int> dirs)
	{
		var random = new Random(seed * 7919);
		var now = DateTimeOffset.UtcNow;
		var index = new VolumeIndex(Guid.NewGuid(), "C:\\", initialCapacity: 64);
		dirs = [];

		for (int i = 0; i < 400; i++)
		{
			int parent = dirs.Count == 0 || random.Next(5) == 0 ? VolumeIndex.RootParentIndex : dirs[random.Next(dirs.Count)];
			bool isDir = random.Next(3) == 0;
			int idx = index.Add(0, parent, $"item_{i}" + (isDir ? string.Empty : ".bin"), i, now, now, isDir ? DirAttr : FileAttr);
			if (isDir)
				dirs.Add(idx);
		}

		index.TrimExcess();
		return index;
	}

	private static bool IsAncestorOrSelf(VolumeIndex index, int candidate, int node)
	{
		for (int current = node, depth = 0; current >= 0 && depth < 1000; current = index.GetParentIndex(current), depth++)
		{
			if (current == candidate)
				return true;
		}

		return false;
	}

	/// <summary>
	/// O algoritmo anterior à T17a (varre o índice inteiro a cada nível), mantido como referência de resultado.
	/// </summary>
	private static int ReferenceMarkDeleted(VolumeIndex index, int target)
	{
		if (index.IsDeleted(target))
			return 0;

		var doomed = new HashSet<int> { target };
		bool added = index.IsDirectory(target);
		while (added)
		{
			added = false;
			for (int i = 0; i < index.Count; i++)
			{
				int p = index.GetParentIndex(i);
				if (p >= 0 && doomed.Contains(p) && doomed.Add(i))
					added = true;
			}
		}

		int count = 0;
		foreach (int i in doomed)
		{
			if (!index.IsDeleted(i))
				count++;
		}

		// Apaga de baixo para cima com a própria API (folhas primeiro, então cada chamada apaga um item)
		foreach (int i in doomed.OrderByDescending(i => Depth(index, i)))
			index.MarkDeleted(i);

		return count;
	}

	private static int Depth(VolumeIndex index, int node)
	{
		int depth = 0;
		for (int current = node; current >= 0 && depth < 1000; current = index.GetParentIndex(current))
			depth++;
		return depth;
	}

	private static int ReferenceFindByParentAndName(VolumeIndex index, int parent, string name)
	{
		for (int i = 0; i < index.Count; i++)
		{
			if (!index.IsDeleted(i) && index.GetParentIndex(i) == parent &&
				string.Equals(index.GetName(i), name, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return -1;
	}

	private static List<int> AliveSet(VolumeIndex index)
	{
		var alive = new List<int>();
		for (int i = 0; i < index.Count; i++)
		{
			if (!index.IsDeleted(i))
				alive.Add(i);
		}

		return alive;
	}

	private static int CountAlive(VolumeIndex index, string name)
	{
		int count = 0;
		for (int i = 0; i < index.Count; i++)
		{
			if (!index.IsDeleted(i) && string.Equals(index.GetName(i), name, StringComparison.OrdinalIgnoreCase))
				count++;
		}

		return count;
	}

	private static int FindAlive(VolumeIndex index, string name)
	{
		for (int i = 0; i < index.Count; i++)
		{
			if (!index.IsDeleted(i) && string.Equals(index.GetName(i), name, StringComparison.OrdinalIgnoreCase))
				return i;
		}

		return -1;
	}
}
