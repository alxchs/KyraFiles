// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Numerics;
using Windows.Foundation;

namespace KyraFiles.App.Services
{
	/// <summary>
	/// Coordinates global UI scaling and compensated layout across application windows (Files Custom F090).
	/// </summary>
	public sealed class UiScaleService : IUiScaleService
	{
		private readonly IAppearanceSettingsService _appearanceSettingsService;
		private readonly Dictionary<Window, (FrameworkElement Root, TypedEventHandler<object, WindowSizeChangedEventArgs> Handler)> _trackedWindows = [];

		public int ScalePercent
		{
			get
			{
				var p = _appearanceSettingsService.AppUiScalePercent;
				return p > 0 ? p : 100;
			}
		}

		public double ScaleFactor => ScalePercent / 100.0;

		public UiScaleService(IAppearanceSettingsService appearanceSettingsService)
		{
			_appearanceSettingsService = appearanceSettingsService;
			_appearanceSettingsService.PropertyChanged += AppearanceSettingsService_PropertyChanged;
		}

		private void AppearanceSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IAppearanceSettingsService.AppUiScalePercent))
			{
				UpdateAllTrackedWindows();
			}
		}

		public void ApplyScale(Window window, FrameworkElement? rootElement)
		{
			if (rootElement is null)
				return;

			if (!_trackedWindows.ContainsKey(window))
			{
				TypedEventHandler<object, WindowSizeChangedEventArgs> handler = (s, e) =>
				{
					ApplyLayoutCompensation(window, rootElement, ScaleFactor);
				};
				window.SizeChanged += handler;
				window.Closed += (s, e) =>
				{
					_trackedWindows.Remove(window);
				};
				_trackedWindows[window] = (rootElement, handler);
			}

			ApplyLayoutCompensation(window, rootElement, ScaleFactor);
		}

		private void UpdateAllTrackedWindows()
		{
			var factor = ScaleFactor;
			foreach (var (window, (root, _)) in _trackedWindows.ToList())
			{
				try
				{
					ApplyLayoutCompensation(window, root, factor);
				}
				catch
				{
				}
			}
		}

		private static void ApplyLayoutCompensation(Window window, FrameworkElement rootElement, double scaleFactor)
		{
			if (Math.Abs(scaleFactor - 1.0) < 0.001)
			{
				rootElement.Scale = Vector3.One;
				rootElement.Width = double.NaN;
				rootElement.Height = double.NaN;
				rootElement.HorizontalAlignment = HorizontalAlignment.Stretch;
				rootElement.VerticalAlignment = VerticalAlignment.Stretch;
				rootElement.TransformMatrix = Matrix4x4.Identity;
				return;
			}

			var bounds = window.Bounds;
			var windowWidth = bounds.Width;
			var windowHeight = bounds.Height;

			if (windowWidth <= 0 || windowHeight <= 0)
				return;

			rootElement.HorizontalAlignment = HorizontalAlignment.Left;
			rootElement.VerticalAlignment = VerticalAlignment.Top;
			rootElement.Width = windowWidth / scaleFactor;
			rootElement.Height = windowHeight / scaleFactor;
			rootElement.Scale = new Vector3((float)scaleFactor, (float)scaleFactor, 1.0f);
			rootElement.TransformMatrix = Matrix4x4.CreateScale((float)scaleFactor, (float)scaleFactor, 1.0f);
		}
	}
}
