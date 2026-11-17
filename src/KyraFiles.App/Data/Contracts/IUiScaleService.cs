// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace KyraFiles.App.Data.Contracts
{
	/// <summary>
	/// Service that applies and coordinates UI scaling and layout compensation across application windows.
	/// </summary>
	public interface IUiScaleService
	{
		/// <summary>
		/// Gets the current scale percentage (e.g. 80, 90, 100, 110, 125, 150).
		/// </summary>
		int ScalePercent { get; }

		/// <summary>
		/// Gets the scale factor multiplier (e.g. 0.8, 0.9, 1.0, 1.1, 1.25, 1.5).
		/// </summary>
		double ScaleFactor { get; }

		/// <summary>
		/// Applies the current UI scale with layout compensation to the specified Window and root element.
		/// </summary>
		/// <param name="window">The target window.</param>
		/// <param name="rootElement">The root visual element to scale and compensate.</param>
		void ApplyScale(Window window, FrameworkElement? rootElement);
	}
}
