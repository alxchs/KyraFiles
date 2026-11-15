// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Data.Contracts
{
	public interface IPreviewPopupService : INotifyPropertyChanged
	{
		/// <summary>
		/// Get Preview Popup provider
		/// </summary>
		/// <returns></returns>
		Task<IPreviewPopupProvider?> GetProviderAsync();
	}
}
