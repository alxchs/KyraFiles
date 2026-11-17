// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Utils.Git;
using KyraFiles.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class CopyPathAsAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CopyPathAs.GetLocalizedResource();

		public string Description
			=> Strings.CopyPathAsDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.CopyAsPath");

		public bool IsExecutable
			=> context.HasSelection;

		public CopyPathAsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public static PathFormat ParseFormat(object? parameter)
		{
			if (parameter is PathFormat pf)
				return pf;

			if (parameter is string s && Enum.TryParse<PathFormat>(s, true, out var parsed))
				return parsed;

			return PathFormat.Windows;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is { SlimContentPage: { } contentPage } shellPage)
			{
				var items = contentPage.SelectedItems;
				var workingDir = shellPage.GetRequiredShellViewModel().WorkingDirectory;
				var paths = items is not null && items.Count > 0
					? items.Where(x => x.ItemPath is not null).Select(x => x.ItemPath!).ToList()
					: (workingDir is not null ? new List<string> { workingDir } : new List<string>());

				var currentFolder = workingDir;
				var repoRoot = currentFolder is not null
					? GitHelpers.GetGitRepositoryPath(currentFolder, System.IO.Path.GetPathRoot(currentFolder))
					: null;
				var format = ParseFormat(parameter);

				try
				{
					var formattedText = PathFormatter.FormatAll(paths, format, repoRoot, currentFolder);
					if (FtpHelpers.IsFtpPath(formattedText))
						formattedText = formattedText.Replace('\\', '/');

					DataPackage data = new();
					data.SetText(formattedText);

					Clipboard.SetContent(data);
					Clipboard.Flush();
				}
				catch (Exception ex)
				{
					App.Logger?.LogWarning(ex, "Failed to copy formatted path(s) to clipboard.");
				}
			}

			return Task.CompletedTask;
		}
	}
}
