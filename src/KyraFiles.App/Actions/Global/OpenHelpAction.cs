// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Data.Contexts;
using KyraFiles.App.Data.Help;
using KyraFiles.App.Services.Help;
using KyraFiles.App.Views.Layouts;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class OpenHelpAction : IAction
	{
		private readonly IHelpService _helpService;
		private readonly IContentPageContext _contentPageContext;

		public string Label
			=> Strings.Help.GetLocalizedResource();

		public string Description
			=> Strings.OpenHelpDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public HotKey HotKey
			=> new(Keys.F1);

		public OpenHelpAction()
		{
			_helpService = Ioc.Default.GetRequiredService<IHelpService>();
			_contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var contextId = ResolveCurrentContext();
			_helpService.OpenHelp(contextId);
			return Task.CompletedTask;
		}

		private HelpContextId ResolveCurrentContext()
		{
			// Check active page layout type
			var layoutType = _contentPageContext.PageLayoutType;
			if (layoutType == typeof(ColumnLayoutPage) || layoutType == typeof(ColumnsLayoutPage))
				return HelpContextId.ColumnsLayout;

			if (layoutType == typeof(DetailsLayoutPage))
				return HelpContextId.DetailsLayout;

			if (layoutType == typeof(GridLayoutPage))
				return HelpContextId.GridLayout;

			// Check if multi-pane is active
			if (_contentPageContext.IsMultiPaneActive)
				return HelpContextId.TabsAndPanes;

			// Check page type
			if (_contentPageContext.PageType == ContentPageTypes.Settings)
				return HelpContextId.SettingsAndCustomization;

			return HelpContextId.General;
		}
	}
}
