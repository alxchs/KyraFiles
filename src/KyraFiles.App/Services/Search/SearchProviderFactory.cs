// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Data.Enums;

namespace KyraFiles.App.Services.Search
{
	/// <summary>
	/// Default implementation of ISearchProviderFactory.
	/// </summary>
	public sealed class SearchProviderFactory : ISearchProviderFactory
	{
		private readonly Dictionary<string, ISearchProvider> _providers;
		private readonly ISearchProvider _defaultProvider;
		private readonly IUserSettingsService? _userSettingsService;

		public SearchProviderFactory(IEnumerable<ISearchProvider> providers, IUserSettingsService? userSettingsService = null)
		{
			_providers = providers.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
			_defaultProvider = _providers.TryGetValue("native", out var native)
				? native
				: new NativeFilesSearchProvider();
			_userSettingsService = userSettingsService;
		}

		public ISearchProvider GetCurrentProvider()
		{
			if (_userSettingsService?.FoldersSettingsService is { } foldersSettings)
			{
				if (foldersSettings.SearchEnginePreference == SearchEngineKind.AgentRansack &&
				    _providers.TryGetValue("agentransack", out var arProvider) && arProvider.IsAvailable)
				{
					return arProvider;
				}

				if (foldersSettings.SearchEnginePreference == SearchEngineKind.Native)
				{
					return _defaultProvider;
				}

				if (foldersSettings.SearchEnginePreference == SearchEngineKind.Everything &&
				    _providers.TryGetValue("everything", out var everythingProvider) && everythingProvider.IsAvailable)
				{
					return everythingProvider;
				}
			}

			// Padrão primário: motor nativo ultrarrápido autônomo
			return _defaultProvider;
		}

		public ISearchProvider? GetProvider(string id)
		{
			return _providers.GetValueOrDefault(id);
		}

		public IReadOnlyList<ISearchProvider> GetAllProviders()
		{
			return _providers.Values.ToList().AsReadOnly();
		}
	}
}
