// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using KyraFiles.App.Search.Index.Models;
using KyraFiles.App.Search.Index.Query;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Engine;

/// <summary>
/// Motor de consulta de alto desempenho para busca em volumes indexados.
/// Atende aos requisitos:
/// - FR-014: Sem itens duplicados.
/// - FR-022: Suporte a escopo por subárvore ou volume inteiro.
/// - FR-023: Resultados progressivos em lotes (200 itens/30ms no 1º lote, depois a cada 100ms) e cancelamento.
/// - NFR-001: Primeiros resultados em &lt; 100 ms para até 5 milhões de itens.
/// </summary>
public sealed class QueryEngine
{
	private const int FirstBatchItemThreshold = 200;
	private const int FirstBatchTimeThresholdMs = 30;
	private const int SubsequentBatchTimeThresholdMs = 100;
	private const int DefaultMaxResults = 100_000;

	/// <summary>
	/// Executa a busca em um volume indexado entregando os resultados progressivamente em lotes.
	/// </summary>
	/// <param name="volume">Volume indexado.</param>
	/// <param name="matcher">Avaliador da consulta compilada.</param>
	/// <param name="batchCallback">Callback chamado quando um lote fica pronto.</param>
	/// <param name="scopeParentIndex">Índice da pasta de escopo (-1 para todo o volume).</param>
	/// <param name="maxResults">Teto máximo de resultados a retornar.</param>
	/// <param name="cancellationToken">Token de cancelamento.</param>
	/// <returns>Resultado consolidado da busca.</returns>
	public QueryExecutionResult Execute(
		VolumeIndex volume,
		QueryMatcher matcher,
		Action<IReadOnlyList<SearchResultItem>> batchCallback,
		int scopeParentIndex = -1,
		int maxResults = DefaultMaxResults,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(volume);
		ArgumentNullException.ThrowIfNull(matcher);
		ArgumentNullException.ThrowIfNull(batchCallback);

		var stopwatch = Stopwatch.StartNew();
		int totalMatches = 0;
		bool isTruncated = false;
		bool isCancelled = false;

		// Pré-calcula conjunto de pastas do escopo se restrito a subárvore
		BitArray? scopeFolders = null;
		if (scopeParentIndex >= 0)
		{
			scopeFolders = BuildScopeFolderSet(volume, scopeParentIndex);
		}

		var currentBatch = new List<SearchResultItem>(FirstBatchItemThreshold);
		bool isFirstBatch = true;
		long lastBatchTimestamp = stopwatch.ElapsedMilliseconds;

		int count = volume.Count;
		for (int i = 0; i < count; i++)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				isCancelled = true;
				break;
			}

			if (volume.IsDeleted(i))
				continue;

			// 1. Filtro de escopo de subárvore
			if (scopeFolders is not null)
			{
				int parent = volume.GetParentIndex(i);
				if (parent != scopeParentIndex && (parent < 0 || !scopeFolders.Get(parent)))
					continue;
			}

			// 2. Avaliação de correspondência sobre bytes UTF-8 (zero alocações)
			var normalizedUtf8 = volume.GetNormalizedNameUtf8(i);
			if (normalizedUtf8.IsEmpty || !matcher.MatchesNormalizedUtf8(normalizedUtf8))
				continue;

			// 3. Monta o SearchResultItem sob demanda para os itens que deram match
			var item = volume.GetItem(i);
			currentBatch.Add(item);
			totalMatches++;

			if (totalMatches >= maxResults)
			{
				isTruncated = true;
				break;
			}

			// 4. Verificação de entrega de lote por limite de itens ou tempo
			long elapsed = stopwatch.ElapsedMilliseconds;
			long timeSinceLastBatch = elapsed - lastBatchTimestamp;

			bool shouldFlush = isFirstBatch
				? (currentBatch.Count >= FirstBatchItemThreshold || timeSinceLastBatch >= FirstBatchTimeThresholdMs)
				: timeSinceLastBatch >= SubsequentBatchTimeThresholdMs;

			if (shouldFlush && currentBatch.Count > 0)
			{
				batchCallback(currentBatch.ToArray());
				currentBatch.Clear();
				isFirstBatch = false;
				lastBatchTimestamp = stopwatch.ElapsedMilliseconds;
			}
		}

		// Envia último lote remanescente
		if (currentBatch.Count > 0 && !isCancelled)
		{
			batchCallback(currentBatch.ToArray());
			currentBatch.Clear();
		}

		stopwatch.Stop();

		return new QueryExecutionResult(
			TotalMatches: totalMatches,
			Elapsed: stopwatch.Elapsed,
			IsTruncated: isTruncated,
			IsCancelled: isCancelled);
	}

	private static BitArray BuildScopeFolderSet(VolumeIndex volume, int rootFolderIndex)
	{
		int count = volume.Count;
		var set = new BitArray(count);
		set.Set(rootFolderIndex, true);

		// Varre pastas identificando descendentes
		for (int i = 0; i < count; i++)
		{
			if (!volume.IsDirectory(i))
				continue;

			int parent = volume.GetParentIndex(i);
			if (parent >= 0 && parent != i && parent < count && set.Get(parent))
			{
				set.Set(i, true);
			}
		}

		return set;
	}
}

/// <summary>
/// Informações de diagnóstico e término da execução da consulta.
/// </summary>
public readonly record struct QueryExecutionResult(
	int TotalMatches,
	TimeSpan Elapsed,
	bool IsTruncated,
	bool IsCancelled);

