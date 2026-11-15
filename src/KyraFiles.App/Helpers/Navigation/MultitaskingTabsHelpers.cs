// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Helpers
{
	public static class MultitaskingTabsHelpers
	{
		public static void CloseTabsToTheLeft(TabBarItem? clickedTab, ITabBar? multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				var currentIndex = ((System.Collections.IList)tabs).IndexOf(clickedTab);

				tabs.Take(currentIndex).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseTabsToTheRight(TabBarItem? clickedTab, ITabBar? multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				var currentIndex = ((System.Collections.IList)tabs).IndexOf(clickedTab);

				tabs.Skip(currentIndex + 1).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseOtherTabs(TabBarItem? clickedTab, ITabBar? multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				tabs.Where((t) => t != clickedTab).ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		public static void CloseAllTabs(ITabBar multitaskingControl)
		{
			if (multitaskingControl is not null)
			{
				var tabs = MainPageViewModel.AppInstances;
				tabs.ToList().ForEach(tab => multitaskingControl.CloseTab(tab));
			}
		}

		/// <summary>
		/// F032: the number of pinned tabs, which always occupy the first positions.
		/// </summary>
		public static int PinnedTabCount
			=> MainPageViewModel.AppInstances.Count(tab => tab.IsPinned);

		/// <summary>
		/// F032: pins or unpins a tab and moves it to the edge of the pinned band: a pinned tab goes right after the other
		/// pinned tabs, an unpinned one right after the pinned tabs that remain. The tab keeps the selection if it had it.
		/// </summary>
		public static void SetTabPinned(TabBarItem tab, bool isPinned)
		{
			var tabs = MainPageViewModel.AppInstances;
			var from = tabs.IndexOf(tab);
			if (from < 0 || tab.IsPinned == isPinned)
				return;

			var wasSelected = App.AppModel.TabStripSelectedIndex == from;
			tab.IsPinned = isPinned;

			var to = tabs.Count(other => other.IsPinned && other != tab);
			if (to != from)
				tabs.Move(from, to);
			if (wasSelected)
				App.AppModel.TabStripSelectedIndex = to;

			AppLifecycleHelper.SaveSessionTabs();
		}

		/// <summary>
		/// F032: stable reorder that puts pinned tabs before the others, keeping the relative order of each group.
		/// </summary>
		public static void KeepPinnedTabsFirst()
		{
			var tabs = MainPageViewModel.AppInstances;
			var selected = tabs.ElementAtOrDefault(App.AppModel.TabStripSelectedIndex);
			var target = 0;
			for (var i = 0; i < tabs.Count; i++)
			{
				if (!tabs[i].IsPinned)
					continue;
				if (i != target)
					tabs.Move(i, target);
				target++;
			}

			if (selected is not null)
				App.AppModel.TabStripSelectedIndex = tabs.IndexOf(selected);
		}

		public static Task MoveTabToNewWindow(TabBarItem tab, ITabBar multitaskingControl)
		{
			int index = MainPageViewModel.AppInstances.IndexOf(tab);
			if (index < 0)
				return Task.CompletedTask;

			TabBarItemParameter? tabItemArguments = MainPageViewModel.AppInstances[index].NavigationParameter;

			// F032: the tab moves to the new window unpinned
			tab.IsPinned = false;
			multitaskingControl?.CloseTab(MainPageViewModel.AppInstances[index]);

			return tabItemArguments is not null
				? NavigationHelpers.OpenTabInNewWindowAsync(tabItemArguments.Serialize())
				: NavigationHelpers.OpenPathInNewWindowAsync("Home");
		}
	}
}
