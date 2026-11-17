// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.Shared.Helpers;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class ConvertToCrlfAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Converter fim de linha para CRLF (Windows)";

		public string Description
			=> "Converte os arquivos selecionados para quebras de linha padrão Windows (CRLF)";

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Rename");

		public bool IsExecutable =>
			context.HasSelection &&
			context.SelectedItems.Any(item => item.PrimaryItemAttribute != StorageItemTypes.Folder);

		public ConvertToCrlfAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
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

			var result = await BatchTextConverter.ConvertToCrlfAsync(files);

			string title = "Conversão para CRLF";
			string message = $"{result.ConvertedCount} arquivo(s) convertido(s) para CRLF com sucesso.";

			if (result.SkippedCount > 0 || result.FailedCount > 0)
			{
				message += $"\n({result.SkippedCount + result.FailedCount} pulado(s) ou inacessível(is)).";
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
