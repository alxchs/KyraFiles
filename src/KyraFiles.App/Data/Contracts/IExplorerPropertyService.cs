// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Models;

namespace KyraFiles.App.Data.Contracts
{
	/// <summary>
	/// Provides metadata and property values from the Windows Property System.
	/// </summary>
	public interface IExplorerPropertyService
	{
		/// <summary>
		/// Gets the list of all available Explorer column properties.
		/// </summary>
		IReadOnlyList<DynamicColumnDefinition> GetAllAvailableProperties();

		/// <summary>
		/// Gets the formatted display value of a property for a specified file path.
		/// </summary>
		string? GetFormattedProperty(string filePath, string canonicalName);
	}
}
