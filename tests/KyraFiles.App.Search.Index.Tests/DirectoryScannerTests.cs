// Copyright (c) Files Community. Licensed under the MIT License.

using System.Diagnostics;
using KyraFiles.App.Search.Index.Basic;
using KyraFiles.App.Search.Index.Engine;
using KyraFiles.App.Search.Index.Models;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class DirectoryScannerTests
{
	[TestMethod]
	public void ScanDirectory_CyclicJunction_CompletesWithoutDuplicates_AC9()
	{
		string tempBase = Path.Combine(
			Path.GetTempPath(),
			"claude",
			"desenv",
			"utils",
			"FilesApp",
			"f015",
			"t7_cycle_" + Guid.NewGuid().ToString("N"));

		string subDir = Path.Combine(tempBase, "subpasta");
		string targetFile = Path.Combine(subDir, "arquivo_unico_ac9.txt");
		string cycleJunction = Path.Combine(subDir, "ciclo_ancestral");
		string siblingJunction = Path.Combine(tempBase, "atalho_subpasta");

		Directory.CreateDirectory(subDir);
		File.WriteAllText(targetFile, "conteudo de teste AC-9");

		try
		{
			// Cria junção em ciclo apontando para a pasta ancestral (tempBase)
			RunCmd($"mklink /J \"{cycleJunction}\" \"{tempBase}\"");
			// Cria junção irmã apontando para subpasta
			RunCmd($"mklink /J \"{siblingJunction}\" \"{subDir}\"");

			ScanProgress lastProgress = default;
			var progress = new SynchronousProgress<ScanProgress>(p => lastProgress = p);

			var index = DirectoryScanner.ScanDirectory(Guid.NewGuid(), tempBase, progress);

			Assert.IsTrue(lastProgress.IsCompleted, "O progresso final deve indicar conclusão.");

			// Busca pelo arquivo dentro da subpasta: deve aparecer exatamente 1 vez (sem duplicar via junção)
			var fileMatches = CollectMatches(index, "arquivo_unico_ac9.txt");
			Assert.HasCount(1, fileMatches, "O arquivo não pode ser duplicado por junções ou ciclos.");
			Assert.AreEqual(targetFile, fileMatches[0].FullPath);

			// As junções em si são registradas como itens (sem descer nelas)
			var juncMatches = CollectMatches(index, "ciclo_ancestral");
			Assert.HasCount(1, juncMatches, "A junção em ciclo deve constar como item único.");
		}
		finally
		{
			TryDeleteJunction(cycleJunction);
			TryDeleteJunction(siblingJunction);
			TryDeleteDirectory(tempBase);
		}
	}

	[TestMethod]
	public void ScanDirectory_HardLink_AppearsOnceWithPrimaryName_AC21()
	{
		string tempBase = Path.Combine(
			Path.GetTempPath(),
			"claude",
			"desenv",
			"utils",
			"FilesApp",
			"f015",
			"t7_hardlink_" + Guid.NewGuid().ToString("N"));

		// Colocamos o original em "zzz_original" e o segundo hardlink em "aaa_secundario"
		// para garantir que mesmo quando a varredura visita o link secundário antes do original,
		// o HardLinkResolver (FindFirstFileNameW) preserva o primeiro nome criado!
		string dirOriginal = Path.Combine(tempBase, "zzz_original");
		string dirSecondary = Path.Combine(tempBase, "aaa_secundario");
		Directory.CreateDirectory(dirOriginal);
		Directory.CreateDirectory(dirSecondary);

		string primaryFile = Path.Combine(dirOriginal, "relatorio_primario.txt");
		string secondaryLink = Path.Combine(dirSecondary, "relatorio_copia_hardlink.txt");

		File.WriteAllText(primaryFile, "conteudo compartilhado por hardlink");

		try
		{
			int exitCode = RunCmd($"fsutil hardlink create \"{secondaryLink}\" \"{primaryFile}\"");
			Assert.AreEqual(0, exitCode, "fsutil hardlink create deve funcionar sem admin no mesmo volume NTFS.");
			Assert.IsTrue(File.Exists(secondaryLink), "O segundo hardlink deve existir no disco.");

			var index = DirectoryScanner.ScanDirectory(Guid.NewGuid(), tempBase);

			// O Windows NÃO garante que o "primeiro nome" (FindFirstFileNameW) seja o primeiro link criado:
			// medido em 27/10/2026, `fsutil hardlink list` devolveu o link secundário primeiro em 5 de 300
			// rodadas. O contrato do índice é "uma vez só, no nome que o próprio Windows chama de primeiro";
			// por isso o esperado é lido do Windows, não presumido pela ordem de criação.
			string expectedFirstFullPath = GetFirstHardLinkFullPath(primaryFile);
			string expectedName = Path.GetFileName(expectedFirstFullPath);
			string otherName = string.Equals(expectedName, "relatorio_primario.txt", StringComparison.OrdinalIgnoreCase)
				? "relatorio_copia_hardlink.txt"
				: "relatorio_primario.txt";

			var expectedMatches = CollectMatches(index, expectedName);
			var otherMatches = CollectMatches(index, otherName);

			Assert.HasCount(1, expectedMatches, "O arquivo com hardlink deve aparecer exatamente uma vez, no primeiro nome informado pelo Windows.");
			Assert.AreEqual(expectedFirstFullPath, expectedMatches[0].FullPath, ignoreCase: true);
			Assert.IsEmpty(otherMatches, "O outro nome do hardlink não deve gerar duplicata no índice.");
		}
		finally
		{
			TryDeleteDirectory(tempBase);
		}
	}

	private static List<SearchResultItem> CollectMatches(Storage.VolumeIndex index, string query)
	{
		var results = new List<SearchResultItem>();
		var engine = new QueryEngine();
		var matcher = new Query.QueryMatcher(query);
		engine.Execute(index, matcher, batch => results.AddRange(batch));
		return results;
	}

	private static int RunCmd(string command)
	{
		var psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		using var proc = Process.Start(psi)!;
		proc.WaitForExit();
		return proc.ExitCode;
	}

	private static string GetFirstHardLinkFullPath(string filePath)
	{
		var psi = new ProcessStartInfo("cmd.exe", $"/c fsutil hardlink list \"{filePath}\"")
		{
			CreateNoWindow = true,
			UseShellExecute = false,
			RedirectStandardOutput = true
		};
		using var proc = Process.Start(psi)!;
		string output = proc.StandardOutput.ReadToEnd();
		proc.WaitForExit();
		Assert.AreEqual(0, proc.ExitCode, "fsutil hardlink list deve funcionar.");

		string firstRelative = output
			.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0]
			.Trim();

		// O fsutil devolve o caminho sem a letra do volume.
		return Path.GetPathRoot(filePath)!.TrimEnd('\\') + firstRelative;
	}

	private static void TryDeleteJunction(string path)
	{
		if (Directory.Exists(path))
		{
			try { Directory.Delete(path); } catch { }
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		if (Directory.Exists(path))
		{
			try { Directory.Delete(path, recursive: true); } catch { }
		}
	}

	private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
	{
		public void Report(T value) => handler(value);
	}
}
