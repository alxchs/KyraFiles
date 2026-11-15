// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Controls
{
	public partial class ToolbarRadioButton : RadioButton, IToolbarItemSet
	{
		public ToolbarRadioButton()
		{
			DefaultStyleKey = typeof(ToolbarRadioButton);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
