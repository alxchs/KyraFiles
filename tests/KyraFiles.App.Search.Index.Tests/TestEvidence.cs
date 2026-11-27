// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.IO;

namespace KyraFiles.App.Search.Index.Tests
{
	/// <summary>
	/// Where benchmark tests write their CSV evidence. The repository folder is used only when
	/// <c>KYRAFILES_WRITE_EVIDENCE=1</c>; otherwise the CSV goes to the test's own temp folder,
	/// so a plain <c>dotnet test</c> never modifies tracked files.
	/// </summary>
	internal static class TestEvidence
	{
		public const string VariableName = "KYRAFILES_WRITE_EVIDENCE";

		public static bool WritesToRepository
			=> Environment.GetEnvironmentVariable(VariableName) == "1";

		public static string GetCsvPath(string feature, string fileName, string tempFallbackDirectory)
		{
			var directory = WritesToRepository
				? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "agents", "evidence", feature))
				: Path.Combine(tempFallbackDirectory, "evidence", feature);

			Directory.CreateDirectory(directory);
			return Path.Combine(directory, fileName);
		}
	}
}
