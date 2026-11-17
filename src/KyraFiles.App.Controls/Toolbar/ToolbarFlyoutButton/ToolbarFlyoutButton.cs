// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Controls
{
	public partial class ToolbarFlyoutButton : DropDownButton, IToolbarItemSet
	{
		public ToolbarFlyoutButton()
		{
			this.DefaultStyleKey = typeof(ToolbarFlyoutButton);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
