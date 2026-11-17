// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;
using Windows.Storage;
using WinRT;

namespace KyraFiles.App.Converters
{
	internal sealed partial class StorageDeleteOptionToBooleanConverter : IValueConverter
	{
		[DynamicWindowsRuntimeCast(typeof(StorageDeleteOption))]
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is StorageDeleteOption option && option == StorageDeleteOption.PermanentDelete;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return (value is bool bl && bl) ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default;
		}
	}
}
