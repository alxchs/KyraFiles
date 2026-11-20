// Copyright (c) 2023 KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.Core.Storage.Contracts
{
	/// <summary>
	/// A disposable object which can notify of changes to the folder.
	/// </summary>
	public interface IWatcher : IDisposable
	{
		/// <summary>
		/// Starts the watcher
		/// </summary>
		void StartWatcher();

		/// <summary>
		/// Stops the watcher
		/// </summary>
		void StopWatcher();
	}
}
