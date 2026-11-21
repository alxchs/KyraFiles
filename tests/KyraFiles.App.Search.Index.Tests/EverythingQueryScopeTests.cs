// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class EverythingQueryScopeTests
	{
		private static readonly HashSet<string> Existing = new(StringComparer.OrdinalIgnoreCase)
		{
			@"c:\desenv\KyraFiles",
			@"C:\Program Files",
			@"\\servidor\pasta",
			@"C:\",
		};

		private static EverythingQueryScope Parse(string q) => EverythingQueryScope.Parse(q, Existing.Contains);

		[TestMethod]
		public void AC1_SemCaminho_BuscaNoIndiceTodo()
		{
			var r = Parse("nota.txt");
			Assert.AreEqual("nota.txt", r.Terms);
			Assert.IsNull(r.ScopePath);
			Assert.IsFalse(r.HasNativePathSyntax);
		}

		[TestMethod]
		public void AC2_MascaraSeguidaDeCaminho_LimitaAPasta()
		{
			var r = Parse(@"*.cs c:\desenv\KyraFiles");
			Assert.AreEqual("*.cs", r.Terms);
			Assert.AreEqual(@"c:\desenv\KyraFiles", r.ScopePath);
		}

		[TestMethod]
		public void AC2_BarraFinalEBarraNormal_SaoOMesmoCaminho()
		{
			Assert.AreEqual(@"c:\desenv\KyraFiles", Parse(@"*.cs c:\desenv\KyraFiles\").ScopePath);
			Assert.AreEqual(@"c:\desenv\KyraFiles", Parse("*.cs c:/desenv/KyraFiles").ScopePath);
		}

		[TestMethod]
		public void AC3_CaminhoComEspacoEntreAspas()
		{
			var r = Parse("*.txt \"C:\\Program Files\"");
			Assert.AreEqual("*.txt", r.Terms);
			Assert.AreEqual(@"C:\Program Files", r.ScopePath);
		}

		[TestMethod]
		public void AC3_CaminhoComEspacoSemAspas()
		{
			var r = Parse(@"*.txt C:\Program Files");
			Assert.AreEqual("*.txt", r.Terms);
			Assert.AreEqual(@"C:\Program Files", r.ScopePath);
		}

		[TestMethod]
		public void AC4_PastaInexistente_TextoVaiComoFoiDigitado()
		{
			var r = Parse(@"*.txt C:\pasta-que-nao-existe");
			Assert.AreEqual(@"*.txt C:\pasta-que-nao-existe", r.Terms);
			Assert.IsNull(r.ScopePath);
		}

		[TestMethod]
		public void AC4_VerificacaoDePastaQueFalha_NaoQuebra()
		{
			var r = EverythingQueryScope.Parse(@"*.txt C:\x", _ => throw new UnauthorizedAccessException());
			Assert.IsNull(r.ScopePath);
			Assert.AreEqual(@"*.txt C:\x", r.Terms);
		}

		[TestMethod]
		[DataRow(@"path:c:\desenv *.cs")]
		[DataRow(@"PARENT:c:\desenv")]
		[DataRow(@"infolder:c:\desenv foo")]
		public void AC5_SintaxePropriaDoEverything_PassaSemAlteracao(string q)
		{
			var r = Parse(q);
			Assert.IsTrue(r.HasNativePathSyntax);
			Assert.AreEqual(q, r.Terms);
			Assert.IsNull(r.ScopePath);
		}

		[TestMethod]
		public void AC14_ComposeEParseSaoInversos()
		{
			var composed = EverythingQueryScope.Compose("*.txt", @"C:\Program Files");
			var r = Parse(composed);
			Assert.AreEqual("*.txt", r.Terms);
			Assert.AreEqual(@"C:\Program Files", r.ScopePath);
			Assert.AreEqual("*.txt", EverythingQueryScope.Compose("*.txt", "Home"));
			Assert.AreEqual("*.txt", EverythingQueryScope.Compose("*.txt", ""));
		}

		[TestMethod]
		public void Borda_TextoSoComCaminho_ListaAPasta()
		{
			var r = Parse(@"c:\desenv\KyraFiles");
			Assert.AreEqual(string.Empty, r.Terms);
			Assert.AreEqual(@"c:\desenv\KyraFiles", r.ScopePath);
		}

		[TestMethod]
		public void Borda_PastaDeRede()
		{
			var r = Parse(@"*.pdf \\servidor\pasta");
			Assert.AreEqual("*.pdf", r.Terms);
			Assert.AreEqual(@"\\servidor\pasta", r.ScopePath);
		}

		[TestMethod]
		public void Borda_RaizDaUnidade_MantemABarra()
			=> Assert.AreEqual(@"C:\", Parse(@"*.log C:\").ScopePath);

		[TestMethod]
		public void Borda_LetraDeUnidadeSemBarra_NaoViraEscopo()
		{
			var r = Parse("*.log c:");
			Assert.IsNull(r.ScopePath);
			Assert.AreEqual("*.log c:", r.Terms);
		}

		[TestMethod]
		public void Borda_TextoVazio()
		{
			var r = Parse("   ");
			Assert.AreEqual(string.Empty, r.Terms);
			Assert.IsNull(r.ScopePath);
		}
	}
}
