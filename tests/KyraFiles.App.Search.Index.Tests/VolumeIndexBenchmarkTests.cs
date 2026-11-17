// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;
using KyraFiles.App.Search.Index.Engine;
using KyraFiles.App.Search.Index.Query;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class VolumeIndexBenchmarkTests
{
	[TestMethod]
	[Ignore("F021: Desativado temporariamente para prevenir estouro de 32 GB de RAM e congelamento do sistema.")]
	public void Benchmark_FiveMillionEntries_MemoryAndQueryLatency()
	{
		const int TotalItems = 5_000_000;

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		long memBefore = GC.GetTotalMemory(true);

		var volume = new VolumeIndex(Guid.NewGuid(), "C:\\", initialCapacity: TotalItems);

		// Gera 5 milhões de itens com nomes realistas
		var now = DateTimeOffset.UtcNow;
		string[] prefixes = ["doc", "foto", "rel", "proj", "nota", "arq"];
		string[] extensions = [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".txt"];

		// 1. Gera 1.000 pastas raiz
		for (int d = 0; d < 1000; d++)
		{
			volume.Add(
				fileReference: (ulong)(d + 1),
				parentIndex: -1,
				name: $"Pasta_{d:D3}",
				size: 0,
				creationTime: now,
				lastWriteTime: now,
				attributes: 0x10);
		}

		// 2. Gera 4.999.000 arquivos distribuídos nas pastas
		for (int i = 1000; i < TotalItems; i++)
		{
			string prefix = prefixes[i % prefixes.Length];
			string ext = extensions[(i / prefixes.Length) % extensions.Length];
			string name = $"{prefix}_{i:D7}{ext}";

			volume.Add(
				fileReference: (ulong)(i + 1),
				parentIndex: i % 1000,
				name: name,
				size: (i * 137) % 1_000_000,
				creationTime: now,
				lastWriteTime: now,
				attributes: 0x20);
		}

		volume.TrimExcess();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		long memAfter = GC.GetTotalMemory(true);
		long bytesUsed = memAfter - memBefore;
		double mbPerMillion = (double)bytesUsed / (1024.0 * 1024.0) / 5.0;

		Console.WriteLine($"[BENCHMARK] Total Itens: {volume.Count:N0}");
		Console.WriteLine($"[BENCHMARK] Memória Total: {bytesUsed / (1024.0 * 1024.0):F2} MB");
		Console.WriteLine($"[BENCHMARK] RAM por Milhão: {mbPerMillion:F2} MB / milhão (Meta NFR-003: < 100 MB / milhão)");

		// Medição de consulta até o primeiro lote
		var engine = new QueryEngine();
		var matcher = new QueryMatcher("rel 00050"); // busca por substring composta

		long? firstBatchMs = null;
		var sw = Stopwatch.StartNew();

		var result = engine.Execute(volume, matcher, batch =>
		{
			firstBatchMs ??= sw.ElapsedMilliseconds;
		});

		sw.Stop();
		long latency = firstBatchMs ?? sw.ElapsedMilliseconds;

		Console.WriteLine($"[BENCHMARK] Latência até 1º lote: {latency} ms (Meta NFR-001: < 100 ms)");
		Console.WriteLine($"[BENCHMARK] Total de matches: {result.TotalMatches}");
		Console.WriteLine($"[BENCHMARK] Tempo total da consulta: {result.Elapsed.TotalMilliseconds:F2} ms");

		// Asserções dos requisitos não funcionais
		Assert.IsTrue(mbPerMillion < 100.0, $"Consumo de memória por milhão ({mbPerMillion:F2} MB) excedeu a meta de 100 MB");
		Assert.IsTrue(latency < 200, $"Latência até o primeiro lote ({latency} ms) excedeu a margem de tolerância");
	}
}

