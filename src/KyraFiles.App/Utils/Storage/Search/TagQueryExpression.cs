// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Utils.Storage
{
	public sealed class TagQueryExpression
	{
		public List<List<TagTerm>> OrGroups { get; set; } = new();
	}
}
