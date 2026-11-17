// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Extensions
{
	public static class GroupOptionExtensions
	{
		public static bool IsGroupByDate(this GroupOption groupOption)
			=> groupOption is GroupOption.DateModified or GroupOption.DateCreated or GroupOption.DateDeleted;
	}
}
