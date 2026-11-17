// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Utils.Storage
{
	public interface IStorageHistoryOperations : IDisposable
	{
		/// <summary>
		/// Redo an action with given <paramref name="history"/>
		/// </summary>
		/// <param name="history"></param>
		/// <returns></returns>
		Task<ReturnResult> Undo(IStorageHistory history);

		/// <summary>
		/// Redo an action with given <paramref name="history"/>
		/// </summary>
		/// <param name="history"></param>
		/// <returns></returns>
		Task<ReturnResult> Redo(IStorageHistory history);
	}
}