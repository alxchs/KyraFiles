// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Controls
{
	public record class OmnibarQuerySubmittedEventArgs(OmnibarMode Mode, object? Item, string Text);

	public record class OmnibarSuggestionChosenEventArgs(OmnibarMode Mode, object SelectedItem);

	public record class OmnibarTextChangedEventArgs(OmnibarMode Mode, OmnibarTextChangeReason Reason);

	public record class OmnibarModeChangedEventArgs(OmnibarMode? OldMode, OmnibarMode NewMode);

	public record class OmnibarIsFocusedChangedEventArgs(bool IsFocused);
}
