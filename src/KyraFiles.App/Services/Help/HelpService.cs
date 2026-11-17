// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Data.Help;

namespace KyraFiles.App.Services.Help
{
	/// <summary>
	/// Default implementation of <see cref="IHelpService"/>.
	/// </summary>
	public sealed class HelpService : IHelpService
	{
		public event EventHandler<HelpRequestedEventArgs>? HelpRequested;

		public IReadOnlyList<HelpTopic> GetAllTopics()
		{
			return HelpCatalog.AllTopics;
		}

		public HelpTopic GetTopic(HelpContextId contextId)
		{
			return HelpCatalog.GetTopic(contextId);
		}

		public IReadOnlyList<HelpTopic> SearchTopics(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				return GetAllTopics();

			var clean = query.Trim();
			var terms = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			return HelpCatalog.AllTopics
				.Where(topic => terms.All(term =>
					topic.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
					topic.Summary.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
					topic.BadgeLabel.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
					topic.Keywords.Any(k => k.Contains(term, StringComparison.CurrentCultureIgnoreCase)) ||
					topic.Shortcuts.Any(s => s.Keys.Contains(term, StringComparison.CurrentCultureIgnoreCase) || s.Description.Contains(term, StringComparison.CurrentCultureIgnoreCase))))
				.ToList();
		}

		public void OpenHelp(HelpContextId contextId = HelpContextId.General)
		{
			HelpRequested?.Invoke(this, new HelpRequestedEventArgs(contextId));
		}
	}
}

