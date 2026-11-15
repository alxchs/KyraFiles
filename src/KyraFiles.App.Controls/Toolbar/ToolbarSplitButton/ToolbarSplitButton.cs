// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Controls
{
	public partial class ToolbarSplitButton : SplitButton, IToolbarItemSet
	{
		public ToolbarSplitButton()
		{
			DefaultStyleKey = typeof(ToolbarSplitButton);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
