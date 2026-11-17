// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.Shared.Helpers;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CreateLinkAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Criar link...";

		public string Description
			=> "Cria um link simbólico, junção ou hardlink para o item selecionado";

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Shortcut");

		public bool IsExecutable =>
			context.HasSelection &&
			context.SelectedItems.Count == 1 &&
			!string.IsNullOrEmpty(context.SelectedItem?.ItemPath);

		public CreateLinkAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var targetPath = context.SelectedItem?.ItemPath;
			if (string.IsNullOrWhiteSpace(targetPath))
				return;

			bool isDirectory = Directory.Exists(targetPath);
			string parentDir = Path.GetDirectoryName(targetPath) ?? string.Empty;
			string name = Path.GetFileName(targetPath);

			if (string.IsNullOrEmpty(parentDir))
				return;

			string linkName = isDirectory ? $"{name} - Junction" : $"{name} - Link";
			string linkPath = Path.Combine(parentDir, linkName);

			// Avoid existing collision
			int counter = 2;
			while (File.Exists(linkPath) || Directory.Exists(linkPath))
			{
				linkName = isDirectory ? $"{name} - Junction ({counter})" : $"{name} - Link ({counter})";
				linkPath = Path.Combine(parentDir, linkName);
				counter++;
			}

			// Tenta criar SymbolicLink primeiro
			var symResult = LinkHelper.CreateSymbolicLink(linkPath, targetPath);
			if (symResult.Success)
			{
				await DialogDisplayHelper.ShowDialogAsync("Link criado", $"Link simbólico criado com sucesso:\n{linkPath}");
				return;
			}

			// AC-5: Se falta permissão para link simbólico, recua e oferece Junction (pasta) ou Hardlink (arquivo)
			if (symResult.PrivilegeDenied)
			{
				if (isDirectory)
				{
					var juncResult = LinkHelper.CreateJunction(linkPath, targetPath);
					if (juncResult.Success)
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"Junção criada (Alternativa)",
							$"A criação de link simbólico exige permissão de administrador ou Modo Desenvolvedor.\n\nUma Junção de diretório foi criada com sucesso:\n{linkPath}");
						return;
					}
				}
				else
				{
					var hardResult = LinkHelper.CreateHardLink(linkPath, targetPath);
					if (hardResult.Success)
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"Hardlink criado (Alternativa)",
							$"A criação de link simbólico exige permissão de administrador ou Modo Desenvolvedor.\n\nUm Hardlink foi criado com sucesso no mesmo volume:\n{linkPath}");
						return;
					}
				}

				await DialogDisplayHelper.ShowDialogAsync(
					"Permissão insuficiente",
					"Não foi possível criar o link simbólico porque o sistema requer privilégios de administrador ou a ativação do 'Modo de Desenvolvedor' nas Configurações do Windows.");
				return;
			}

			await DialogDisplayHelper.ShowDialogAsync("Erro ao criar link", symResult.ErrorMessage ?? "Falha inesperada ao criar link.");
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.SelectedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
