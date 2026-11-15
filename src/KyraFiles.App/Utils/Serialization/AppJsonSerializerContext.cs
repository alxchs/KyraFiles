// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Utils.Serialization
{
	[JsonSerializable(typeof(PaneNavigationArguments))]
	[JsonSerializable(typeof(TaggedFile[]))]
	[JsonSerializable(typeof(List<TaggedFile>))]
	[JsonSerializable(typeof(LayoutPreferencesDatabaseItem[]))]
	[JsonSerializable(typeof(List<LayoutPreferencesDatabaseItem>))]
	[JsonSerializable(typeof(LayoutPreferencesItem))]
	[JsonSerializable(typeof(List<ViewModels.Properties.FileProperty>))]
	[JsonSerializable(typeof(List<KyraFiles.App.Data.Models.DynamicColumnDefinition>))]
	[JsonSerializable(typeof(KyraFiles.App.Data.Models.DynamicColumnDefinition))]
	internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
	{
	}
}
