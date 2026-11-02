// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	/// <summary>
	/// Abstraction for storing and retrieving credentials from the secure platform vault.
	/// </summary>
	public interface ICredentialsVault
	{
		void SavePassword(string resourceName, string username, string password);
		void DeleteSavedPassword(string resourceName, string username);
		string GetPassword(string resourceName, string username);
	}

	/// <summary>
	/// Default implementation using Windows PasswordVault via CredentialsHelpers.
	/// </summary>
	internal sealed class DefaultCredentialsVault : ICredentialsVault
	{
		public void SavePassword(string resourceName, string username, string password)
		{
			CredentialsHelpers.SavePassword(resourceName, username, password);
		}

		public void DeleteSavedPassword(string resourceName, string username)
		{
			CredentialsHelpers.DeleteSavedPassword(resourceName, username);
		}

		public string GetPassword(string resourceName, string username)
		{
			return CredentialsHelpers.GetPassword(resourceName, username);
		}
	}
}

