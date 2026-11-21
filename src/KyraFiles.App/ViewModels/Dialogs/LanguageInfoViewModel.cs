// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.Shared.RepoMap;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class LanguageInfoViewModel : ObservableObject
	{
		public LanguageInfo Model { get; }

		public string LanguageName => Model.LanguageName;
		public int FileCount => Model.FileCount;
		public long TotalLines => Model.TotalLines;
		public long TotalBytes => Model.TotalBytes;
		public double PercentLines => Model.PercentLines;
		public double PercentBytes => Model.PercentBytes;
		public string FormattedTotalBytes => Model.FormattedTotalBytes;

		public string FormattedSummary => $"{FileCount:N0} arquivos | {TotalLines:N0} linhas ({PercentLines:F1}%) | {FormattedTotalBytes}";

		public IReadOnlyList<RepoFileInfo> Files => Model.Files;

		public LanguageInfoViewModel(LanguageInfo model)
		{
			Model = model;
		}
	}
}
