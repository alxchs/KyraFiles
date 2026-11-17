// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Services.Search;
using Microsoft.Extensions.Logging;
using System.IO;

namespace KyraFiles.App.Actions
{
	/// <summary>
	/// KyraFiles: F3 opens Agent Ransack on the current folder, as in Explorer; without it, F3 opens the Files search (F013).
	/// </summary>
	[GeneratedRichCommand]
	internal sealed partial class SearchWithAgentRansackAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.SearchWithAgentRansack.GetLocalizedResource();

		public string Description
			=> Strings.SearchWithAgentRansackDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public HotKey HotKey
			=> new(Keys.F3);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Omnibar.Search");

		public bool IsExecutable
			=> context.ShellPage is not null;

		public SearchWithAgentRansackAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var executable = AgentRansackSearchProvider.GetExecutablePath();
			var folder = GetSearchFolder();
			if (executable is null || folder is null)
				return Ioc.Default.GetRequiredService<ICommandManager>().Search.ExecuteAsync();

			try
			{
				var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false, WorkingDirectory = folder };
				startInfo.ArgumentList.Add("-d");
				startInfo.ArgumentList.Add(folder);
				Process.Start(startInfo)?.Dispose();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to open Agent Ransack on {Folder}", folder);
			}

			return Task.CompletedTask;
		}

		private string? GetSearchFolder()
		{
			if (context.PageType is not (ContentPageTypes.Folder or ContentPageTypes.CloudDrive or ContentPageTypes.SearchResults))
				return null;

			// Disk and network folders only; Agent Ransack cannot search shell locations
			var folder = context.Folder?.ItemPath;
			return !string.IsNullOrEmpty(folder) && Path.IsPathRooted(folder) && Directory.Exists(folder) ? folder : null;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.ShellPage))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
