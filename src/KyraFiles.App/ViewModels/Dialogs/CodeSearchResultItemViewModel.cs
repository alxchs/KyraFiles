// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.Shared.CodeSearch;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed class CodeSearchResultItemViewModel : ObservableObject
	{
		private readonly CodeSearchResultItem _item;

		public string FilePath => _item.FilePath;

		public string RelativePath => _item.RelativePath;

		public int LineNumber => _item.LineNumber;

		public int ColumnNumber => _item.ColumnNumber;

		public string LineText => _item.LineText.Trim();

		public string FullLineText => _item.LineText;

		public int MatchLength => _item.MatchLength;

		public string DisplayLocation => $"{_item.RelativePath}:{_item.LineNumber}:{_item.ColumnNumber}";

		public CodeSearchResultItemViewModel(CodeSearchResultItem item)
		{
			_item = item;
		}
	}
}
