// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.ViewModels
{
	public sealed partial class ReleaseNotesViewModel : ObservableObject
	{
		public string BlogPostUrl =>
			Constants.ExternalUrl.ReleaseNotesUrl;

		public string SupportUsUrl =>
			Constants.ExternalUrl.SupportUsUrl;

		public ReleaseNotesViewModel()
		{
		}
	}
}
