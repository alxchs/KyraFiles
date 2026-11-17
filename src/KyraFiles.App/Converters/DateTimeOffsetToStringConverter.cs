// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace KyraFiles.App.Converters
{
	internal sealed partial class DateTimeOffsetToStringConverter : IValueConverter
	{
		private static readonly IDateTimeFormatter formatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is null
				? string.Empty
				: formatter.ToLongLabel((DateTimeOffset)value);
		}

		public object? ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return DateTimeOffset.Parse((value as string)!);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}
