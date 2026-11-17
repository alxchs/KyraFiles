// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace KyraFiles.App.Converters
{
	internal sealed partial class EnumToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value?.ToString() ?? string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
