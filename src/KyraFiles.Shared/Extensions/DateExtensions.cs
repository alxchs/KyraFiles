// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices.ComTypes;

namespace KyraFiles.Shared.Extensions
{
	public static class DateExtensions
	{
		public static DateTime ToDateTime(this FILETIME time)
		{
			uint low = (uint)time.dwLowDateTime;
			ulong high = (ulong)time.dwHighDateTime;
			long fileTime = (long)((high << 32) + low);

			try
			{
				return DateTime.FromFileTimeUtc(fileTime);
			}
			catch
			{
				return DateTime.FromFileTimeUtc(0xFFFFFFFF);
			}
		}
	}
}
