// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Help;

namespace Files.App.Services.Help
{
	/// <summary>
	/// Event arguments dispatched when a help request is triggered with a requested context.
	/// </summary>
	public sealed class HelpRequestedEventArgs : EventArgs
	{
		public HelpContextId ContextId { get; }

		public HelpRequestedEventArgs(HelpContextId contextId)
		{
			ContextId = contextId;
		}
	}

	/// <summary>
	/// Provides context-sensitive help, topic lookup, and notifications.
	/// </summary>
	public interface IHelpService
	{
		/// <summary>
		/// Gets all available help topics.
		/// </summary>
		IReadOnlyList<HelpTopic> GetAllTopics();

		/// <summary>
		/// Gets the topic corresponding to a specific context ID.
		/// </summary>
		HelpTopic GetTopic(HelpContextId contextId);

		/// <summary>
		/// Searches topics matching a free-text query in title, keywords, or content.
		/// </summary>
		IReadOnlyList<HelpTopic> SearchTopics(string query);

		/// <summary>
		/// Signals the UI to open and present the help pane or view for the given context.
		/// </summary>
		void OpenHelp(HelpContextId contextId = HelpContextId.General);

		/// <summary>
		/// Event raised when an open help request is triggered.
		/// </summary>
		event EventHandler<HelpRequestedEventArgs>? HelpRequested;
	}
}

