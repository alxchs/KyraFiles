// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Services.DateTimeFormatter
{
	public interface IDateTimeFormatterFactory
	{
		IDateTimeFormatter GetDateTimeFormatter(DateTimeFormats dateTimeFormat);
	}
}
