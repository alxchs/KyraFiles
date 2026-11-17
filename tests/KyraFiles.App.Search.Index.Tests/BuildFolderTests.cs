// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using KyraFiles.Shared.BuildFolder;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class BuildFolderTests
{
	private static string CreateAgyTempDirectory(out string guidDir)
	{
		string guid = Guid.NewGuid().ToString("N");
		guidDir = Path.Combine(Path.GetTempPath(), "agy", guid);
		string targetDir = Path.Combine(guidDir, "desenv", "utils", "KyraFiles", "Files", "f047");
		Directory.CreateDirectory(targetDir);
		return targetDir;
	}

	private static void RunGitCommand(string workingDir, string arguments)
	{
		var psi = new ProcessStartInfo
		{
			FileName = "git",
			Arguments = arguments,
			WorkingDirectory = workingDir,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to launch git.");
		proc.WaitForExit(5000);
		if (proc.ExitCode != 0)
		{
			string err = proc.StandardError.ReadToEnd();
			throw new InvalidOperationException($"git {arguments} failed with code {proc.ExitCode}: {err}");
		}
	}

	private static void SafeDeleteDirectory(string path)
	{
		if (!Directory.Exists(path))
			return;

		try
		{
			foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
			{
				try
				{
					File.SetAttributes(file, FileAttributes.Normal);
				}
				catch { }
			}

			Directory.Delete(path, true);
		}
		catch
		{
			// Best-effort cleanup
		}
	}

	[TestMethod]
	public void AC1_AC2_AC6_TableDriven_BuildFolderScan_MatchesExpectedRulesAndNegativeControl()
	{
		string testRoot = CreateAgyTempDirectory(out string guidDir);

		try
		{
			// 1. .NET project
			string dotNetDir = Path.Combine(testRoot, "DotNetApp");
			Directory.CreateDirectory(dotNetDir);
			File.WriteAllText(Path.Combine(dotNetDir, "DotNetApp.csproj"), "<Project />");
			string dotNetBin = Path.Combine(dotNetDir, "bin");
			string dotNetObj = Path.Combine(dotNetDir, "obj");
			Directory.CreateDirectory(dotNetBin);
			Directory.CreateDirectory(dotNetObj);
			File.WriteAllBytes(Path.Combine(dotNetBin, "app.dll"), new byte[2048]);
			File.WriteAllBytes(Path.Combine(dotNetObj, "app.pdb"), new byte[1024]);

			// 2. Node.js project
			string nodeDir = Path.Combine(testRoot, "NodeApp");
			Directory.CreateDirectory(nodeDir);
			File.WriteAllText(Path.Combine(nodeDir, "package.json"), "{}");
			string nodeModules = Path.Combine(nodeDir, "node_modules");
			Directory.CreateDirectory(nodeModules);
			File.WriteAllBytes(Path.Combine(nodeModules, "index.js"), new byte[4096]);

			// 3. Flutter project
			string flutterDir = Path.Combine(testRoot, "FlutterApp");
			Directory.CreateDirectory(flutterDir);
			File.WriteAllText(Path.Combine(flutterDir, "pubspec.yaml"), "name: flutter_app");
			string flutterBuild = Path.Combine(flutterDir, "build");
			string flutterDartTool = Path.Combine(flutterDir, ".dart_tool");
			Directory.CreateDirectory(flutterBuild);
			Directory.CreateDirectory(flutterDartTool);
			File.WriteAllBytes(Path.Combine(flutterBuild, "app.apk"), new byte[8192]);
			File.WriteAllBytes(Path.Combine(flutterDartTool, "package_config.json"), new byte[512]);

			// 4. Negative Control (AC-2): bin folder WITHOUT marker file in parent
			string randomDir = Path.Combine(testRoot, "RandomFolderWithoutMarker");
			Directory.CreateDirectory(randomDir);
			File.WriteAllText(Path.Combine(randomDir, "notes.txt"), "no project marker here");
			string fakeBin = Path.Combine(randomDir, "bin");
			Directory.CreateDirectory(fakeBin);
			File.WriteAllBytes(Path.Combine(fakeBin, "data.bin"), new byte[1000]);

			// Execute scanner
			var scanner = new BuildFolderScanner();
			var candidates = scanner.Scan(testRoot);

			// Assert AC-2 Negative Control: fakeBin MUST NOT be present
			bool hasFakeBin = candidates.Any(c => string.Equals(c.FolderPath, fakeBin, StringComparison.OrdinalIgnoreCase));
			Assert.IsFalse(hasFakeBin, "AC-2: Pastas 'bin' sem arquivo marcador no pai NÃO podem ser oferecidas.");

			// Table verification for AC-1
			var expectedEntries = new (string ExpectedFolder, string ExpectedProject, string ExpectedType, long ExpectedSize)[]
			{
				(dotNetBin, "DotNetApp", ".NET", 2048),
				(dotNetObj, "DotNetApp", ".NET", 1024),
				(nodeModules, "NodeApp", "Node.js", 4096),
				(flutterBuild, "FlutterApp", "Flutter", 8192),
				(flutterDartTool, "FlutterApp", "Flutter", 512),
			};

			Assert.AreEqual(expectedEntries.Length, candidates.Count, "AC-1: Quantidade total de pastas de build encontradas deve bater exatamente.");

			long totalExpectedSize = 0;
			foreach (var (expectedFolder, expectedProject, expectedType, expectedSize) in expectedEntries)
			{
				var matched = candidates.FirstOrDefault(c => string.Equals(c.FolderPath, expectedFolder, StringComparison.OrdinalIgnoreCase));
				Assert.IsNotNull(matched, $"AC-1: Pasta esperada não encontrada: {expectedFolder}");
				Assert.AreEqual(expectedProject, matched.ProjectName, $"AC-1: Nome de projeto incorreto para {expectedFolder}");
				Assert.AreEqual(expectedType, matched.ProjectType, $"AC-1: Tipo de projeto incorreto para {expectedFolder}");

				long calculatedSize = matched.CalculateSize();
				Assert.AreEqual(expectedSize, calculatedSize, $"AC-1: Tamanho incorreto para {expectedFolder}");
				totalExpectedSize += expectedSize;
			}

			// Assert AC-6: Total size summary
			long totalSummary = BuildFolderScanner.CalculateTotalSize(candidates);
			Assert.AreEqual(totalExpectedSize, totalSummary, "AC-6: Resumo total a liberar deve bater exatamente com a soma dos itens medidos.");
		}
		finally
		{
			SafeDeleteDirectory(guidDir);
		}
	}

	[TestMethod]
	public void FR003_CustomRules_JsonLoadingAndMerging()
	{
		string json = """
		[
			{
				"FolderName": "dist",
				"ProjectType": "Webpack",
				"MarkerPatterns": [ "webpack.config.js" ]
			}
		]
		""";

		var customRules = BuildFolderRules.LoadFromJson(json);
		Assert.AreEqual(1, customRules.Count);
		Assert.AreEqual("dist", customRules[0].FolderName);
		Assert.AreEqual("Webpack", customRules[0].ProjectType);
		Assert.AreEqual("webpack.config.js", customRules[0].MarkerPatterns[0]);

		var merged = BuildFolderRules.Merge(customRules);
		Assert.IsTrue(merged.Any(r => r.FolderName == "dist" && r.ProjectType == "Webpack"));
		Assert.IsTrue(merged.Any(r => r.FolderName == "node_modules" && r.ProjectType == "Node.js"));
	}

	[TestMethod]
	public void AC5_GitTrackingSafety_TrackedBuildFolder_IsNotOffered()
	{
		string testRoot = CreateAgyTempDirectory(out string guidDir);

		try
		{
			// Initialize a temporary Git repository inside testRoot
			RunGitCommand(testRoot, "init");
			RunGitCommand(testRoot, "config user.name \"FilesTest\"");
			RunGitCommand(testRoot, "config user.email \"test@files.local\"");

			// Project A: contains a bin folder WITH a tracked file
			string projADir = Path.Combine(testRoot, "ProjectA_Tracked");
			Directory.CreateDirectory(projADir);
			File.WriteAllText(Path.Combine(projADir, "ProjectA.csproj"), "<Project />");
			string projABin = Path.Combine(projADir, "bin");
			Directory.CreateDirectory(projABin);
			string trackedFile = Path.Combine(projABin, "tracked_release.dll");
			File.WriteAllBytes(trackedFile, new byte[1024]);

			RunGitCommand(testRoot, "add ProjectA_Tracked/bin/tracked_release.dll");
			RunGitCommand(testRoot, "commit -m \"Track bin file\"");

			// Project B: contains a bin folder with UNTRACKED files only
			string projBDir = Path.Combine(testRoot, "ProjectB_Untracked");
			Directory.CreateDirectory(projBDir);
			File.WriteAllText(Path.Combine(projBDir, "ProjectB.csproj"), "<Project />");
			string projBBin = Path.Combine(projBDir, "bin");
			Directory.CreateDirectory(projBBin);
			string untrackedFile = Path.Combine(projBBin, "local_build.dll");
			File.WriteAllBytes(untrackedFile, new byte[2048]);
			// Not tracked by git

			// Scan with git tracking safety enabled
			var gitChecker = new CliGitTrackingChecker();
			var scanner = new BuildFolderScanner(gitTracker: gitChecker);
			var results = scanner.Scan(testRoot);

			// Assert FR-002, AC-5:
			// Project A's bin MUST NOT be offered (it contains a tracked file)
			bool hasProjABin = results.Any(r => string.Equals(r.FolderPath, projABin, StringComparison.OrdinalIgnoreCase));
			Assert.IsFalse(hasProjABin, "FR-002 / AC-5: Pasta de build com arquivo rastreado pelo git NÃO pode ser oferecida.");

			// Project B's bin MUST be offered (all files are untracked)
			bool hasProjBBin = results.Any(r => string.Equals(r.FolderPath, projBBin, StringComparison.OrdinalIgnoreCase));
			Assert.IsTrue(hasProjBBin, "AC-5: Pasta de build sem arquivos rastreados DEVE ser oferecida.");
		}
		finally
		{
			SafeDeleteDirectory(guidDir);
		}
	}

	[TestMethod]
	public void FR002_GitTrackingSafety_WhenGitIsUnavailable_FailsClosedAndDoesNotOfferCandidate()
	{
		string testRoot = CreateAgyTempDirectory(out string guidDir);

		try
		{
			// Initialize a temporary Git repository
			RunGitCommand(testRoot, "init");
			RunGitCommand(testRoot, "config user.name \"FilesTest\"");
			RunGitCommand(testRoot, "config user.email \"test@files.local\"");

			// Project with tracked file in bin
			string projDir = Path.Combine(testRoot, "TrackedApp");
			Directory.CreateDirectory(projDir);
			File.WriteAllText(Path.Combine(projDir, "TrackedApp.csproj"), "<Project />");
			string binDir = Path.Combine(projDir, "bin");
			Directory.CreateDirectory(binDir);
			string trackedFile = Path.Combine(binDir, "important_release.dll");
			File.WriteAllBytes(trackedFile, new byte[1024]);

			RunGitCommand(testRoot, "add TrackedApp/bin/important_release.dll");
			RunGitCommand(testRoot, "commit -m \"Track bin file\"");

			// Checker configured with PATH that has no git binary
			var envWithoutGit = new Dictionary<string, string?>
			{
				["PATH"] = @"C:\Windows\System32" // Does not contain git executable
			};
			var gitChecker = new CliGitTrackingChecker(environment: envWithoutGit);

			// Assert checker status is CannotVerify and HasTrackedFiles fails closed (returns true)
			var status = gitChecker.CheckDirectory(binDir);
			Assert.AreEqual(GitTrackingResult.CannotVerify, status, "Git inacessível deve retornar status CannotVerify.");
			Assert.IsTrue(gitChecker.HasTrackedFiles(binDir), "Git inacessível deve falhar fechado (HasTrackedFiles == true).");

			// Scanner must NOT offer the folder
			var scanner = new BuildFolderScanner(gitTracker: gitChecker);
			var results = scanner.Scan(testRoot);

			bool isOffered = results.Any(r => string.Equals(r.FolderPath, binDir, StringComparison.OrdinalIgnoreCase));
			Assert.IsFalse(isOffered, "FR-002: Pasta com arquivo rastreado NÃO pode ser oferecida quando git estiver inacessível (falha fechada).");
		}
		finally
		{
			SafeDeleteDirectory(guidDir);
		}
	}

	[TestMethod]
	public void NFR001_Scan200Projects_Benchmark_CompletesUnder30Seconds()
	{
		string testRoot = CreateAgyTempDirectory(out string guidDir);

		try
		{
			// Generate 200 synthetic projects
			const int projectCount = 200;
			for (int i = 1; i <= projectCount; i++)
			{
				string projDir = Path.Combine(testRoot, $"Proj_{i:D3}");
				Directory.CreateDirectory(projDir);
				File.WriteAllText(Path.Combine(projDir, $"Proj_{i:D3}.csproj"), "<Project />");

				string binDir = Path.Combine(projDir, "bin");
				string objDir = Path.Combine(projDir, "obj");
				Directory.CreateDirectory(binDir);
				Directory.CreateDirectory(objDir);

				File.WriteAllBytes(Path.Combine(binDir, "dummy.dll"), new byte[64]);
				File.WriteAllBytes(Path.Combine(objDir, "dummy.pdb"), new byte[32]);
			}

			var scanner = new BuildFolderScanner();
			var rounds = new List<(int Round, int Projects, double DurationMs, bool Passed)>();
			var csvBuilder = new StringBuilder();
			csvBuilder.AppendLine("Round,Projects,DurationMs,Passed");

			// Warmup / 3 rounds
			for (int round = 1; round <= 3; round++)
			{
				var sw = Stopwatch.StartNew();
				var results = scanner.Scan(testRoot);
				sw.Stop();

				double ms = sw.Elapsed.TotalMilliseconds;
				bool passed = ms < 30000.0 && results.Count == projectCount * 2;
				rounds.Add((round, projectCount, ms, passed));

				csvBuilder.AppendLine(CultureInfo.InvariantCulture, $"{round},{projectCount},{ms:F2},{passed}");
				Assert.IsTrue(passed, $"Round {round}: Varredura deve levar < 30 s (levou {ms:F2} ms) e achar {projectCount * 2} pastas (achou {results.Count}).");
			}

			// Evidence is saved into temporary test directory (isolated from repository working tree)
			string tempEvidenceDir = Path.Combine(Path.GetTempPath(), "agy", "evidence", "F047");
			Directory.CreateDirectory(tempEvidenceDir);
			string tempEvidenceFile = Path.Combine(tempEvidenceDir, "nfr001_scan_200_projects.csv");
			File.WriteAllText(tempEvidenceFile, csvBuilder.ToString());

		}
		finally
		{
			SafeDeleteDirectory(guidDir);
		}
	}

	[TestMethod]
	public void NFR001_ScanTreeWithDeepNodeModules_50kFiles_ProvesPruningUnder30Seconds()
	{
		string testRoot = CreateAgyTempDirectory(out string guidDir);

		try
		{
			// 1. Create a project with deep node_modules containing ~50,000 files
			string nodeDir = Path.Combine(testRoot, "BigNodeApp");
			Directory.CreateDirectory(nodeDir);
			File.WriteAllText(Path.Combine(nodeDir, "package.json"), "{\"name\": \"big-node-app\"}");
			string nodeModules = Path.Combine(nodeDir, "node_modules");
			Directory.CreateDirectory(nodeModules);

			const int packageCount = 50;
			const int filesPerPackage = 1000;
			Parallel.For(0, packageCount, pkgIndex =>
			{
				string pkgDir = Path.Combine(nodeModules, $"pkg_{pkgIndex:D2}");
				Directory.CreateDirectory(pkgDir);
				for (int j = 0; j < filesPerPackage; j++)
				{
					string f = Path.Combine(pkgDir, $"file_{j:D3}.js");
					using var fs = File.Create(f);
				}
			});

			// 2. Create another project alongside to verify full tree traversal
			string dotNetDir = Path.Combine(testRoot, "DotNetApp");
			Directory.CreateDirectory(dotNetDir);
			File.WriteAllText(Path.Combine(dotNetDir, "DotNetApp.csproj"), "<Project />");
			string dotNetBin = Path.Combine(dotNetDir, "bin");
			string dotNetObj = Path.Combine(dotNetDir, "obj");
			Directory.CreateDirectory(dotNetBin);
			Directory.CreateDirectory(dotNetObj);
			File.WriteAllBytes(Path.Combine(dotNetBin, "dummy.dll"), new byte[64]);
			File.WriteAllBytes(Path.Combine(dotNetObj, "dummy.pdb"), new byte[32]);

			var scanner = new BuildFolderScanner();
			var csvBuilder = new StringBuilder();
			csvBuilder.AppendLine("Round,Projects,DeepFiles,DurationMs,CandidatesFound,Passed");

			// 3 rounds measuring scan duration with pruning
			for (int round = 1; round <= 3; round++)
			{
				var sw = Stopwatch.StartNew();
				var results = scanner.Scan(testRoot);
				sw.Stop();

				double ms = sw.Elapsed.TotalMilliseconds;
				// Expected candidates: node_modules (BigNodeApp), bin (DotNetApp), obj (DotNetApp) = 3
				bool passed = ms < 30000.0 && results.Count == 3 &&
				              results.Any(r => string.Equals(r.FolderPath, nodeModules, StringComparison.OrdinalIgnoreCase));

				csvBuilder.AppendLine(CultureInfo.InvariantCulture, $"{round},2,{packageCount * filesPerPackage},{ms:F2},{results.Count},{passed}");
				Assert.IsTrue(passed, $"Round {round}: Varredura com 50k arquivos em node_modules deve podar e concluir em < 30 s (levou {ms:F2} ms).");
			}

			string tempEvidenceDir = Path.Combine(Path.GetTempPath(), "agy", "evidence", "F047");
			Directory.CreateDirectory(tempEvidenceDir);
			string tempEvidenceFile = Path.Combine(tempEvidenceDir, "nfr001_scan_pruning_50k_files.csv");
			File.WriteAllText(tempEvidenceFile, csvBuilder.ToString());
		}
		finally
		{
			SafeDeleteDirectory(guidDir);
		}
	}
}
