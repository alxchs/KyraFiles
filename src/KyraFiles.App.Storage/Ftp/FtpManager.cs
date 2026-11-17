// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Net;

namespace KyraFiles.App.Storage
{
	public static class FtpManager
	{
		public static readonly Dictionary<string, NetworkCredential> Credentials = [];

		public static readonly NetworkCredential Anonymous = new("anonymous", "anonymous");
	}
}
