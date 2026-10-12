// Copyright (c) Files Community. Licensed under the MIT License.

using System.Diagnostics;
using Files.App.Search.Index.Volumes;

namespace Files.App.Search.Index.Tests;

[TestClass]
public sealed class VolumeTableAndScopeTests
{
	[TestMethod]
	public void VolumeTable_Simulated_FindsCorrectVolumeAndResolvesSubst()
	{
		var guidC = Guid.NewGuid();
		var volC = new VolumeInfo(
			guidC,
			$@"\\?\Volume{{{guidC}}}\",
			@"C:\",
			[@"C:\", @"C:\Mount\DriveC\"],
			3 /* DRIVE_FIXED */);

		var guidD = Guid.NewGuid();
		var volD = new VolumeInfo(
			guidD,
			$@"\\?\Volume{{{guidD}}}\",
			@"D:\",
			[@"D:\"],
			3 /* DRIVE_FIXED */);

		var substMap = new Dictionary<string, string>
		{
			{ "X:", @"C:\desenv\workspace" }
		};

		var table = new VolumeTable([volC, volD], substMap);

		// 1. Busca normal no C:
		var foundC = table.FindVolumeByPath(@"C:\Users\Alexandre\file.txt", out var resolvedC);
		Assert.IsNotNull(foundC);
		Assert.AreEqual(guidC, foundC.VolumeGuid);
		Assert.AreEqual(@"C:\Users\Alexandre\file.txt", resolvedC);

		// 2. Busca através da letra subst X:
		var foundSubst = table.FindVolumeByPath(@"X:\projeto\codigo.cs", out var resolvedSubst);
		Assert.IsNotNull(foundSubst);
		Assert.AreEqual(guidC, foundSubst.VolumeGuid);
		Assert.AreEqual(@"C:\desenv\workspace\projeto\codigo.cs", resolvedSubst);

		// 3. Busca no volume D:
		var foundD = table.FindVolumeByPath(@"D:\Backup\dados.zip", out var resolvedD);
		Assert.IsNotNull(foundD);
		Assert.AreEqual(guidD, foundD.VolumeGuid);
		Assert.AreEqual(@"D:\Backup\dados.zip", resolvedD);
	}

	[TestMethod]
	public void ScopeResolver_RewritesPathBackToUserPrefix_AC4_AC6()
	{
		var guidC = Guid.NewGuid();
		var volC = new VolumeInfo(guidC, $@"\\?\Volume{{{guidC}}}\", @"C:\", [@"C:\"], 3);

		var substMap = new Dictionary<string, string>
		{
			{ "X:", @"C:\desenv\projeto" }
		};

		var table = new VolumeTable([volC], substMap);

		// Usuário pesquisou dentro de X:\subpasta
		var scope = ScopeResolver.Resolve(@"X:\subpasta", table);

		Assert.IsFalse(scope.IsGlobal);
		Assert.IsNotNull(scope.Volume);
		Assert.AreEqual(@"X:\", scope.UserPrefix);
		Assert.AreEqual(@"C:\desenv\projeto\", scope.RealPrefix);

		// Índice devolve o caminho físico real
		string realResult = @"C:\desenv\projeto\subpasta\documento.pdf";

		// Reescrita devolve caminho com a letra do usuário
		string rewritten = ScopeResolver.RewriteResultPath(realResult, scope);
		Assert.AreEqual(@"X:\subpasta\documento.pdf", rewritten);
	}

	[TestMethod]
	public void ScopeResolver_GlobalSearch_PreservesOriginalPaths()
	{
		var table = new VolumeTable([]);
		var scopeHome = ScopeResolver.Resolve("Home", table);
		Assert.IsTrue(scopeHome.IsGlobal);

		var scopeThisPc = ScopeResolver.Resolve("This PC", table);
		Assert.IsTrue(scopeThisPc.IsGlobal);

		string input = @"C:\teste\arquivo.txt";
		Assert.AreEqual(input, ScopeResolver.RewriteResultPath(input, scopeHome));
	}

	[TestMethod]
	public void Integration_RealSubstAndJunction_CreatedAndCleanedUp()
	{
		// Encontra uma letra livre entre Z e M
		string? freeLetter = null;
		for (char c = 'Z'; c >= 'M'; c--)
		{
			string drive = $"{c}:";
			if (!Directory.Exists(drive))
			{
				freeLetter = drive;
				break;
			}
		}

		if (freeLetter is null)
		{
			Assert.Inconclusive("Nenhuma letra de drive disponível para teste de subst.");
			return;
		}

		string tempBase = Path.Combine(Path.GetTempPath(), "claude", "desenv", "utils", "FilesApp", "f015", "t6_test");
		string realTarget = Path.Combine(tempBase, "alvo");
		string junctionPath = Path.Combine(tempBase, "link_juncao");

		Directory.CreateDirectory(realTarget);

		bool substCreated = false;
		try
		{
			// Cria o subst real
			var psiSubst = new ProcessStartInfo("subst", $"{freeLetter} \"{realTarget}\"")
			{
				CreateNoWindow = true,
				UseShellExecute = false
			};
			using var procSubst = Process.Start(psiSubst);
			procSubst?.WaitForExit();
			substCreated = procSubst?.ExitCode == 0;

			// Cria uma junção real
			var psiJunction = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junctionPath}\" \"{realTarget}\"")
			{
				CreateNoWindow = true,
				UseShellExecute = false
			};
			using var procJunc = Process.Start(psiJunction);
			procJunc?.WaitForExit();

			// Atualiza tabela com o sistema real
			var table = new VolumeTable();

			if (substCreated)
			{
				Assert.IsTrue(table.SubstAliases.ContainsKey(freeLetter), $"Subst {freeLetter} deveria ter sido detectado.");

				// Testa resolução pelo subst real
				var scope = ScopeResolver.Resolve($"{freeLetter}\\teste", table);
				Assert.IsFalse(scope.IsGlobal);
				Assert.IsNotNull(scope.Volume);

				string realFile = Path.Combine(realTarget, "teste", "arquivo.txt");
				string userVisible = ScopeResolver.RewriteResultPath(realFile, scope);
				Assert.IsTrue(userVisible.StartsWith($"{freeLetter}\\", StringComparison.OrdinalIgnoreCase));
			}

			if (Directory.Exists(junctionPath))
			{
				// Testa resolução de junção canônica
				var juncScope = ScopeResolver.Resolve(junctionPath, table);
				Assert.IsNotNull(juncScope.Volume);
			}
		}
		finally
		{
			if (substCreated)
			{
				var psiDel = new ProcessStartInfo("subst", $"{freeLetter} /D")
				{
					CreateNoWindow = true,
					UseShellExecute = false
				};
				using var p = Process.Start(psiDel);
				p?.WaitForExit();
			}

			if (Directory.Exists(junctionPath))
			{
				try { Directory.Delete(junctionPath); } catch { }
			}

			if (Directory.Exists(tempBase))
			{
				try { Directory.Delete(tempBase, true); } catch { }
			}
		}
	}
}

