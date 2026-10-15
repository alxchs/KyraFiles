// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Help
{
	/// <summary>
	/// Identifies the UI and workflow context for context-sensitive help resolution.
	/// </summary>
	public enum HelpContextId
	{
		/// <summary>
		/// General welcome and overview of Files Custom features.
		/// </summary>
		General,

		/// <summary>
		/// Miller Columns / Column View navigation (Inspired by OneCommander / macOS).
		/// </summary>
		ColumnsLayout,

		/// <summary>
		/// Details View, independent column widths and 300+ shell columns.
		/// </summary>
		DetailsLayout,

		/// <summary>
		/// Grid and icon views.
		/// </summary>
		GridLayout,

		/// <summary>
		/// Smart tab management, tab duplication, and dual-pane view (Horizontal / Vertical).
		/// </summary>
		TabsAndPanes,

		/// <summary>
		/// Batch renaming with syntax patterns and collision protection.
		/// </summary>
		BulkRename,

		/// <summary>
		/// Classic Windows Menu Bar (Alt+F, Alt+E, Alt+V, Alt+T, Alt+H).
		/// </summary>
		ClassicMenuBar,

		/// <summary>
		/// Fast indexing search and external engine integration (Agent Ransack / Everything / F3).
		/// </summary>
		SearchEngine,

		/// <summary>
		/// Transparent Windows Explorer replacement with automatic safe fallback.
		/// </summary>
		ExplorerReplacement,

		/// <summary>
		/// Settings, layout density, fonts, themes, and personal customization.
		/// </summary>
		SettingsAndCustomization,

		/// <summary>
		/// Comprehensive keyboard shortcuts map.
		/// </summary>
		KeyboardShortcuts,
	}

	/// <summary>
	/// Highlights technical and design advantages over default Windows Explorer or inspirations.
	/// </summary>
	public enum HelpBadgeType
	{
		None,

		/// <summary>
		/// Capabilities that exceed standard Windows Explorer.
		/// </summary>
		ExplorerAdvantage,

		/// <summary>
		/// Workflow and layouts inspired by OneCommander or modern productivity file managers.
		/// </summary>
		OneCommanderInspired,

		/// <summary>
		/// Exclusive custom features of Files Custom.
		/// </summary>
		ExclusiveFeature,
	}

	/// <summary>
	/// Represents an accelerator or key combination related to a feature.
	/// </summary>
	public sealed class HelpShortcut
	{
		public string Keys { get; }
		public string Description { get; }

		public HelpShortcut(string keys, string description)
		{
			Keys = keys;
			Description = description;
		}
	}

	/// <summary>
	/// Self-contained educational help topic with human-friendly guidance.
	/// </summary>
	public sealed class HelpTopic
	{
		public HelpContextId ContextId { get; }
		public string Title { get; }
		public string Category { get; }
		public HelpBadgeType BadgeType { get; }
		public string BadgeLabel { get; }
		public string Summary { get; }
		public string Content { get; }
		public IReadOnlyList<HelpShortcut> Shortcuts { get; }
		public IReadOnlyList<string> Keywords { get; }

		public HelpTopic(
			HelpContextId contextId,
			string title,
			string category,
			HelpBadgeType badgeType,
			string badgeLabel,
			string summary,
			string content,
			IReadOnlyList<HelpShortcut>? shortcuts = null,
			IReadOnlyList<string>? keywords = null)
		{
			ContextId = contextId;
			Title = title;
			Category = category;
			BadgeType = badgeType;
			BadgeLabel = badgeLabel;
			Summary = summary;
			Content = content;
			Shortcuts = shortcuts ?? Array.Empty<HelpShortcut>();
			Keywords = keywords ?? Array.Empty<string>();
		}
	}
}

