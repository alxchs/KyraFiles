// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Helpers;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenDocumentationAction : IAction
	{
		public string Label
			=> Strings.Documentation.GetLocalizedResource();

		public string Description
			=> Strings.DocumentationDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public Task ExecuteAsync(object? parameter = null)
		{
			return DocumentationHelper.OpenDocumentationAsync("getting-started/overview.html");
		}
	}
}

