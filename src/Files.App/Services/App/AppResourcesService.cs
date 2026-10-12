// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Files.App.Services
{
	/// <inheritdoc cref="IResourcesService"/>
	public sealed class ResourcesService : IResourcesService
	{
		private IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public ResourcesService()
		{
			SetScrollInertiaEnabled(UserSettingsService.GeneralSettingsService.EnableSmoothScrolling);
			SetAppThemeDensity(UserSettingsService.AppearanceSettingsService.AppDensity);
			SetAppControlShapeStyle(UserSettingsService.AppearanceSettingsService.ControlShapeStyle);

			UserSettingsService.GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
			UserSettingsService.AppearanceSettingsService.PropertyChanged += AppearanceSettingsService_PropertyChanged;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IGeneralSettingsService.EnableSmoothScrolling))
			{
				SetScrollInertiaEnabled(UserSettingsService.GeneralSettingsService.EnableSmoothScrolling);
				ApplyResources();
			}
		}

		private void AppearanceSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IAppearanceSettingsService.AppDensity))
			{
				SetAppThemeDensity(UserSettingsService.AppearanceSettingsService.AppDensity);
				ApplyResources();
			}
			else if (e.PropertyName == nameof(IAppearanceSettingsService.ControlShapeStyle))
			{
				SetAppControlShapeStyle(UserSettingsService.AppearanceSettingsService.ControlShapeStyle);
				ApplyResources();
			}
		}

		/// <inheritdoc/>
		public void SetAppThemeBackgroundColor(Color appThemeBackgroundColor)
		{
			Application.Current.Resources["App.Theme.BackgroundBrush"] = appThemeBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.AddressBar.BackgroundBrush"] = appThemeAddressBarBackgroundColor;

			// Overrides the selected tab background to match the address bar
			Application.Current.Resources["TabViewItemHeaderBackgroundSelected"] = appThemeAddressBarBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeToolbarBackgroundColor(Color appThemeToolbarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Toolbar.BackgroundBrush"] = appThemeToolbarBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor)
		{
			Application.Current.Resources["App.Theme.Sidebar.BackgroundBrush"] = appThemeSidebarBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.BackgroundBrush"] = appThemeFileAreaBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeFileAreaSecondaryBackgroundColor(Color appThemeFileAreaSecondaryBackgroundColor)
		{
			Application.Current.Resources["App.Theme.FileArea.SecondaryBackgroundBrush"] = appThemeFileAreaSecondaryBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeInfoPaneBackgroundColor(Color appThemeInfoPaneBackgroundColor)
		{
			Application.Current.Resources["App.Theme.InfoPane.BackgroundBrush"] = appThemeInfoPaneBackgroundColor;
		}

		/// <inheritdoc/>
		public void SetAppThemeFontFamily(string contentControlThemeFontFamily)
		{
			Application.Current.Resources["ContentControlThemeFontFamily"] = contentControlThemeFontFamily;
		}

		/// <inheritdoc/>
		public void SetAppThemeFileAreaFontFamily(string appThemeFileAreaFontFamily)
		{
			Application.Current.Resources["App.Theme.FileArea.FontFamily"] = appThemeFileAreaFontFamily;
		}

		/// <inheritdoc/>
		public void SetScrollInertiaEnabled(bool enableScrollInertia)
		{
			Application.Current.Resources["App.ScrollInertiaEnabled"] = enableScrollInertia;
		}

		/// <inheritdoc/>
		public void SetAppThemeDensity(AppDensityKind density)
		{
			double itemHeight = density switch
			{
				AppDensityKind.UltraCompact => 30.0,
				AppDensityKind.Compact => 32.0,
				_ => 40.0,
			};
			Application.Current.Resources["App.Theme.Sidebar.ItemHeight"] = itemHeight;
		}

		/// <inheritdoc/>
		public void SetAppControlShapeStyle(ControlShapeStyleKind shapeStyle)
		{
			// ControlCornerRadius (buttons, textboxes, previews, items)
			// OverlayCornerRadius (dialogs, flyouts, status center, toolbars)
			// App.Theme.Tab.CornerRadius (tab container)
			(CornerRadius controlRadius, CornerRadius overlayRadius, CornerRadius tabRadius) = shapeStyle switch
			{
				// Opção 1 do desenho: Côncavo à esquerda (0px), ondas/lóbulos à direita (14px superior direito, 4px inferior direito)
				ControlShapeStyleKind.TicketWave => (new CornerRadius(0, 10, 4, 0), new CornerRadius(0, 14, 6, 0), new CornerRadius(0, 14, 0, 4)),

				// Opção 2 do desenho: Encaixe fita / macho-fêmea
				ControlShapeStyleKind.RibbonDock => (new CornerRadius(0, 12, 12, 0), new CornerRadius(0, 16, 16, 0), new CornerRadius(0, 14, 4, 0)),

				// Cyberpunk: Chanfro agressivo
				ControlShapeStyleKind.CyberpunkTrapezoid => (new CornerRadius(0, 12, 0, 6), new CornerRadius(0, 16, 0, 8), new CornerRadius(0, 16, 0, 4)),

				// Pílula Direcional: Esquerda 100% arredondada, direita reta
				ControlShapeStyleKind.DirectionalPill => (new CornerRadius(12, 2, 2, 12), new CornerRadius(16, 4, 4, 16), new CornerRadius(14, 4, 0, 0)),

				// Hexagonal Tech: Chanfros moderados simétricos
				ControlShapeStyleKind.HexagonalTech => (new CornerRadius(2, 12, 2, 12), new CornerRadius(4, 16, 4, 16), new CornerRadius(4, 14, 0, 0)),

				// Onda Sinusoidal: Curva orgânica contínua
				ControlShapeStyleKind.SinusoidalWave => (new CornerRadius(0, 14, 4, 10), new CornerRadius(0, 18, 6, 12), new CornerRadius(0, 16, 4, 6)),

				// Origami Fold: Dobradura assimétrica
				ControlShapeStyleKind.OrigamiFold => (new CornerRadius(14, 0, 14, 0), new CornerRadius(18, 0, 18, 0), new CornerRadius(16, 0, 4, 0)),

				// Cartão de Embarque: Canto compacto
				ControlShapeStyleKind.BoardingPass => (new CornerRadius(4, 4, 4, 4), new CornerRadius(6, 6, 6, 6), new CornerRadius(6, 6, 0, 0)),

				// Folha Orgânica: Diagonal suave
				ControlShapeStyleKind.OrganicLeaf => (new CornerRadius(14, 0, 14, 0), new CornerRadius(18, 2, 18, 2), new CornerRadius(14, 2, 2, 0)),

				// Padrão Fluent (original do Windows 11)
				_ => (new CornerRadius(4), new CornerRadius(8), new CornerRadius(8, 8, 0, 0))
			};

			Application.Current.Resources["ControlCornerRadius"] = controlRadius;
			Application.Current.Resources["OverlayCornerRadius"] = overlayRadius;
			Application.Current.Resources["App.Theme.Tab.CornerRadius"] = tabRadius;
			Application.Current.Resources["App.Theme.ShapeStyle"] = shapeStyle;
		}

		/// <inheritdoc/>
		public void ApplyResources()
		{
			AppThemeModeService.ApplyResources();
		}
	}
}
