// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Converters
{
	internal sealed partial class StringToFontFamilyConverter : IValueConverter
	{
		public static FontFamily ConvertToFontFamily(string? value)
		{
			if (string.IsNullOrWhiteSpace(value) || value == Strings.Default.GetLocalizedResource())
				return new FontFamily("Segoe UI Variable, Segoe UI");

			return new FontFamily(value);
		}

		public object? Convert(object? value, Type? targetType, object? parameter, string? language)
		{
			return ConvertToFontFamily(value as string);
		}

		public object? ConvertBack(object? value, Type? targetType, object? parameter, string? language)
		{
			throw new NotImplementedException();
		}
	}
}

