// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.App.Services.FileIntegrity;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class UnblockDownloadedFilesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IZoneIdentifierService zoneIdentifierService;

		public string Label
			=> Strings.UnblockDownloadedFile.GetLocalizedResource();

		public string Description
			=> "Desbloqueia os arquivos baixados da web removendo a marca de bloqueio (Zone.Identifier)";

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Properties");

		public bool IsExecutable =>
			context.HasSelection &&
			context.SelectedItems.Any(item => item.PrimaryItemAttribute != StorageItemTypes.Folder);

		public UnblockDownloadedFilesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			zoneIdentifierService = Ioc.Default.GetRequiredService<IZoneIdentifierService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var files = context.SelectedItems
				.Where(item => item.PrimaryItemAttribute != StorageItemTypes.Folder && !string.IsNullOrEmpty(item.ItemPath))
				.Select(item => item.ItemPath!)
				.ToList();

			if (files.Count == 0)
				return;

			var result = await zoneIdentifierService.UnblockFilesAsync(files);

			string title = Strings.UnblockDownloadedFile.GetLocalizedResource();
			string message = result.UnblockedCount switch
			{
				0 => "Nenhum dos arquivos selecionados possuía a marca de bloqueio de internet.",
				1 => "1 arquivo baixado foi desbloqueado com sucesso.",
				_ => $"{result.UnblockedCount} arquivos baixados foram desbloqueados com sucesso."
			};

			if (result.FailedCount > 0)
			{
				message += $"\n({result.FailedCount} arquivo(s) não puderam ser alterados).";
			}

			await DialogDisplayHelper.ShowDialogAsync(title, message);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
