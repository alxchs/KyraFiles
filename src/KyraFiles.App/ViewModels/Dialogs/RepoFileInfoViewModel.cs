// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.Shared.RepoMap;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class RepoFileInfoViewModel : ObservableObject
	{
		public RepoFileInfo Model { get; }

		public string FilePath => Model.FilePath;
		public string RelativePath => Model.RelativePath;
		public string Language => Model.Language;
		public long FileSizeBytes => Model.FileSizeBytes;
		public long LineCount => Model.LineCount;
		public int CommitCount12Months => Model.CommitCount12Months;
		public double HotSpotScore => Model.HotSpotScore;
		public string FormattedSize => Model.FormattedSize;

		public string FormattedLineCount => $"{LineCount:N0} linhas";
		public string FormattedCommitCount => $"{CommitCount12Months} mudanças (12m)";
		public string FormattedHotSpotScore => $"Score: {HotSpotScore:N0}";

		public RepoFileInfoViewModel(RepoFileInfo model)
		{
			Model = model;
		}
	}
}
