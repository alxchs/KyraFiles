// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinRT;

namespace Files.App.Helpers
{
	/// <summary>
	/// Attached property that automatically applies a scaled geometric shape clip to a FrameworkElement
	/// according to the current <see cref="ControlShapeStyleKind"/> setting.
	/// </summary>
	public sealed class ShapeClip : DependencyObject
	{
		private static readonly ConditionalWeakTable<FrameworkElement, ShapeClipController> Controllers = new();

		public static double GetScale(DependencyObject obj)
		{
			return (double)obj.GetValue(ScaleProperty);
		}

		public static void SetScale(DependencyObject obj, double value)
		{
			obj.SetValue(ScaleProperty, value);
		}

		public static readonly DependencyProperty ScaleProperty =
			DependencyProperty.RegisterAttached(
				"Scale",
				typeof(double),
				typeof(ShapeClip),
				new PropertyMetadata(0.0, OnScaleChanged));

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not FrameworkElement element)
				return;

			var controller = Controllers.GetValue(element, el => new ShapeClipController(el));
			controller.UpdateScale((double)e.NewValue);
		}

		private sealed class ShapeClipController
		{
			private readonly FrameworkElement _element;
			private double _scale;
			private bool _isSubscribed;
			private IAppearanceSettingsService? _appearanceSettings;

			public ShapeClipController(FrameworkElement element)
			{
				_element = element;
				_element.Loaded += Element_Loaded;
				_element.Unloaded += Element_Unloaded;
			}

			public void UpdateScale(double scale)
			{
				_scale = scale;
				if (_element.IsLoaded)
				{
					if (_scale > 0 && !_isSubscribed)
						Subscribe();
					else if (_scale <= 0 && _isSubscribed)
						Unsubscribe();

					Apply();
				}
			}

			private void Element_Loaded(object sender, RoutedEventArgs e)
			{
				if (_scale > 0)
				{
					Subscribe();
					Apply();
				}
			}

			private void Element_Unloaded(object sender, RoutedEventArgs e)
			{
				Unsubscribe();
			}

			private void Subscribe()
			{
				if (_isSubscribed)
					return;

				_element.SizeChanged -= Element_SizeChanged;
				_element.SizeChanged += Element_SizeChanged;

				_appearanceSettings ??= Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
				if (_appearanceSettings != null)
				{
					_appearanceSettings.PropertyChanged -= AppearanceSettings_PropertyChanged;
					_appearanceSettings.PropertyChanged += AppearanceSettings_PropertyChanged;
				}

				_isSubscribed = true;
			}

			private void Unsubscribe()
			{
				if (!_isSubscribed)
					return;

				_element.SizeChanged -= Element_SizeChanged;

				if (_appearanceSettings != null)
				{
					_appearanceSettings.PropertyChanged -= AppearanceSettings_PropertyChanged;
				}

				_isSubscribed = false;
			}

			private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
			{
				Apply();
			}

			private void AppearanceSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(IAppearanceSettingsService.ControlShapeStyle))
				{
					if (_element.DispatcherQueue != null)
					{
						_element.DispatcherQueue.TryEnqueue(Apply);
					}
					else
					{
						Apply();
					}
				}
			}

			private void Apply()
			{
				if (_scale <= 0)
				{
					ShapeGeometryHelper.ApplyClip(_element, ControlShapeStyleKind.FluentStandard);
					return;
				}

				_appearanceSettings ??= Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
				var style = _appearanceSettings?.ControlShapeStyle ?? ControlShapeStyleKind.FluentStandard;
				ShapeGeometryHelper.ApplyClip(_element, style, (float)_scale);
			}
		}
	}
}
