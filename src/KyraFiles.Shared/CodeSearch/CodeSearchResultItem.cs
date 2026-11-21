// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.Shared.CodeSearch
{
	/// <summary>
	/// Represents an individual code search hit with file, line, column, and snippet (AC-1, FR-001).
	/// </summary>
	public sealed class CodeSearchResultItem
	{
		public string FilePath { get; }

		public string RelativePath { get; }

		public int LineNumber { get; }

		public int ColumnNumber { get; }

		public string LineText { get; }

		public int MatchLength { get; }

		public CodeSearchResultItem(
			string filePath,
			string relativePath,
			int lineNumber,
			int columnNumber,
			string lineText,
			int matchLength)
		{
			FilePath = filePath;
			RelativePath = relativePath;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
			LineText = lineText;
			MatchLength = matchLength;
		}

		public override string ToString()
			=> $"{RelativePath}:{LineNumber}:{ColumnNumber}: {LineText.Trim()}";
	}
}
