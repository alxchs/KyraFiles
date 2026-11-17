// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Utils.Storage
{
	public class TagTerm
	{
		public HashSet<string> TagUids { get; set; } = new();

		public bool IsExclude { get; set; }
	}
}
