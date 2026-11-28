// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace KyraFiles.App.ViewModels.Previews
{
	internal enum StructuredNodeKind
	{
		Object,
		Array,
		Element,
		Attribute,
		Text,
		String,
		Number,
		Boolean,
		Null,
	}

	internal sealed class StructuredNode(string label, StructuredNodeKind kind, string? value = null)
	{
		public string Label { get; } = label;

		public StructuredNodeKind Kind { get; } = kind;

		public string? Value { get; } = value;

		public List<StructuredNode> Children { get; } = [];
	}

	/// <summary>
	/// Result of parsing a document into a tree. When parsing fails, <see cref="Root"/> is null and the
	/// 1-based error position is set, so the preview can fall back to text with the error highlighted.
	/// </summary>
	internal sealed record StructuredTreeResult(
		StructuredNode? Root,
		int NodeCount,
		bool Truncated,
		int? ErrorLine = null,
		int? ErrorColumn = null,
		string? ErrorMessage = null)
	{
		public bool IsValid => Root is not null;
	}

	/// <summary>
	/// Builds a bounded tree for JSON and XML previews (F054 T2). Parsing stops at <c>maxNodes</c>
	/// so a large document never becomes a large tree, and honors cancellation between nodes.
	/// </summary>
	internal static class StructuredTreeBuilder
	{
		public const int DefaultMaxNodes = 5000;

		public static StructuredTreeResult BuildJson(string text, int maxNodes = DefaultMaxNodes, CancellationToken cancellationToken = default)
		{
			var bytes = Encoding.UTF8.GetBytes(text);
			var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
			{
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip,
			});

			var stack = new Stack<StructuredNode>();
			StructuredNode? root = null;
			string? pendingName = null;
			var count = 0;

			try
			{
				while (reader.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (reader.TokenType == JsonTokenType.PropertyName)
					{
						pendingName = reader.GetString();
						continue;
					}

					if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
					{
						stack.Pop();
						continue;
					}

					if (count >= maxNodes)
						return new(root, count, Truncated: true);

					var parent = stack.Count > 0 ? stack.Peek() : null;
					var label = pendingName ?? (parent?.Kind == StructuredNodeKind.Array ? $"[{parent.Children.Count}]" : "$");
					pendingName = null;

					var node = reader.TokenType switch
					{
						JsonTokenType.StartObject => new StructuredNode(label, StructuredNodeKind.Object),
						JsonTokenType.StartArray => new StructuredNode(label, StructuredNodeKind.Array),
						JsonTokenType.String => new StructuredNode(label, StructuredNodeKind.String, reader.GetString()),
						JsonTokenType.Number => new StructuredNode(label, StructuredNodeKind.Number, Encoding.UTF8.GetString(reader.ValueSpan)),
						JsonTokenType.True => new StructuredNode(label, StructuredNodeKind.Boolean, "true"),
						JsonTokenType.False => new StructuredNode(label, StructuredNodeKind.Boolean, "false"),
						_ => new StructuredNode(label, StructuredNodeKind.Null, "null"),
					};

					count++;
					if (parent is null)
						root = node;
					else
						parent.Children.Add(node);

					if (node.Kind is StructuredNodeKind.Object or StructuredNodeKind.Array)
						stack.Push(node);
				}

				if (root is null)
					return new(null, 0, false, 1, 1, "Empty document");

				return new(root, count, Truncated: false);
			}
			catch (JsonException ex)
			{
				return new(null, count, false, (int)(ex.LineNumber ?? 0) + 1, (int)(ex.BytePositionInLine ?? 0) + 1, ex.Message);
			}
		}

		public static StructuredTreeResult BuildXml(string text, int maxNodes = DefaultMaxNodes, CancellationToken cancellationToken = default)
		{
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
			};

			var stack = new Stack<StructuredNode>();
			StructuredNode? root = null;
			var count = 0;

			try
			{
				using var reader = XmlReader.Create(new StringReader(text), settings);
				while (reader.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();

					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							if (count >= maxNodes)
								return new(root, count, Truncated: true);

							var element = new StructuredNode(reader.Name, StructuredNodeKind.Element);
							count++;
							if (stack.Count == 0)
								root = element;
							else
								stack.Peek().Children.Add(element);

							if (reader.HasAttributes)
							{
								while (reader.MoveToNextAttribute() && count < maxNodes)
								{
									element.Children.Add(new StructuredNode("@" + reader.Name, StructuredNodeKind.Attribute, reader.Value));
									count++;
								}

								reader.MoveToElement();
							}

							if (!reader.IsEmptyElement)
								stack.Push(element);
							break;

						case XmlNodeType.EndElement:
							stack.Pop();
							break;

						case XmlNodeType.Text:
						case XmlNodeType.CDATA:
							if (stack.Count > 0 && count < maxNodes)
							{
								stack.Peek().Children.Add(new StructuredNode("#text", StructuredNodeKind.Text, reader.Value));
								count++;
							}

							break;
					}
				}

				return root is null
					? new(null, 0, false, 1, 1, "Empty document")
					: new(root, count, Truncated: false);
			}
			catch (XmlException ex)
			{
				return new(null, count, false, ex.LineNumber, ex.LinePosition, ex.Message);
			}
		}
	}
}
