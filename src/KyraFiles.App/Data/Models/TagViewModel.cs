// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Models
{
	[Serializable]
	public sealed partial class TagViewModel : ObservableObject
	{
		[JsonPropertyName("TagName")]
		public string Name { get; set; }

		[JsonPropertyName("ColorString")]
		public string Color { get; set; }

		[JsonPropertyName("Uid")]
		public string Uid { get; set; }

		[JsonConstructor]
		public TagViewModel(string name, string color, string uid)
		{
			Name = name;
			Color = color;
			Uid = uid;
		}
	}
}
