// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Items;
using KyraFiles.App.Helpers;
using KyraFiles.App.Utils.Storage;
using Microsoft.Data.Sqlite;
using System.IO;

namespace KyraFiles.App.Utils.Git
{
	internal static class SvnHelpers
	{
		public static string? GetSvnRepositoryPath(string? path, string? root)
		{
			if (string.IsNullOrEmpty(root) || string.IsNullOrWhiteSpace(path))
				return null;

			if (root.EndsWith('\\'))
				root = root.Substring(0, root.Length - 1);

			if (path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
				path.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
				ShellStorageFolder.IsShellPath(path))
			{
				return null;
			}

			try
			{
				var current = path;
				while (!string.IsNullOrWhiteSpace(current) && !current.Equals(root, StringComparison.OrdinalIgnoreCase))
				{
					var svnPath = Path.Combine(current, ".svn");
					if (Directory.Exists(svnPath))
						return current;

					var parentDir = PathNormalization.GetParentDir(current);
					if (parentDir == current)
						break;

					current = parentDir;
				}
			}
			catch
			{
			}

			return null;
		}

		public static BranchItem? GetSvnHead(string? repositoryPath)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath))
				return null;

			var wcDbPath = Path.Combine(repositoryPath, ".svn", "wc.db");
			if (!File.Exists(wcDbPath))
			{
				return new BranchItem("svn", true, false, 0, 0, 0);
			}

			string branchName = "trunk";
			int pendingChanges = 0;

			try
			{
				var connectionString = new SqliteConnectionStringBuilder
				{
					DataSource = wcDbPath,
					Mode = SqliteOpenMode.ReadOnly
				}.ToString();

				using var connection = new SqliteConnection(connectionString);
				connection.Open();

				using var cmdBranch = connection.CreateCommand();
				cmdBranch.CommandText = "SELECT repos_path FROM NODES WHERE local_relpath = '' LIMIT 1;";
				var reposPathObj = cmdBranch.ExecuteScalar();
				if (reposPathObj is string reposPath && !string.IsNullOrWhiteSpace(reposPath))
				{
					var parts = reposPath.Trim('/').Split('/');
					branchName = parts.Length > 1 ? parts[^1] : parts[0];
				}

				using var cmdPending = connection.CreateCommand();
				cmdPending.CommandText = "SELECT COUNT(*) FROM ACTUAL_NODE;";
				var countObj = cmdPending.ExecuteScalar();
				if (countObj is long count)
					pendingChanges = (int)count;
			}
			catch
			{
			}

			return new BranchItem(branchName, true, false, 0, 0, pendingChanges);
		}
	}
}

