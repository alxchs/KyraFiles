// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Contracts
{
	/// <summary>
	/// Represents capabilities supported by a search provider.
	/// </summary>
	public sealed class SearchCapabilities
	{
		public bool SupportsContentSearch { get; init; }
		public bool SupportsRegex { get; init; }
		public bool SupportsWildcards { get; init; } = true;
		public bool SupportsDeepVolumes { get; init; }
		public bool IsInstantStreaming { get; init; } = true;
	}

	/// <summary>
	/// Encapsulates parameters for a search operation.
	/// </summary>
	public sealed class SearchRequest
	{
		public string Query { get; init; } = string.Empty;
		public string Folder { get; init; } = string.Empty;
		public uint MaxItemCount { get; init; }
		public bool SearchSubfolders { get; init; } = true;
		public bool SearchContent { get; init; }

		/// <summary>
		/// Callback invocado a cada lote de resultados recebido do motor de busca (FR-023).
		/// </summary>
		public Action<IReadOnlyList<ListedItem>>? OnBatchReceived { get; init; }

		/// <summary>
		/// Callback invocado quando a busca cai no FolderSearch de reserva, informando o motivo para exibição na tela (FR-041).
		/// </summary>
		public Action<string>? OnFallbackNotice { get; init; }

		/// <summary>
		/// Callback invocado quando o resultado atinge o teto de itens (FR-023).
		/// </summary>
		public Action<int>? OnTruncated { get; init; }
	}

	/// <summary>
	/// Defines the contract for pluggable file search providers.
	/// </summary>
	public interface ISearchProvider
	{
		/// <summary>
		/// Gets the unique identifier of the provider.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets the display name of the provider.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets a value indicating whether the provider is installed and ready to execute queries.
		/// </summary>
		bool IsAvailable { get; }

		/// <summary>
		/// Gets the capabilities supported by this provider.
		/// </summary>
		SearchCapabilities Capabilities { get; }

		/// <summary>
		/// Executes a search asynchronously, adding matched items to the result collection.
		/// </summary>
		Task SearchAsync(SearchRequest request, IList<ListedItem> results, CancellationToken cancellationToken);
	}
}