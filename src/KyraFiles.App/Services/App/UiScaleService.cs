// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Helpers;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Numerics;
using Windows.Foundation;

namespace KyraFiles.App.Services
{
	/// <summary>
	/// Coordinates global UI scaling and compensated layout across application windows (KyraFiles F090).
	/// </summary>
	public sealed class UiScaleService : IUiScaleService
	{
		private sealed class TrackedWindowEntry
		{
			public required FrameworkElement Root { get; set; }
			public required TypedEventHandler<object, WindowSizeChangedEventArgs> SizeChangedHandler { get; init; }
			public required TypedEventHandler<object, WindowEventArgs> ClosedHandler { get; init; }
			public XamlRoot? AttachedXamlRoot { get; set; }
			public TypedEventHandler<XamlRoot, XamlRootChangedEventArgs>? XamlRootChangedHandler { get; set; }
		}

		private readonly IAppearanceSettingsService _appearanceSettingsService;
		private readonly Dictionary<Window, TrackedWindowEntry> _trackedWindows = [];

		public int ScalePercent => UiScaleMath.NormalizeScalePercent(_appearanceSettingsService.AppUiScalePercent);

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

			if (_trackedWindows.TryGetValue(window, out var existing))
			{
				existing.Root = rootElement;
				AttachXamlRootHandler(window, existing);
			}
			else
			{
				TrackedWindowEntry? entry = null;
				TypedEventHandler<object, WindowSizeChangedEventArgs> sizeHandler = (s, e) =>
				{
					if (entry is null)
						return;

					AttachXamlRootHandler(window, entry);
					ApplyLayoutCompensation(entry.Root, e.Size.Width, e.Size.Height, ScaleFactor);
				};
				TypedEventHandler<object, WindowEventArgs> closedHandler = (s, e) =>
				{
					DetachTrackedWindow(window);
				};

				entry = new TrackedWindowEntry
				{
					Root = rootElement,
					SizeChangedHandler = sizeHandler,
					ClosedHandler = closedHandler,
				};

				window.SizeChanged += sizeHandler;
				window.Closed += closedHandler;
				_trackedWindows[window] = entry;
				AttachXamlRootHandler(window, entry);
			}

			var bounds = window.Bounds;
			ApplyLayoutCompensation(rootElement, bounds.Width, bounds.Height, ScaleFactor);
		}

		private void AttachXamlRootHandler(Window window, TrackedWindowEntry entry)
		{
			var xamlRoot = entry.Root.XamlRoot;
			if (xamlRoot is null || ReferenceEquals(xamlRoot, entry.AttachedXamlRoot))
				return;

			DetachXamlRootHandler(entry);

			TypedEventHandler<XamlRoot, XamlRootChangedEventArgs> xamlRootHandler = (s, e) =>
			{
				try
				{
					var bounds = window.Bounds;
					ApplyLayoutCompensation(entry.Root, bounds.Width, bounds.Height, ScaleFactor);
				}
				catch
				{
					DetachTrackedWindow(window);
				}
			};

			xamlRoot.Changed += xamlRootHandler;
			entry.AttachedXamlRoot = xamlRoot;
			entry.XamlRootChangedHandler = xamlRootHandler;
		}

		private static void DetachXamlRootHandler(TrackedWindowEntry entry)
		{
			if (entry.AttachedXamlRoot is not null && entry.XamlRootChangedHandler is not null)
			{
				try
				{
					entry.AttachedXamlRoot.Changed -= entry.XamlRootChangedHandler;
				}
				catch
				{
				}
			}

			entry.AttachedXamlRoot = null;
			entry.XamlRootChangedHandler = null;
		}

		private void DetachTrackedWindow(Window window)
		{
			if (!_trackedWindows.Remove(window, out var removed))
				return;

			try
			{
				window.SizeChanged -= removed.SizeChangedHandler;
				window.Closed -= removed.ClosedHandler;
			}
			catch
			{
			}

			DetachXamlRootHandler(removed);
		}

		private void UpdateAllTrackedWindows()
		{
			var factor = ScaleFactor;
			foreach (var (window, entry) in _trackedWindows.ToList())
			{
				try
				{
					AttachXamlRootHandler(window, entry);
					var bounds = window.Bounds;
					ApplyLayoutCompensation(entry.Root, bounds.Width, bounds.Height, factor);
				}
				catch
				{
					DetachTrackedWindow(window);
				}
			}
		}

		private static void ApplyLayoutCompensation(FrameworkElement rootElement, double windowWidth, double windowHeight, double scaleFactor)
		{
			var layout = UiScaleMath.ComputeCompensatedSize(windowWidth, windowHeight, scaleFactor);
			if (!layout.IsValid)
				return;

			if (layout.IsIdentity)
			{
				rootElement.CenterPoint = Vector3.Zero;
				rootElement.Scale = Vector3.One;
				rootElement.TransformMatrix = Matrix4x4.Identity;
				rootElement.Margin = new Thickness(0);
				rootElement.Width = double.NaN;
				rootElement.Height = double.NaN;
				rootElement.HorizontalAlignment = HorizontalAlignment.Stretch;
				rootElement.VerticalAlignment = VerticalAlignment.Stretch;
				return;
			}

			// Quando scaleFactor < 1.0, Width/Height compensados excedem os bounds da janela e o XAML
			// aplica LayoutClip antes do Scale visual. Margem negativa compensa o overflow sem clipping.
			var rightMargin = Math.Min(0, windowWidth - layout.Width);
			var bottomMargin = Math.Min(0, windowHeight - layout.Height);

			rootElement.HorizontalAlignment = HorizontalAlignment.Left;
			rootElement.VerticalAlignment = VerticalAlignment.Top;
			rootElement.Margin = new Thickness(0, 0, rightMargin, bottomMargin);
			rootElement.Width = layout.Width;
			rootElement.Height = layout.Height;
			rootElement.CenterPoint = Vector3.Zero;
			rootElement.Scale = new Vector3((float)scaleFactor, (float)scaleFactor, 1.0f);
			rootElement.TransformMatrix = Matrix4x4.Identity;
		}
	}
}
