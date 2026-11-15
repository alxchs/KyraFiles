// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.App.Extensions;
using KyraFiles.Shared.BuildFolder;

namespace KyraFiles.App.ViewModels.Dialogs
{
	/// <summary>
	/// Represents an item in the Clean Build Folders dialog list (US1, AC-1).
	/// </summary>
	public sealed partial class BuildFolderItemViewModel : ObservableObject
	{
		public string FolderPath { get; }

		public string FolderName { get; }

		public string ParentFolderPath { get; }

		public string ProjectName { get; }

		public string ProjectType { get; }

		public DateTime LastModified { get; }

		public string LastModifiedFormatted => LastModified.ToString("g");

		[ObservableProperty]
		private long sizeBytes = -1;

		[ObservableProperty]
		private string formattedSize = "...";

		[ObservableProperty]
		private bool isSelected = true;

		[ObservableProperty]
		private string status = string.Empty;

		[ObservableProperty]
		private bool isInUse = false;

		[ObservableProperty]
		private bool isDeleted = false;

		public bool CanToggleSelection => !IsDeleted && !IsInUse;

		public BuildFolderItemViewModel(BuildFolderCandidate candidate)
		{
			FolderPath = candidate.FolderPath;
			FolderName = candidate.FolderName;
			ParentFolderPath = candidate.ParentFolderPath;
			ProjectName = candidate.ProjectName;
			ProjectType = candidate.ProjectType;
			LastModified = candidate.LastModified;
			SizeBytes = candidate.SizeBytes;

			if (candidate.SizeBytes >= 0)
			{
				FormattedSize = candidate.SizeBytes.ToSizeString();
			}
		}

		public void UpdateSize(long bytes)
		{
			SizeBytes = bytes;
			FormattedSize = bytes.ToSizeString();
		}

		partial void OnIsSelectedChanged(bool value)
		{
			OnPropertyChanged(nameof(CanToggleSelection));
		}

		partial void OnIsInUseChanged(bool value)
		{
			OnPropertyChanged(nameof(CanToggleSelection));
		}

		partial void OnIsDeletedChanged(bool value)
		{
			OnPropertyChanged(nameof(CanToggleSelection));
		}
	}
}
