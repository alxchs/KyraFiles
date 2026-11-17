// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Data.Parameters
{
	public sealed class ColumnParam : NavigationArguments
	{
		public int Column { get; set; }

		public ListView? ListView { get; set; }

		public ColumnLayoutPage? Source { get; set; }
	}
}
