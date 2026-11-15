// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Data.EventArguments
{
	internal sealed class MainPageNavigationArguments
	{
		public object? Parameter { get; set; }

		public bool IgnoreStartupSettings { get; set; }
	}
}
