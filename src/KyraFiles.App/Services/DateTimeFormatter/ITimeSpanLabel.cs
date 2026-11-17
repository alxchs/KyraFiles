// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Services.DateTimeFormatter
{
	public interface ITimeSpanLabel
	{
		string Text { get; }

		string Glyph { get; }

		int Index { get; }
	}
}
