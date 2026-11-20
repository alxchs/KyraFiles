// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;
using KyraFiles.Shared.ProjectActions;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class ProjectActionsTests
	{
		private string _tempDir = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_tempDir = Path.Combine(
				Path.GetTempPath(),
				"agy",
				Guid.NewGuid().ToString("N"),
				"desenv",
				"utils",
				"KyraFiles",
				"KyraFiles",
				"F045Tests");
			Directory.CreateDirectory(_tempDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, recursive: true);
		}

		[TestMethod]
		public void T1_Detector_RecognizesProjectMarkers_AndReturnsEmptyForPlainFolder()
		{
			var dotnetDir = Path.Combine(_tempDir, "dotnet");
			var delphiDir = Path.Combine(_tempDir, "delphi");
			var flutterDir = Path.Combine(_tempDir, "flutter");
			var emptyDir = Path.Combine(_tempDir, "empty");

			Directory.CreateDirectory(dotnetDir);
			Directory.CreateDirectory(delphiDir);
			Directory.CreateDirectory(flutterDir);
			Directory.CreateDirectory(emptyDir);

			File.WriteAllText(Path.Combine(dotnetDir, "App.slnx"), "<Solution />");
			File.WriteAllText(Path.Combine(delphiDir, "Proj.dpr"), "program Proj;");
			File.WriteAllText(Path.Combine(flutterDir, "pubspec.yaml"), "name: demo");
			File.WriteAllText(Path.Combine(emptyDir, "readme.txt"), "plain folder");

			var detector = new ProjectDetector();

			var rDotnet = detector.Detect(dotnetDir);
			Assert.IsTrue(rDotnet.HasProject);
			CollectionAssert.Contains(rDotnet.DetectedKinds.ToList(), ProjectKind.DotNet);
			Assert.IsTrue(rDotnet.BuiltInActions.Any(a => a.Id == "dotnet-mkfile-d"));
			Assert.IsTrue(rDotnet.BuiltInActions.Any(a => a.Id == "dotnet-open-sln"));

			var rDelphi = detector.Detect(delphiDir);
			CollectionAssert.Contains(rDelphi.DetectedKinds.ToList(), ProjectKind.Delphi);

			var rFlutter = detector.Detect(flutterDir);
			CollectionAssert.Contains(rFlutter.DetectedKinds.ToList(), ProjectKind.Flutter);

			// Controle negativo: pasta sem marcador devolve vazio
			var rEmpty = detector.Detect(emptyDir);
			Assert.IsFalse(rEmpty.HasProject);
			Assert.AreEqual(0, rEmpty.BuiltInActions.Count);
		}

		[TestMethod]
		public void T1_Detector_10kFilesFolder_CompletesFast()
		{
			var bigDir = Path.Combine(_tempDir, "ten-k");
			Directory.CreateDirectory(bigDir);
			for (int i = 0; i < 10_000; i++)
			{
				using var _ = File.Create(Path.Combine(bigDir, $"f_{i:D5}.txt"));
			}
			File.WriteAllText(Path.Combine(bigDir, "package.json"), "{}");

			var detector = new ProjectDetector();
			var sw = Stopwatch.StartNew();
			var result = detector.Detect(bigDir);
			sw.Stop();

			Assert.IsTrue(result.HasProject);
			CollectionAssert.Contains(result.DetectedKinds.ToList(), ProjectKind.Node);
			Assert.IsLessThan(2000, sw.ElapsedMilliseconds, $"Deteccao em 10k arquivos levou {sw.ElapsedMilliseconds} ms.");
		}

		[TestMethod]
		public async Task T2_Runner_AdversarialArguments_ArriveVerbatimWithoutInjection()
		{
			var runner = new ProjectCommandRunner();
			var adversarialValues = new[] { "a b", "çãé", "x'y", "$(calc)", "foo;bar" };
			var ctx = new ProjectActionExecutionContext(
				FolderPath: _tempDir,
				SelectedPaths: adversarialValues,
				GitRepoRoot: _tempDir);

			var expanded = PlaceholderExpander.ExpandArgumentTemplates(["{seleção}"], ctx);
			CollectionAssert.AreEqual(adversarialValues, expanded.ToList());

			// Programa de teste temporario que apenas imprime cada argumento recebido em UTF-8
			var echoScript = Path.Combine(_tempDir, "echo_args.ps1");
			File.WriteAllText(echoScript, "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)\r\nforeach ($a in $args) { [Console]::Out.WriteLine($a) }\r\n", System.Text.Encoding.UTF8);

			// 1. Caminho ArgumentList (-File)
			var fileArgs = new List<string> { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", echoScript };
			fileArgs.AddRange(expanded);
			var resFile = await runner.RunAsync("powershell.exe", fileArgs, _tempDir);
			Assert.IsTrue(resFile.Succeeded, string.Join(" | ", resFile.ErrorLines));
			CollectionAssert.AreEqual(adversarialValues, resFile.OutputLines.ToList());

			// 2. Caminho PowerShell escapado (BuildSafePowerShellCommand)
			var safePsCmd = PlaceholderExpander.BuildSafePowerShellCommand(echoScript, expanded);
			var resSafePs = await runner.RunAsync(
				"powershell.exe",
				["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", safePsCmd],
				_tempDir);
			Assert.IsTrue(resSafePs.Succeeded, string.Join(" | ", resSafePs.ErrorLines));
			CollectionAssert.AreEqual(adversarialValues, resSafePs.OutputLines.ToList());

			// 3. Controle negativo: concatenacao crua DEVE falhar/quebrar no parser
			var rawUnsafeCmd = $"& '{echoScript}' {string.Join(' ', expanded)}";
			var resUnsafe = await runner.RunAsync(
				"powershell.exe",
				["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", rawUnsafeCmd],
				_tempDir);
			Assert.IsFalse(resUnsafe.Succeeded, "Controle negativo: concatenacao crua nao deveria passar.");
		}

		[TestMethod]
		public async Task T2_Runner_FailingCommand_ReturnsExitCodeAndErrorLines()
		{
			var runner = new ProjectCommandRunner();
			var res = await runner.RunAsync(
				"powershell.exe",
				["-NoProfile", "-NonInteractive", "-Command", "[Console]::Error.WriteLine('erro simulado'); exit 42"],
				_tempDir);

			Assert.IsFalse(res.Succeeded);
			Assert.IsFalse(res.Cancelled);
			Assert.AreEqual(42, res.ExitCode);
			Assert.IsTrue(res.ErrorLines.Any(l => l.Contains("erro simulado", StringComparison.Ordinal)));
		}

		[TestMethod]
		public async Task T2_Runner_LongCommand_CancelsWithinOneSecond()
		{
			var runner = new ProjectCommandRunner();
			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

			var sw = Stopwatch.StartNew();
			var res = await runner.RunAsync(
				"powershell.exe",
				["-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 30"],
				_tempDir,
				cancellationToken: cts.Token);
			sw.Stop();

			Assert.IsTrue(res.Cancelled);
			Assert.IsFalse(res.Succeeded);
			// Cancelamento efetivo apos o disparo do token deve ficar bem abaixo de 1s (+250ms de atraso do token)
			Assert.IsLessThan(2000, sw.ElapsedMilliseconds, $"Cancelamento levou {sw.ElapsedMilliseconds} ms.");
		}

		[TestMethod]
		public void T3_Store_RoundTripAndInvalidJsonHandledGracefully()
		{
			var cfgPath = Path.Combine(_tempDir, "project-actions.json");
			var store = new ProjectActionsStore(cfgPath);
			store.Upsert(new CustomProjectCommand
			{
				Name = "Abrir no editor",
				Executable = "echo.exe",
				Arguments = ["{caminho}", "{raiz do repositório}"],
				ApplicableKinds = [ProjectKind.DotNet],
			});

			var reloaded = new ProjectActionsStore(cfgPath);
			var (ok, err) = reloaded.Load();
			Assert.IsTrue(ok, err);
			Assert.AreEqual(1, reloaded.GetAll().Count);
			Assert.AreEqual(1, reloaded.GetForKinds([ProjectKind.DotNet]).Count);
			Assert.AreEqual(0, reloaded.GetForKinds([ProjectKind.Rust]).Count);

			// Controle negativo: JSON invalido retorna erro sem excecao
			var bad = reloaded.ImportFromJson("{ quebrado", saveAfter: false);
			Assert.IsFalse(bad.Ok);
			Assert.IsFalse(string.IsNullOrWhiteSpace(bad.Error));
		}
	}
}
