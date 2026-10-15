// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	[WinRT.GeneratedBindableCustomProperty([nameof(Text), nameof(Definition)], [])]
	public sealed partial class DynamicColumnValue : ObservableObject
	{
		public DynamicColumnDefinition Definition { get; set; } = null!;

		private string text = string.Empty;
		public string Text
		{
			get => text;
			set => SetProperty(ref text, value);
		}
	}
}
