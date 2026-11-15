// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using KyraFiles.App.Services.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class UserSettingsMigrationTests
	{
		private string _testRoot = null!;

		[TestInitialize]
		public void Setup()
		{
			_testRoot = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString(), "SettingsMigrationTests");
			Directory.CreateDirectory(_testRoot);
		}

		[TestCleanup]
		public void Cleanup()
		{
			try
			{
				if (Directory.Exists(_testRoot))
					Directory.Delete(_testRoot, true);
			}
			catch
			{
			}
		}

		[TestMethod]
		public void MigrateDirectory_CopiesAllFilesNonDestructively()
		{
			var sourceDir = Path.Combine(_testRoot, "legacy_settings");
			var targetDir = Path.Combine(_testRoot, "target_settings");

			Directory.CreateDirectory(sourceDir);
			File.WriteAllText(Path.Combine(sourceDir, "user_settings.json"), "{\"Theme\":\"Dark\"}");
			File.WriteAllText(Path.Combine(sourceDir, "filetags.json"), "{\"Tags\":[]}");
			File.WriteAllText(Path.Combine(sourceDir, "custom.db"), "binary-data");

			var service = new UserSettingsMigrationService();
			var copied = service.MigrateDirectory(sourceDir, targetDir);

			Assert.AreEqual(3, copied);
			Assert.IsTrue(File.Exists(Path.Combine(targetDir, "user_settings.json")));
			Assert.IsTrue(File.Exists(Path.Combine(targetDir, "filetags.json")));
			Assert.IsTrue(File.Exists(Path.Combine(targetDir, "custom.db")));
			Assert.AreEqual("{\"Theme\":\"Dark\"}", File.ReadAllText(Path.Combine(targetDir, "user_settings.json")));
		}

		[TestMethod]
		public void MigrateDirectory_DoesNotOverwriteExistingTargetFiles()
		{
			var sourceDir = Path.Combine(_testRoot, "legacy_settings");
			var targetDir = Path.Combine(_testRoot, "target_settings");

			Directory.CreateDirectory(sourceDir);
			Directory.CreateDirectory(targetDir);

			File.WriteAllText(Path.Combine(sourceDir, "user_settings.json"), "{\"Theme\":\"LegacyLight\"}");
			File.WriteAllText(Path.Combine(targetDir, "user_settings.json"), "{\"Theme\":\"KyraDark\"}");

			var service = new UserSettingsMigrationService();
			var copied = service.MigrateDirectory(sourceDir, targetDir);

			Assert.AreEqual(0, copied);
			Assert.AreEqual("{\"Theme\":\"KyraDark\"}", File.ReadAllText(Path.Combine(targetDir, "user_settings.json")));
		}

		[TestMethod]
		public void MigrateDirectory_ReturnsZeroWhenSourceDoesNotExist()
		{
			var sourceDir = Path.Combine(_testRoot, "non_existent_folder");
			var targetDir = Path.Combine(_testRoot, "target_settings");

			var service = new UserSettingsMigrationService();
			var copied = service.MigrateDirectory(sourceDir, targetDir);

			Assert.AreEqual(0, copied);
			Assert.IsFalse(Directory.Exists(targetDir));
		}

		[TestMethod]
		public void EnsureMigrated_ReturnsFalseWhenTargetAlreadyHasSettings()
		{
			var targetLocalFolder = Path.Combine(_testRoot, "TargetAppLocalState");
			var settingsDir = Path.Combine(targetLocalFolder, "settings");
			Directory.CreateDirectory(settingsDir);
			File.WriteAllText(Path.Combine(settingsDir, "user_settings.json"), "{\"Already\":\"Configured\"}");

			var service = new UserSettingsMigrationService();
			var migrated = service.EnsureMigrated(targetLocalFolder);

			Assert.IsFalse(migrated);
			Assert.AreEqual("{\"Already\":\"Configured\"}", File.ReadAllText(Path.Combine(settingsDir, "user_settings.json")));
		}
	}
}
