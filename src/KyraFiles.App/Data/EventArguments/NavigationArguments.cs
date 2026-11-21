// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Data.EventArguments
{
	public class NavigationArguments
	{
		public bool FocusOnNavigation { get; set; } = false;

		public string? NavPathParam
		{
			get;
			set => field = value is null ? null : ShellHelpers.ResolveShellPath(value);
		}

		public IShellPage? AssociatedTabInstance { get; set; }

		public bool IsSearchResultPage { get; set; } = false;

		public string? SearchPathParam
		{
			get;
			set => field = value is null ? null : ShellHelpers.ResolveShellPath(value);
		}

		public string? SearchQuery { get; set; } = null;

		public KyraFiles.Shared.Helpers.SearchIntent? NaturalSearchIntent { get; set; }

		/// <summary>Search provider to use instead of the current one (repeating a history entry, F094 FR-013).</summary>
		public string? SearchProviderId { get; set; }

		public bool IsLayoutSwitch { get; set; } = false;

		public IEnumerable<string>? SelectItems { get; set; }
	}
}
