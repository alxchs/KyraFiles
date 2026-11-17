// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using LibGit2Sharp;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class LibGit2RepoViewTests
{
	private string? _sessionRootDir;
	private string? _testRepoDir;

	[TestInitialize]
	public void Setup()
	{
		var sessionId = Guid.NewGuid().ToString("N");
		_sessionRootDir = Path.Combine(Path.GetTempPath(), "agy", sessionId);
		var tempRoot = Path.Combine(_sessionRootDir, "KyraFiles", "GitTestRepo");
		Directory.CreateDirectory(tempRoot);
		_testRepoDir = tempRoot;
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (_sessionRootDir is not null && Directory.Exists(_sessionRootDir))
		{
			try
			{
				// Remove read-only attributes that git may leave on packfiles/objects
				foreach (var file in Directory.GetFiles(_sessionRootDir, "*", SearchOption.AllDirectories))
				{
					File.SetAttributes(file, FileAttributes.Normal);
				}
				Directory.Delete(_sessionRootDir, true);
			}
			catch
			{
			}
		}
	}

	private static string ResolveHeadName(string repoPath)
	{
		using var repo = new Repository(repoPath);
		var branch = repo.Head;
		if (branch?.Tip is null)
			return string.Empty;

		if (repo.Info.IsHeadDetached || string.Equals(branch.FriendlyName, "(no branch)", StringComparison.OrdinalIgnoreCase))
		{
			return branch.Tip.Sha.Length >= 7 ? branch.Tip.Sha[..7] : branch.Tip.Sha;
		}

		return branch.FriendlyName;
	}

	[TestMethod]
	public void NormalBranch_ReturnsBranchFriendlyName()
	{
		Assert.IsNotNull(_testRepoDir);
		Repository.Init(_testRepoDir);

		using (var repo = new Repository(_testRepoDir))
		{
			var testFile = Path.Combine(_testRepoDir, "readme.txt");
			File.WriteAllText(testFile, "initial commit");
			LibGit2Sharp.Commands.Stage(repo, testFile);

			var sig = new Signature("TestAuthor", "test@example.com", DateTimeOffset.Now);
			repo.Commit("Initial commit", sig, sig);
		}

		var headName = ResolveHeadName(_testRepoDir);
		Assert.IsFalse(string.IsNullOrEmpty(headName));
		Assert.IsTrue(headName is "master" or "main");
	}

	[TestMethod]
	public void DetachedHead_ReturnsShortSevenCharacterSha()
	{
		Assert.IsNotNull(_testRepoDir);
		Repository.Init(_testRepoDir);

		string commitSha;
		using (var repo = new Repository(_testRepoDir))
		{
			var testFile = Path.Combine(_testRepoDir, "readme.txt");
			File.WriteAllText(testFile, "commit 1");
			LibGit2Sharp.Commands.Stage(repo, testFile);

			var sig = new Signature("TestAuthor", "test@example.com", DateTimeOffset.Now);
			var commit = repo.Commit("Commit 1", sig, sig);
			commitSha = commit.Sha;

			// Checkout the commit directly to detach HEAD
			LibGit2Sharp.Commands.Checkout(repo, commit);
			Assert.IsTrue(repo.Info.IsHeadDetached);
		}

		var headName = ResolveHeadName(_testRepoDir);
		Assert.IsNotNull(headName);
		Assert.AreEqual(7, headName.Length);
		Assert.AreEqual(commitSha[..7], headName);
	}

	[TestMethod]
	public void TabTitleFormat_ComposesFolderAndBranchOrSha()
	{
		const string folderName = "KyraFiles";
		const string branch = "main";
		const string sha = "1c1a02d";

		var normalTitle = $"{folderName} ({branch})";
		var detachedTitle = $"{folderName} ({sha})";

		Assert.AreEqual("KyraFiles (main)", normalTitle);
		Assert.AreEqual("KyraFiles (1c1a02d)", detachedTitle);
	}

	[TestMethod]
	public void GitIgnore_FiltersIgnoredFilesAndDirectories()
	{
		Assert.IsNotNull(_testRepoDir);
		Repository.Init(_testRepoDir);

		var gitignorePath = Path.Combine(_testRepoDir, ".gitignore");
		File.WriteAllText(gitignorePath, "bin/\nobj/\n*.log\nignored_file.txt\n");

		using var repo = new Repository(_testRepoDir);

		Assert.IsTrue(repo.Ignore.IsPathIgnored("ignored_file.txt"));
		Assert.IsTrue(repo.Ignore.IsPathIgnored("app.log"));
		Assert.IsTrue(repo.Ignore.IsPathIgnored("bin/"));
		Assert.IsTrue(repo.Ignore.IsPathIgnored("bin/out.dll"));
		Assert.IsTrue(repo.Ignore.IsPathIgnored("obj/project.assets.json"));

		Assert.IsFalse(repo.Ignore.IsPathIgnored("regular_file.txt"));
		Assert.IsFalse(repo.Ignore.IsPathIgnored("src/main.cs"));
		Assert.IsFalse(repo.Ignore.IsPathIgnored("README.md"));
	}

	private static List<KyraFiles.App.Data.Items.GitWorktreeItem> ResolveWorktrees(string repoPath, string currentPath)
	{
		using var repository = new Repository(repoPath);
		var list = new List<KyraFiles.App.Data.Items.GitWorktreeItem>();

		var mainWorktreePath = repository.Info.WorkingDirectory;
		if (!string.IsNullOrEmpty(mainWorktreePath))
		{
			var mainDir = mainWorktreePath.TrimEnd('\\', '/');
			var mainName = Path.GetFileName(mainDir) ?? "main";
			var head = repository.Head;
			var branchName = head?.Tip is null ? null : head.FriendlyName;
			var isCurrent = string.Equals(Path.GetFullPath(currentPath).TrimEnd('\\'), Path.GetFullPath(mainDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

			list.Add(new KyraFiles.App.Data.Items.GitWorktreeItem(mainName, mainDir, branchName, isCurrent, false));
		}

		foreach (var wt in repository.Worktrees)
		{
			try
			{
				var wtRepo = wt.WorktreeRepository;
				if (wtRepo is null)
					continue;

				var wtPath = wtRepo.Info.WorkingDirectory?.TrimEnd('\\', '/');
				if (string.IsNullOrEmpty(wtPath) || !Directory.Exists(wtPath))
					continue;

				var head = wtRepo.Head;
				var branchName = head?.Tip is null ? null : head.FriendlyName;
				var isCurrent = string.Equals(Path.GetFullPath(currentPath).TrimEnd('\\'), Path.GetFullPath(wtPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

				list.Add(new KyraFiles.App.Data.Items.GitWorktreeItem(wt.Name, wtPath, branchName, isCurrent, wt.IsLocked));
			}
			catch
			{
			}
		}

		return list;
	}

	[TestMethod]
	public void Worktrees_EnumeratesMainAndLinkedWorktrees()
	{
		Assert.IsNotNull(_testRepoDir);
		Assert.IsNotNull(_sessionRootDir);
		Repository.Init(_testRepoDir);

		using (var repo = new Repository(_testRepoDir))
		{
			var testFile = Path.Combine(_testRepoDir, "readme.txt");
			File.WriteAllText(testFile, "commit 1");
			LibGit2Sharp.Commands.Stage(repo, testFile);

			var sig = new Signature("TestAuthor", "test@example.com", DateTimeOffset.Now);
			repo.Commit("Commit 1", sig, sig);

			var wtDir = Path.Combine(_sessionRootDir, "KyraFiles", "GitTestRepo_wt1");
			repo.Worktrees.Add("wt1", wtDir, false);
		}

		var worktrees = ResolveWorktrees(_testRepoDir, _testRepoDir);
		Assert.AreEqual(2, worktrees.Count);
		Assert.IsTrue(worktrees.Any(w => w.IsCurrent));
		Assert.IsTrue(worktrees.Any(w => w.Name == "wt1" && !w.IsCurrent));
	}
}
