// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Data.Items
{
	/// <summary>
	/// Represents base item for widget card item.
	/// </summary>
	public abstract class WidgetCardItem : ObservableObject
	{
		public virtual string? Path { get; set; }

		public virtual object? Item { get; set; }
	}
}
