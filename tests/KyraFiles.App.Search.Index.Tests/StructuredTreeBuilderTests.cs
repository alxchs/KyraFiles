// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.ViewModels.Previews;
using System.Text;

namespace KyraFiles.App.Search.Index.Tests;

/// <summary>F054 T2a (AC-2, AC-3): bounded JSON/XML trees and error position.</summary>
[TestClass]
public sealed class StructuredTreeBuilderTests
{
	[TestMethod]
	public void Json_BuildsNestedTreeWithLabelsAndValues()
	{
		var result = StructuredTreeBuilder.BuildJson("{\"name\": \"x\", \"tags\": [1, true, null], \"inner\": {\"k\": 2.5}}");

		Assert.IsTrue(result.IsValid);
		Assert.IsFalse(result.Truncated);
		var root = result.Root!;
		Assert.AreEqual(StructuredNodeKind.Object, root.Kind);
		Assert.AreEqual(3, root.Children.Count);
		Assert.AreEqual("name", root.Children[0].Label);
		Assert.AreEqual("x", root.Children[0].Value);

		var tags = root.Children[1];
		Assert.AreEqual(StructuredNodeKind.Array, tags.Kind);
		CollectionAssert.AreEqual(new[] { "[0]", "[1]", "[2]" }, tags.Children.Select(c => c.Label).ToArray());
		CollectionAssert.AreEqual(new[] { "1", "true", "null" }, tags.Children.Select(c => c.Value).ToArray());

		Assert.AreEqual("2.5", root.Children[2].Children[0].Value);
		Assert.AreEqual(8, result.NodeCount);
	}

	[TestMethod]
	public void Json_ThousandKeys_AllNodesPresent()
	{
		var sb = new StringBuilder("{");
		for (var i = 0; i < 1000; i++)
			sb.Append(i == 0 ? "" : ",").Append("\"k").Append(i).Append("\": ").Append(i);
		sb.Append('}');

		var result = StructuredTreeBuilder.BuildJson(sb.ToString());

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual(1000, result.Root!.Children.Count);
		Assert.AreEqual("k999", result.Root.Children[999].Label);
	}

	[TestMethod]
	public void Json_AllowsCommentsAndTrailingCommas()
	{
		var result = StructuredTreeBuilder.BuildJson("{\n  // comment\n  \"a\": 1,\n}");

		Assert.IsTrue(result.IsValid);
		Assert.AreEqual("a", result.Root!.Children.Single().Label);
	}

	[TestMethod]
	public void Json_Invalid_ReportsOneBasedLineAndColumn()
	{
		var result = StructuredTreeBuilder.BuildJson("{\n  \"a\": 1,\n  \"b\" 2\n}");

		Assert.IsFalse(result.IsValid);
		Assert.AreEqual(3, result.ErrorLine);
		Assert.AreEqual(7, result.ErrorColumn);
		Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
	}

	[TestMethod]
	public void Json_StopsAtNodeLimit()
	{
		var json = "[" + string.Join(",", Enumerable.Range(0, 100)) + "]";

		var result = StructuredTreeBuilder.BuildJson(json, maxNodes: 10);

		Assert.IsTrue(result.Truncated);
		Assert.AreEqual(10, result.NodeCount);
		Assert.AreEqual(9, result.Root!.Children.Count);
	}

	[TestMethod]
	public void Json_Cancelled_Throws()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		Assert.ThrowsExactly<OperationCanceledException>(() => StructuredTreeBuilder.BuildJson("[1,2,3]", cancellationToken: cts.Token));
	}

	[TestMethod]
	public void Xml_BuildsElementsAttributesAndText()
	{
		var result = StructuredTreeBuilder.BuildXml("<?xml version=\"1.0\"?><root a=\"1\"><item>hello</item><empty/><!-- c --></root>");

		Assert.IsTrue(result.IsValid);
		var root = result.Root!;
		Assert.AreEqual("root", root.Label);
		CollectionAssert.AreEqual(new[] { "@a", "item", "empty" }, root.Children.Select(c => c.Label).ToArray());
		Assert.AreEqual("1", root.Children[0].Value);
		Assert.AreEqual("hello", root.Children[1].Children.Single().Value);
		Assert.AreEqual(0, root.Children[2].Children.Count);
	}

	[TestMethod]
	public void Xml_Invalid_ReportsLineAndColumn()
	{
		var result = StructuredTreeBuilder.BuildXml("<root>\n  <a>\n</root>");

		Assert.IsFalse(result.IsValid);
		Assert.AreEqual(3, result.ErrorLine);
		Assert.IsTrue(result.ErrorColumn > 0);
	}

	[TestMethod]
	public void Xml_DtdIsRejected()
	{
		var result = StructuredTreeBuilder.BuildXml("<!DOCTYPE r [<!ENTITY e \"x\">]><r>&e;</r>");

		Assert.IsFalse(result.IsValid);
	}

	[TestMethod]
	public void Xml_StopsAtNodeLimit()
	{
		var xml = "<r>" + string.Concat(Enumerable.Repeat("<i/>", 100)) + "</r>";

		var result = StructuredTreeBuilder.BuildXml(xml, maxNodes: 10);

		Assert.IsTrue(result.Truncated);
		Assert.AreEqual(10, result.NodeCount);
	}
}
