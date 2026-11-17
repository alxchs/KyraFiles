// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;

namespace KyraFiles.App.Views.Properties
{
	public sealed partial class CustomizationPage : BasePropertiesPage
	{
		private CustomizationViewModel CustomizationViewModel { get; set; } = null!;

		public CustomizationPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;

			base.OnNavigatedTo(e);

			CustomizationViewModel = new(
				AppInstance,
				BaseProperties ?? throw new InvalidOperationException("The properties model has not been initialized."),
				(parameter.Window ?? throw new InvalidOperationException("The properties window has not been initialized.")).AppWindow);
		}

		public override async Task<bool> SaveChangesAsync()
			=> await CustomizationViewModel.UpdateIcon();

		public override void Dispose()
		{
		}
	}
}
