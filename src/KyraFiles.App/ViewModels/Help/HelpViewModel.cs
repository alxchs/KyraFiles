// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using Files.App.Data.Help;
using Files.App.Services.Help;

namespace Files.App.ViewModels.Help
{
	/// <summary>
	/// ViewModel powering the HelpPane and help presentation UI.
	/// </summary>
	public sealed partial class HelpViewModel : ObservableObject
	{
		private readonly IHelpService _helpService;

		public ObservableCollection<HelpTopic> FilteredTopics { get; } = [];

		[ObservableProperty]
		public partial HelpTopic? SelectedTopic { get; set; }

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(HasNoResults))]
		public partial string SearchText { get; set; } = string.Empty;

		[ObservableProperty]
		public partial bool IsHelpOpen { get; set; }

		public bool HasNoResults => !string.IsNullOrWhiteSpace(SearchText) && FilteredTopics.Count == 0;

		public HelpViewModel(IHelpService helpService)
		{
			_helpService = helpService;
			_helpService.HelpRequested += HelpService_HelpRequested;

			LoadTopics();
		}

		private void HelpService_HelpRequested(object? sender, HelpRequestedEventArgs e)
		{
			NavigateToContext(e.ContextId);
			IsHelpOpen = true;
		}

		public void LoadTopics()
		{
			FilteredTopics.Clear();
			foreach (var topic in _helpService.GetAllTopics())
				FilteredTopics.Add(topic);

			SelectedTopic ??= FilteredTopics.FirstOrDefault();
		}

		partial void OnSearchTextChanged(string value)
		{
			var results = _helpService.SearchTopics(value);
			FilteredTopics.Clear();
			foreach (var topic in results)
				FilteredTopics.Add(topic);

			if (SelectedTopic is null || !FilteredTopics.Contains(SelectedTopic))
				SelectedTopic = FilteredTopics.FirstOrDefault();
		}

		public void NavigateToContext(HelpContextId contextId)
		{
			// Reset search to ensure all topics are accessible
			if (!string.IsNullOrEmpty(SearchText))
				SearchText = string.Empty;

			var topic = _helpService.GetTopic(contextId);
			SelectedTopic = topic;
		}

		public void SelectTopic(HelpTopic topic)
		{
			SelectedTopic = topic;
		}

		public void CloseHelp()
		{
			IsHelpOpen = false;
		}
	}
}

