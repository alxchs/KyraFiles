// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
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
