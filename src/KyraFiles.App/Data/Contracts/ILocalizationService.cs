// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Contracts
{
	public interface ILocalizationService
	{
		string LocalizeFromResourceKey(string resourceKey);
	}
}
