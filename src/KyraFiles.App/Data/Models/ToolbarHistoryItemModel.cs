// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Navigation;

namespace KyraFiles.App.Data.Models
{
	internal sealed class ToolbarHistoryItemModel
	{
		public PageStackEntry PageStackEntry { get; }

		public bool IsBackMode { get; }

		public ToolbarHistoryItemModel(PageStackEntry pageStackEntry, bool isBackMode)
		{
			PageStackEntry = pageStackEntry;
			IsBackMode = isBackMode;
		}
	}
}
