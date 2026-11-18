// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;

namespace KyraFiles.App.Helpers
{
	public struct UiCompensatedLayoutSize
	{
		public double Width { get; set; }
		public double Height { get; set; }
		public bool IsIdentity { get; set; }
		public bool IsValid { get; set; }
	}

	public struct UiScaledPixelRect
	{
		public int X { get; set; }
		public int Y { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
	}

	public struct UiTitleBarRegions
	{
		public int BorderWidth { get; set; }
		public int RightBorderX { get; set; }
		public int CaptionWidth { get; set; }
		public int PhysicalTitleBarHeight { get; set; }
	}

	/// <summary>
	/// Pure layout compensation and non-client region scaling calculations for F090 UI scaling.
	/// </summary>
	public static class UiScaleMath
	{
		public const int DefaultScalePercent = 100;
		public const int MinScalePercent = 80;
		public const int MaxScalePercent = 150;

		public static readonly int[] SupportedScalePercents = new int[] { 80, 90, 100, 110, 125, 150 };

		public static int NormalizeScalePercent(int percent)
		{
			if (percent <= 0)
				return DefaultScalePercent;

			if (percent < MinScalePercent)
				return MinScalePercent;

			if (percent > MaxScalePercent)
				return MaxScalePercent;

			return percent;
		}

		public static double GetScaleFactor(int percent)
		{
			return NormalizeScalePercent(percent) / 100.0;
		}

		public static UiCompensatedLayoutSize ComputeCompensatedSize(double windowWidth, double windowHeight, double scaleFactor)
		{
			if (scaleFactor <= 0 || double.IsNaN(scaleFactor) || double.IsInfinity(scaleFactor))
				return new UiCompensatedLayoutSize { Width = double.NaN, Height = double.NaN, IsIdentity = true, IsValid = false };

			if (Math.Abs(scaleFactor - 1.0) < 0.001)
				return new UiCompensatedLayoutSize { Width = double.NaN, Height = double.NaN, IsIdentity = true, IsValid = true };

			if (windowWidth <= 0 || windowHeight <= 0 || double.IsNaN(windowWidth) || double.IsNaN(windowHeight))
				return new UiCompensatedLayoutSize { Width = double.NaN, Height = double.NaN, IsIdentity = false, IsValid = false };

			return new UiCompensatedLayoutSize
			{
				Width = windowWidth / scaleFactor,
				Height = windowHeight / scaleFactor,
				IsIdentity = false,
				IsValid = true
			};
		}

		public static UiScaledPixelRect ScaleLocalRectToPhysical(int x, int y, int width, int height, double rasterizationScale, double uiScaleMultiplier)
		{
			var totalScale = rasterizationScale * uiScaleMultiplier;
			return new UiScaledPixelRect
			{
				X = (int)Math.Round(x * totalScale),
				Y = (int)Math.Round(y * totalScale),
				Width = Math.Max(0, (int)Math.Ceiling(width * totalScale)),
				Height = Math.Max(0, (int)Math.Ceiling(height * totalScale))
			};
		}

		public static UiScaledPixelRect ScaleRootElementRectToPhysical(double rootX, double rootY, double localWidth, double localHeight, double rasterizationScale, double uiScaleMultiplier)
		{
			var totalScale = rasterizationScale * uiScaleMultiplier;
			return new UiScaledPixelRect
			{
				X = (int)Math.Round(rootX * rasterizationScale),
				Y = (int)Math.Round(rootY * rasterizationScale),
				Width = Math.Max(0, (int)Math.Ceiling(localWidth * totalScale)),
				Height = Math.Max(0, (int)Math.Ceiling(localHeight * totalScale))
			};
		}

		public static UiTitleBarRegions ComputeTitleBarRegions(int windowPixelWidth, int localTitleBarHeight, int borderDipThickness, double rasterizationScale, double uiScaleMultiplier)
		{
			var physicalHeight = Math.Max(0, (int)Math.Ceiling(localTitleBarHeight * rasterizationScale * uiScaleMultiplier));
			var borderWidth = Math.Max(1, (int)Math.Ceiling(borderDipThickness * rasterizationScale));
			var safeWindowWidth = Math.Max(0, windowPixelWidth);

			return new UiTitleBarRegions
			{
				BorderWidth = borderWidth,
				RightBorderX = Math.Max(0, safeWindowWidth - borderWidth),
				CaptionWidth = safeWindowWidth,
				PhysicalTitleBarHeight = physicalHeight
			};
		}
	}
}
