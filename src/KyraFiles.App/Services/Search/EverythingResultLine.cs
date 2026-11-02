// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;

namespace Files.App.Services.Search
{
	/// <summary>
	/// Files Custom (F065): one result line of es.exe run with -tsv -no-header -attrib -size -dm,
	/// in the form &lt;path&gt;\t&lt;attributes&gt;\t&lt;size in bytes&gt;\t&lt;date modified&gt;.
	/// Pure logic (no UI types) so it is unit tested by linking this file into the test project.
	/// </summary>
	internal readonly record struct EverythingResultLine(string FullPath, string Name, bool IsFolder, bool IsHidden, long SizeBytes, DateTime? DateModified, string Extension)
	{
		public static bool TryParse(string? line, out EverythingResultLine result)
		{
			result = default;
			if (string.IsNullOrWhiteSpace(line))
				return false;

			var parts = line.Split('\t');
			string path = parts[0].Trim().Trim('"');
			if (string.IsNullOrEmpty(path))
				return false;

			string attrStr = parts.Length > 1 ? parts[1].Trim() : string.Empty;
			string sizeStr = parts.Length > 2 ? parts[2].Trim() : string.Empty;
			string dateStr = parts.Length > 3 ? parts[3].Trim() : string.Empty;

			bool isFolder = path.EndsWith('\\') || path.EndsWith('/');
			bool isHidden = false;
			if (int.TryParse(attrStr, out int attrVal))
			{
				if (!isFolder)
					isFolder = (attrVal & (int)FileAttributes.Directory) != 0;

				isHidden = (attrVal & (int)FileAttributes.Hidden) != 0;
			}

			string cleanPath = isFolder ? path.TrimEnd('\\', '/') : path;
			string name = Path.GetFileName(cleanPath);
			if (string.IsNullOrEmpty(name))
				name = cleanPath;

			long size = 0;
			if (!isFolder && long.TryParse(sizeStr, out long parsedSize))
				size = parsedSize;

			DateTime? dateModified = DateTime.TryParse(dateStr, out var parsedDate) ? parsedDate : null;
			string extension = isFolder ? string.Empty : Path.GetExtension(name);

			result = new(cleanPath, name, isFolder, isHidden, size, dateModified, extension);
			return true;
		}
	}
}
