// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Files.App.Data.Models
{
	public enum SearchFilterKind
	{
		Extension,
		DateModified,
		DateCreated,
		Size
	}

	/// <summary>
	/// Represents an editable filter chip in natural search results (FR-M5, M5-AC-1, M5-AC-2).
	/// </summary>
	public sealed partial class SearchFilterChip : ObservableObject
	{
		public SearchFilterKind Kind { get; }

		private string displayText = string.Empty;
		public string DisplayText
		{
			get => displayText;
			set => SetProperty(ref displayText, value);
		}

		private string editValue = string.Empty;
		public string EditValue
		{
			get => editValue;
			set => SetProperty(ref editValue, value);
		}

		public IRelayCommand RemoveCommand { get; }

		public IRelayCommand ApplyEditCommand { get; }

		public SearchFilterChip(
			SearchFilterKind kind,
			string displayText,
			string initialEditValue,
			Action<SearchFilterChip> onRemove,
			Action<SearchFilterChip, string>? onEdit = null)
		{
			Kind = kind;
			this.displayText = displayText;
			this.editValue = initialEditValue;
			RemoveCommand = new RelayCommand(() => onRemove(this));
			ApplyEditCommand = new RelayCommand(() =>
			{
				if (onEdit is not null && !string.IsNullOrWhiteSpace(EditValue))
				{
					onEdit(this, EditValue);
				}
			});
		}
	}
}
