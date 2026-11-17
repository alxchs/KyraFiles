// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.EventArguments
{
	public sealed class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabBarItemContent? CurrentInstance { get; set; }

		public List<ITabBarItemContent?>? PageInstances { get; set; }
	}
}
