// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.DependencyInjection;
using KyraFiles.App.Data.Contracts;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace KyraFiles.App.Helpers
{
	public static class DragZoneHelper
	{
		public delegate int SetTitleBarDragRegionDelegate(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect);

		/// <summary>
		/// Informs the bearer to refresh the drag region.
		/// will not set<see cref="NonClientRegionKind.LeftBorder"/>, <see cref="NonClientRegionKind.RightBorder"/>, <see cref="NonClientRegionKind.Caption"/>when titleBarHeight less than 0
		/// </summary>
		/// <param name="element"></param>
		/// <param name="window"></param>
		/// <param name="setTitleBarDragRegion"></param>
		public static void RaiseSetTitleBarDragRegion(this Window window, SetTitleBarDragRegionDelegate setTitleBarDragRegion)
		{
			if (!window.AppWindow.IsVisible)
				return;
			// UIElement.RasterizationScale is always 1
			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			if (window.Content is not UIElement { XamlRoot: { } xamlRoot } uiElement)
				return;

			var scaleFactor = xamlRoot.RasterizationScale;
			var size = window.AppWindow.Size;
			var uiScale = Ioc.Default.GetService<IAppearanceSettingsService>()?.AppUiScalePercent ?? UiScaleMath.DefaultScalePercent;
			var uiScaleMultiplier = UiScaleMath.GetScaleFactor(uiScale);

			// If the number of regions is 0 or 1, AppWindow will automatically reset to the default region next time, but if it is >=2, it will not and need to be manually cleared
			source.ClearRegionRects(NonClientRegionKind.Passthrough);
			var titleBarHeight = setTitleBarDragRegion(source, size, scaleFactor, GetScaledRect);
			if (titleBarHeight >= 0)
			{
				// region under the buttons
				const int borderThickness = 5;
				var regions = UiScaleMath.ComputeTitleBarRegions(size.Width, titleBarHeight, borderThickness, scaleFactor, uiScaleMultiplier);
				source.SetRegionRects(NonClientRegionKind.LeftBorder, [new(0, 0, regions.BorderWidth, regions.PhysicalTitleBarHeight)]);
				source.SetRegionRects(NonClientRegionKind.RightBorder, [new(regions.RightBorderX, 0, regions.BorderWidth, regions.PhysicalTitleBarHeight)]);
				source.SetRegionRects(NonClientRegionKind.Caption, [new(0, 0, regions.CaptionWidth, regions.PhysicalTitleBarHeight)]);
			}
		}

		private static RectInt32 GetScaledRect(this UIElement uiElement, RectInt32? r = null)
		{
			var uiScale = Ioc.Default.GetService<IAppearanceSettingsService>()?.AppUiScalePercent ?? UiScaleMath.DefaultScalePercent;
			var uiScaleMultiplier = UiScaleMath.GetScaleFactor(uiScale);
			var scaleFactor = uiElement.XamlRoot.RasterizationScale;

			if (r is { } rect)
			{
				var scaled = UiScaleMath.ScaleLocalRectToPhysical(rect.X, rect.Y, rect.Width, rect.Height, scaleFactor, uiScaleMultiplier);
				return new(scaled.X, scaled.Y, scaled.Width, scaled.Height);
			}
			else
			{
				var pos = uiElement.TransformToVisual(null).TransformPoint(new(0, 0));
				var scaled = UiScaleMath.ScaleRootElementRectToPhysical(pos.X, pos.Y, uiElement.ActualSize.X, uiElement.ActualSize.Y, scaleFactor, uiScaleMultiplier);
				return new(scaled.X, scaled.Y, scaled.Width, scaled.Height);
			}
		}
	}
}
