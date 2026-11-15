// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Data.EventArguments
{
	public sealed class SearchBoxQuerySubmittedEventArgs
	{
		public SuggestionModel ChosenSuggestion { get; }

		public SearchBoxQuerySubmittedEventArgs(SuggestionModel chosenSuggestion)
			=> ChosenSuggestion = chosenSuggestion;
	}
}
