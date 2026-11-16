// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Data.Models;
using KyraFiles.App.Utils.Shell;
using Microsoft.UI.Xaml;

namespace KyraFiles.App.Services
{
	/// <summary>
	/// Provides metadata and formatted values for Windows Explorer Property System columns.
	/// </summary>
	public sealed class ExplorerPropertyService : IExplorerPropertyService
	{
		// Files Custom: names and categories come from the app resources, in the app language (G1 C2).
		// Resource keys: "ExplorerColumn" + canonical name without "System." and dots; "ExplorerColumnCategory" + category.
		private static readonly (string CanonicalName, string Category, double Width, TextAlignment Alignment)[] Entries =
		[
			("System.Image.Dimensions", "Image", 140, TextAlignment.Left),
			("System.Photo.DateTaken", "Image", 150, TextAlignment.Left),
			("System.Photo.CameraModel", "Image", 150, TextAlignment.Left),
			("System.Photo.CameraManufacturer", "Image", 150, TextAlignment.Left),
			("System.Photo.FNumber", "Image", 100, TextAlignment.Right),
			("System.Photo.ISOSpeed", "Image", 100, TextAlignment.Right),
			("System.Image.HorizontalSize", "Image", 100, TextAlignment.Right),
			("System.Image.VerticalSize", "Image", 100, TextAlignment.Right),

			("System.Media.Duration", "Media", 100, TextAlignment.Right),
			("System.Music.Artist", "Media", 180, TextAlignment.Left),
			("System.Music.AlbumTitle", "Media", 180, TextAlignment.Left),
			("System.Music.AlbumArtist", "Media", 180, TextAlignment.Left),
			("System.Music.TrackNumber", "Media", 80, TextAlignment.Right),
			("System.Music.Genre", "Media", 120, TextAlignment.Left),
			("System.Media.Year", "Media", 80, TextAlignment.Right),
			("System.Audio.EncodingBitrate", "Media", 100, TextAlignment.Right),
			("System.Audio.SampleRate", "Media", 120, TextAlignment.Right),

			("System.Video.FrameWidth", "Video", 120, TextAlignment.Right),
			("System.Video.FrameHeight", "Video", 120, TextAlignment.Right),
			("System.Video.FrameRate", "Video", 120, TextAlignment.Right),
			("System.Video.TotalBitrate", "Video", 120, TextAlignment.Right),
			("System.Media.Director", "Video", 160, TextAlignment.Left),
			("System.Media.Producer", "Video", 160, TextAlignment.Left),

			("System.Author", "Document", 160, TextAlignment.Left),
			("System.Title", "Document", 180, TextAlignment.Left),
			("System.Subject", "Document", 160, TextAlignment.Left),
			("System.Keywords", "Document", 140, TextAlignment.Left),
			("System.Comment", "Document", 180, TextAlignment.Left),
			("System.Document.PageCount", "Document", 80, TextAlignment.Right),
			("System.Document.WordCount", "Document", 120, TextAlignment.Right),
			("System.Document.CharacterCount", "Document", 120, TextAlignment.Right),

			("System.FileVersion", "File", 120, TextAlignment.Left),
			("System.Rating", "File", 100, TextAlignment.Left),
			("System.Copyright", "File", 180, TextAlignment.Left),
			("System.ApplicationName", "File", 160, TextAlignment.Left),
			("System.Company", "File", 160, TextAlignment.Left),
			("System.ComputerName", "File", 120, TextAlignment.Left),
			("System.FileOwner", "File", 160, TextAlignment.Left),
			("System.FileAttributes", "File", 100, TextAlignment.Left),

			// Files Custom: F053
			("FilesCustom.Text.Encoding", "Document", 110, TextAlignment.Left),
			("FilesCustom.Text.LineEnding", "Document", 90, TextAlignment.Left),
		];

		private static readonly Dictionary<string, string> CategoryByName =
			Entries.ToDictionary(e => e.CanonicalName, e => e.Category, StringComparer.OrdinalIgnoreCase);

		private static readonly List<DynamicColumnDefinition> Catalog =
			Entries.Select(e => new DynamicColumnDefinition
			{
				CanonicalName = e.CanonicalName,
				UserLength = new GridLength(e.Width),
				TextAlignment = e.Alignment,
			}).ToList();

		/// <summary>
		/// Gets the column name in the app language, falling back to the last part of the canonical name.
		/// </summary>
		public static string GetDisplayName(string canonicalName)
		{
			if (string.IsNullOrEmpty(canonicalName))
				return string.Empty;

			if (canonicalName == "FilesCustom.Text.Encoding")
				return "Codificação";
			if (canonicalName == "FilesCustom.Text.LineEnding")
				return "Fim de linha";

			var key = "ExplorerColumn" + canonicalName.Replace("System.", string.Empty, StringComparison.Ordinal).Replace(".", string.Empty, StringComparison.Ordinal);
			var name = key.GetLocalizedResource();
			return string.IsNullOrEmpty(name) ? canonicalName[(canonicalName.LastIndexOf('.') + 1)..] : name;
		}

		/// <summary>
		/// Gets the column category in the app language.
		/// </summary>
		public static string GetCategory(string canonicalName)
			=> CategoryByName.TryGetValue(canonicalName, out var category)
				? ("ExplorerColumnCategory" + category).GetLocalizedResource()
				: string.Empty;

		/// <inheritdoc/>
		public IReadOnlyList<DynamicColumnDefinition> GetAllAvailableProperties()
		{
			return Catalog;
		}

		/// <inheritdoc/>
		public string? GetFormattedProperty(string filePath, string canonicalName)
		{
			if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(canonicalName))
				return null;

			if (canonicalName == "FilesCustom.Text.Encoding")
			{
				var info = KyraFiles.Shared.Helpers.TextEncodingHelper.GetFileInfo(filePath);
				return info.IsText ? info.Encoding : string.Empty;
			}
			if (canonicalName == "FilesCustom.Text.LineEnding")
			{
				var info = KyraFiles.Shared.Helpers.TextEncodingHelper.GetFileInfo(filePath);
				return info.IsText ? info.LineEnding : string.Empty;
			}

			try
			{
				using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(filePath);
				return shellItem?.Properties?.GetPropertyString(canonicalName);
			}
			catch
			{
				return null;
			}
		}
	}
}
