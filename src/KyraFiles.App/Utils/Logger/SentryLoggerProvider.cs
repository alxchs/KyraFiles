// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace KyraFiles.App.Utils.Logger
{
	public sealed partial class SentryLoggerProvider : ILoggerProvider
	{
		public ILogger CreateLogger(string categoryName)
		{
			return new SentryLogger();
		}

		public void Dispose()
		{
		}
	}
}
