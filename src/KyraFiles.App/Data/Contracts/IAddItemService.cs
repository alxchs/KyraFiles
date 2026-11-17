// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Contracts
{
	/// <summary>
	/// A service to retrieve available item types
	/// </summary>
	public interface IAddItemService
	{
		/// <summary>
		/// Initialize the service
		/// </summary>
		/// <returns>Task</returns>
		Task InitializeAsync();

		/// <summary>
		/// Gets a list of the available item types
		/// </summary>
		/// <returns>List of the available item types</returns>
		List<ShellNewEntry>? GetEntries();
	}
}
