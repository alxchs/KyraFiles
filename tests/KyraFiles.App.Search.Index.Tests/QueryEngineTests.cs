// Copyright (c) Files Community. Licensed under the MIT License.

using Files.App.Search.Index.Engine;
using Files.App.Search.Index.Models;
using Files.App.Search.Index.Query;
using Files.App.Search.Index.Storage;

namespace Files.App.Search.Index.Tests;

[TestClass]
public sealed class QueryEngineTests
{
	[TestMethod]
	public void Execute_WholeVolume_FindsMatches()
	{
		var volume = new VolumeIndex(Guid.NewGuid(), "C:\\");
		int dir = volume.Add(1, -1, "Docs", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);
		volume.Add(2, dir, "Relatório_2026.pdf", 500, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		volume.Add(3, dir, "NotaFiscal.xml", 200, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		volume.Add(4, -1, "relatorio_antigo.docx", 300, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		var engine = new QueryEngine();
		var matcher = new QueryMatcher("relatorio");
		var results = new List<SearchResultItem>();

		var result = engine.Execute(volume, matcher, batch => results.AddRange(batch));

		Assert.AreEqual(2, result.TotalMatches);
		Assert.AreEqual(2, results.Count);
		Assert.IsFalse(result.IsTruncated);
		Assert.IsFalse(result.IsCancelled);
	}

	[TestMethod]
	public void Execute_ScopedSubtree_RestrictsToDescendants()
	{
		var volume = new VolumeIndex(Guid.NewGuid(), "C:\\");
		int dirA = volume.Add(1, -1, "PastaA", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);
		int subA = volume.Add(2, dirA, "SubA", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);
		int dirB = volume.Add(3, -1, "PastaB", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);

		volume.Add(10, dirA, "arquivo_teste.txt", 100, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		volume.Add(11, subA, "segundo_teste.txt", 200, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		volume.Add(12, dirB, "terceiro_teste.txt", 300, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		var engine = new QueryEngine();
		var matcher = new QueryMatcher("teste");
		var results = new List<SearchResultItem>();

		// Busca escopada apenas na PastaA
		var result = engine.Execute(volume, matcher, batch => results.AddRange(batch), scopeParentIndex: dirA);

		Assert.AreEqual(2, result.TotalMatches);
		Assert.IsTrue(results.All(r => r.FullPath.StartsWith("C:\\PastaA", StringComparison.OrdinalIgnoreCase)));
	}

	[TestMethod]
	public void Execute_Batches_DeliverProgressively()
	{
		var volume = new VolumeIndex(Guid.NewGuid(), "C:\\", initialCapacity: 500);
		for (int i = 0; i < 450; i++)
		{
			volume.Add((ulong)i, -1, $"item_teste_{i}.txt", 100, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		}

		var engine = new QueryEngine();
		var matcher = new QueryMatcher("teste");
		int batchCount = 0;
		int totalItemsReceived = 0;

		engine.Execute(volume, matcher, batch =>
		{
			batchCount++;
			totalItemsReceived += batch.Count;
		});

		Assert.IsTrue(batchCount >= 2, $"Esperado pelo menos 2 lotes para 450 itens, mas recebeu {batchCount}");
		Assert.AreEqual(450, totalItemsReceived);
	}

	[TestMethod]
	public void Execute_Cancellation_StopsImmediately()
	{
		var volume = new VolumeIndex(Guid.NewGuid(), "C:\\", initialCapacity: 1000);
		for (int i = 0; i < 1000; i++)
		{
			volume.Add((ulong)i, -1, $"arquivo_{i}.txt", 50, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		}

		var engine = new QueryEngine();
		var matcher = new QueryMatcher("arquivo");
		using var cts = new CancellationTokenSource();

		int batches = 0;
		var result = engine.Execute(volume, matcher, batch =>
		{
			batches++;
			cts.Cancel(); // Cancela após o primeiro lote
		}, cancellationToken: cts.Token);

		Assert.IsTrue(result.IsCancelled);
		Assert.IsTrue(result.TotalMatches < 1000);
	}
}

