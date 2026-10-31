// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenDocumentationAction : IAction
	{
		public string Label
			=> Strings.Documentation.GetLocalizedResource();

		public string Description
			=> "Abre a documentação local ou online do Files";

		public ActionCategory Category
			=> ActionCategory.Open;

		public Task ExecuteAsync(object? parameter = null)
		{
			return DocumentationHelper.OpenDocumentationAsync("getting-started/overview.html");
		}
	}
}

