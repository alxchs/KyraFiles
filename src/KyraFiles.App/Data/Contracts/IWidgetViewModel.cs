// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Data.Contracts
{
	public interface IWidgetViewModel : IDisposable
	{
		string WidgetName { get; }

		string WidgetHeader { get; }

		string AutomationProperties { get; }

		bool IsWidgetSettingEnabled { get; }

		bool ShowMenuFlyout { get; }

		MenuFlyoutItem? MenuFlyoutItem { get; }

		Task RefreshWidgetAsync();
	}
}
