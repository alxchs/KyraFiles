// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Contracts
{
	public interface IThreadingService
	{
		Task ExecuteOnUiThreadAsync(Action action);

		Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func);
	}
}
