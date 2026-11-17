// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CopyToFolderAction : BaseCopyMoveToFolderAction
	{
		protected override bool IsMove
			=> false;

		protected override string ConfirmationFormat
			=> Strings.CopyToFolderConfirmation;

		public override string Label
			=> Strings.CopyToFolder.GetLocalizedResource();

		public override string Description
			=> Strings.CopyToFolderDescription.GetLocalizedResource();

		public CopyToFolderAction()
		{
		}
	}
}
