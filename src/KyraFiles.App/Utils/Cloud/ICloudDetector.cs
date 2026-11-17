// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Utils.Cloud
{
	public interface ICloudDetector
	{
		Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync();

		IAsyncEnumerable<ICloudProvider> DetectCloudProvidersProgressiveAsync();
	}
}
