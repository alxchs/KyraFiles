// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Contexts
{
	interface ITagsContext : INotifyPropertyChanged
	{
		IEnumerable<(string path, bool isFolder)> TaggedItems { get; }
	}
}
