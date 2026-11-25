// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.Shared.Helpers;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class SmokeTests
{
	[TestMethod]
	public void SmokeTest_ProjectSetup_Passes()
	{
		var result = 1 + 1;
		Assert.AreEqual(2, result);
	}

	[TestMethod]
	public void ProtocolUriParser_StandardWinE_NavigatesToHome()
	{
		var uri = new Uri("kyrafiles:");
		var info = ProtocolUriParser.Parse(uri);

		Assert.AreEqual(ProtocolAction.Home, info.Action);
		Assert.AreEqual(string.Empty, info.TargetValue);
	}

	[TestMethod]
	public void ProtocolUriParser_FolderQuery_ExtractsFolderPath()
	{
		var uri = new Uri("kyrafiles:?folder=C%3A%5CWindows");
		var info = ProtocolUriParser.Parse(uri);

		Assert.AreEqual(ProtocolAction.Folder, info.Action);
		Assert.AreEqual(@"C:\Windows", info.TargetValue);
	}

	[TestMethod]
	public void ProtocolUriParser_RawPath_ExtractsDirectory()
	{
		var uri = new Uri("kyrafiles:C%3A%5CUsers");
		var info = ProtocolUriParser.Parse(uri);

		Assert.AreEqual(ProtocolAction.Folder, info.Action);
		Assert.AreEqual(@"C:\Users", info.TargetValue);
	}

	[TestMethod]
	public void ProtocolUriParser_TabWithCoordinates_ParsesPosition()
	{
		var uri = new Uri("kyrafiles:?tab=TestTab&x=500&y=300");
		var info = ProtocolUriParser.Parse(uri);

		Assert.AreEqual(ProtocolAction.Tab, info.Action);
		Assert.AreEqual("TestTab", info.TargetValue);
		Assert.AreEqual(500, info.MoveDx);
		Assert.AreEqual(300, info.MoveDy);
	}

	[TestMethod]
	public void ProtocolUriParser_CmdUntrusted_ExtractsCommand()
	{
		var uri = new Uri("kyrafiles:?cmd=-select%20C%3A%5CTest.txt");
		var info = ProtocolUriParser.Parse(uri);

		Assert.AreEqual(ProtocolAction.Cmd, info.Action);
		Assert.AreEqual("-select C:\\Test.txt", info.TargetValue);
	}

	[TestMethod]
	public void ProtocolUriParser_ForeignScheme_ReturnsHome()
	{
		var uri = new Uri("http://localhost");
		var info = ProtocolUriParser.Parse(uri);

		Assert.AreEqual(ProtocolAction.Home, info.Action);
		Assert.AreEqual(string.Empty, info.TargetValue);
	}

	[TestMethod]
	public void TextEncodingHelper_DetectEncoding_Windows1252AndUtf8()
	{
		// Teste 1: UTF-8 sem BOM com caracteres acentuados válidos
		byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes("Altera\u00e7\u00e3o e compensa\u00e7\u00e3o");
		var detectedUtf8 = TextEncodingHelper.DetectEncoding(utf8Bytes, utf8Bytes.Length);
		Assert.AreEqual(System.Text.Encoding.UTF8.CodePage, detectedUtf8.CodePage);

		// Teste 2: Windows-1252 / ANSI com caracteres acentuados típicos de arquivo Delphi/Pascal legado
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		var win1252 = System.Text.Encoding.GetEncoding(1252);
		byte[] ansiBytes = win1252.GetBytes("Altera\u00e7\u00e3o : Permitir a informa\u00e7\u00e3o");
		var detectedAnsi = TextEncodingHelper.DetectEncoding(ansiBytes, ansiBytes.Length);
		
		// Deve detectar 1252 ou codificação compatível (Latin1/1252) e decodificar com sucesso sem caracteres de interrogação/substituição
		string decoded = detectedAnsi.GetString(ansiBytes);
		Assert.IsTrue(decoded.Contains("Altera\u00e7\u00e3o"));
		Assert.IsTrue(decoded.Contains("informa\u00e7\u00e3o"));
	}
}
