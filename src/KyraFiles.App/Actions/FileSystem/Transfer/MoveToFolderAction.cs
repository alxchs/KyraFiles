// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class MoveToFolderAction : BaseCopyMoveToFolderAction
	{
		protected override bool IsMove
			=> true;

		protected override string ConfirmationFormat
			=> Strings.MoveToFolderConfirmation;

		public override string Label
			=> Strings.MoveToFolder.GetLocalizedResource();

		public override string Description
			=> Strings.MoveToFolderDescription.GetLocalizedResource();

		public MoveToFolderAction()
		{
		}
	}
}
