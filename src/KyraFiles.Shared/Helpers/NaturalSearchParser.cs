// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Vocabulary tables and lookup dictionaries for natural language search (FR-M5).
	/// </summary>
	internal static class NaturalSearchVocabulary
	{
		/// <summary>
		/// Predefined groups of file extensions for common categories (PT and EN).
		/// </summary>
		public static readonly IReadOnlyDictionary<string, string[]> ExtensionGroups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
		{
			["fotos"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["foto"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["photos"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["photo"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["imagens"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["imagem"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["images"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["image"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["pictures"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },
			["picture"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" },

			["planilhas"] = new[] { ".xlsx", ".xls", ".csv", ".ods" },
			["planilha"] = new[] { ".xlsx", ".xls", ".csv", ".ods" },
			["spreadsheets"] = new[] { ".xlsx", ".xls", ".csv", ".ods" },
			["spreadsheet"] = new[] { ".xlsx", ".xls", ".csv", ".ods" },
			["excel"] = new[] { ".xlsx", ".xls", ".csv", ".ods" },

			["pdf"] = new[] { ".pdf" },
			["pdfs"] = new[] { ".pdf" },

			["vídeos"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" },
			["videos"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" },
			["vídeo"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" },
			["video"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm" },

			["músicas"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["musicas"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["música"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["musica"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["áudios"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["audios"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["áudio"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["audio"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["music"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },
			["songs"] = new[] { ".mp3", ".wav", ".flac", ".m4a", ".ogg", ".wma" },

			["documentos"] = new[] { ".docx", ".doc", ".pdf", ".txt", ".odt", ".rtf" },
			["documento"] = new[] { ".docx", ".doc", ".pdf", ".txt", ".odt", ".rtf" },
			["documents"] = new[] { ".docx", ".doc", ".pdf", ".txt", ".odt", ".rtf" },
			["document"] = new[] { ".docx", ".doc", ".pdf", ".txt", ".odt", ".rtf" },
			["docs"] = new[] { ".docx", ".doc", ".pdf", ".txt", ".odt", ".rtf" },

			["compactados"] = new[] { ".zip", ".7z", ".rar", ".tar", ".gz" },
			["compactado"] = new[] { ".zip", ".7z", ".rar", ".tar", ".gz" },
			["archives"] = new[] { ".zip", ".7z", ".rar", ".tar", ".gz" },
			["archive"] = new[] { ".zip", ".7z", ".rar", ".tar", ".gz" },
		};

		/// <summary>
		/// Table of well-known extensions to recognize when specified with or without leading dot.
		/// </summary>
		public static readonly HashSet<string> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
		{
			"ini", "txt", "log", "pdf", "doc", "docx", "xls", "xlsx", "csv", "ods", "odt", "rtf",
			"ppt", "pptx", "zip", "rar", "7z", "tar", "gz", "bz2", "xz",
			"mp3", "wav", "flac", "m4a", "ogg", "wma", "aac",
			"mp4", "mkv", "avi", "mov", "wmv", "webm", "m4v",
			"jpg", "jpeg", "png", "gif", "bmp", "webp", "tiff", "tif", "svg", "ico",
			"json", "xml", "yaml", "yml", "cs", "cpp", "c", "h", "js", "ts", "py", "html", "htm", "css", "md",
			"exe", "dll", "msi", "ps1", "bat", "cmd", "sh", "sql", "iso", "vhd", "vhdx"
		};

		/// <summary>
		/// Ambiguous date qualifiers that do not specify a concrete interval and must not invent a filter.
		/// </summary>
		public static readonly HashSet<string> AmbiguousTerms = new(StringComparer.OrdinalIgnoreCase)
		{
			"recentes", "recente", "recent"
		};

		/// <summary>
		/// Words indicating modification date qualification (PT and EN).
		/// </summary>
		public static readonly HashSet<string> ModificationQualifiers = new(StringComparer.OrdinalIgnoreCase)
		{
			"salvo", "salvos", "salva", "salvas",
			"alterado", "alterados", "alterada", "alteradas",
			"modificado", "modificados", "modificada", "modificadas",
			"saved", "modified", "changed", "updated"
		};

		/// <summary>
		/// Words indicating creation date qualification (PT and EN).
		/// </summary>
		public static readonly HashSet<string> CreationQualifiers = new(StringComparer.OrdinalIgnoreCase)
		{
			"criado", "criados", "criada", "criadas",
			"created"
		};

		/// <summary>
		/// Size unit scale multipliers in bytes.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, long> SizeUnits = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
		{
			["b"] = 1L,
			["bytes"] = 1L,
			["byte"] = 1L,
			["kb"] = 1024L,
			["ko"] = 1024L,
			["kilobytes"] = 1024L,
			["kilobyte"] = 1024L,
			["mb"] = 1024L * 1024L,
			["mo"] = 1024L * 1024L,
			["megabytes"] = 1024L * 1024L,
			["megabyte"] = 1024L * 1024L,
			["gb"] = 1024L * 1024L * 1024L,
			["go"] = 1024L * 1024L * 1024L,
			["gigabytes"] = 1024L * 1024L * 1024L,
			["gigabyte"] = 1024L * 1024L * 1024L,
			["tb"] = 1024L * 1024L * 1024L * 1024L,
			["to"] = 1024L * 1024L * 1024L * 1024L,
			["terabytes"] = 1024L * 1024L * 1024L * 1024L,
			["terabyte"] = 1024L * 1024L * 1024L * 1024L
		};

		/// <summary>
		/// Common stop words in PT and EN that are discarded when resolving remaining search text.
		/// </summary>
		public static readonly HashSet<string> NoiseWords = new(StringComparer.OrdinalIgnoreCase)
		{
			"todos", "todas", "todo", "toda", "os", "as", "o", "a",
			"um", "uma", "uns", "umas",
			"de", "do", "da", "dos", "das",
			"em", "no", "na", "nos", "nas",
			"com", "que", "para", "por",
			"arquivos", "arquivo", "ficheiros", "ficheiro", "itens", "item",
			"all", "the", "a", "an", "of", "in", "on", "at", "with", "that", "for", "by",
			"files", "file", "items", "item", "and", "e", "or", "ou"
		};
	}

	/// <summary>
	/// Pure offline natural language query parser for file search (FR-M5).
	/// Converts plain PT/EN phrases into structured <see cref="SearchIntent"/> filters.
	/// </summary>
	public static class NaturalSearchParser
	{
		private static readonly Regex SizeBetweenRegex = new(
			@"\b(?:entre|between|de|from)\s+(?<val1>[\d.,]+)\s*(?<unit1>[a-zA-Z]+)?\s+(?:e|and|a|at[ée]|to)\s+(?<val2>[\d.,]+)\s*(?<unit2>[a-zA-Z]+)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex SizeGreaterRegex = new(
			@"(?:\b(?:maior(?:es)?\s+que|acima\s+de|mais\s+de|superior(?:es)?\s+a|larger\s+than|greater\s+than|bigger\s+than|more\s+than|above|over)\s*|>\s*)(?<val>[\d.,]+)\s*(?<unit>[a-zA-Z]+)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex SizeLessRegex = new(
			@"(?:\b(?:menor(?:es)?\s+que|abaixo\s+de|menos\s+de|inferior(?:es)?\s+a|smaller\s+than|less\s+than|under|below)\s*|<\s*)(?<val>[\d.,]+)\s*(?<unit>[a-zA-Z]+)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex RelativeSpanRegex = new(
			@"\b(?:(?:nos|nas|dos|das|in\s+the|during\s+the)\s+)?(?:[úu]ltimos?|[úu]ltimas?|last|past)\s+(?<num>\d+)\s+(?<unit>dias?|semanas?|meses|m[êe]s|anos?|days?|weeks?|months?|years?)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex DotExtensionRegex = new(
			@"(?<=(?:^|\s))\.(?<ext>[a-zA-Z0-9]{1,10})(?=(?:$|\s|[,;]))",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex ExplicitExtensionKeywordRegex = new(
			@"\b(?:extens[aã]o|extens[oõ]es|extension|extensions|ext|tipo|formato)\s+(?:de\s+|com\s+|em\s+)?(?:\.)?(?<ext>[a-zA-Z0-9]{1,10})\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex FileExtensionPrefixOrSuffixRegex = new(
			@"\b(?:(?:arquivos?|ficheiros?)\s+(?:de\s+)?(?:\.)?(?<ext1>[a-zA-Z0-9]{1,10})|(?:\.)?(?<ext2>[a-zA-Z0-9]{1,10})\s+(?:files?|arquivos?|ficheiros?)|todos\s+os\s+(?:arquivos\s+)?(?:\.)?(?<ext3>[a-zA-Z0-9]{1,10})|all\s+(?:the\s+)?(?:\.)?(?<ext4>[a-zA-Z0-9]{1,10})\s+files?)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex MultiExtensionPairRegex = new(
			@"\b(?:\.)?(?<ext1>[a-zA-Z0-9]{1,10})\s+(?:e|and|ou|or|,)\s+(?:\.)?(?<ext2>[a-zA-Z0-9]{1,10})\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Parses a natural language query into a structured <see cref="SearchIntent"/>.
		/// </summary>
		/// <param name="query">The raw user query string.</param>
		/// <param name="referenceDate">The injected reference date/time (never reads real system clock).</param>
		/// <returns>A populated <see cref="SearchIntent"/> instance.</returns>
		public static SearchIntent Parse(string? query, DateTimeOffset referenceDate)
		{
			if (string.IsNullOrWhiteSpace(query))
				return SearchIntent.Empty;

			string working = query.Trim();
			var unrecognizedList = new List<string>();
			var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			// 1. Size Extraction
			SizeRange? sizeRange = ExtractSize(ref working);

			// 2. Ambiguous terms check (e.g. "recentes", "recent" without number)
			CheckAmbiguousTerms(ref working, unrecognizedList);

			// 3. Date Extraction (Modification vs Creation)
			(DateRange? dateModified, DateRange? dateCreated) = ExtractDates(ref working, referenceDate);

			// 4. Extension Groups & Specific Extensions Extraction
			ExtractExtensions(ref working, extensions);

			// 5. Cleanup qualifiers and noise words from remaining text
			string? searchText = ExtractRemainingText(working, unrecognizedList);

			bool hasFilters = extensions.Count > 0 ||
			                  (dateModified is not null && dateModified.HasValue) ||
			                  (dateCreated is not null && dateCreated.HasValue) ||
			                  (sizeRange is not null && sizeRange.HasValue);

			// Rule: "Frase sem filtro reconhecível devolve tudo como não entendido, sem inventar filtro."
			if (!hasFilters)
			{
				// If no filter was recognized at all, treat the entire query as unrecognized
				string rawTrimmed = query.Trim();
				if (!unrecognizedList.Contains(rawTrimmed, StringComparer.OrdinalIgnoreCase))
					unrecognizedList.Insert(0, rawTrimmed);

				return new SearchIntent
				{
					SearchText = null,
					FileExtensions = Array.Empty<string>(),
					DateModifiedRange = null,
					DateCreatedRange = null,
					SizeRange = null,
					UnrecognizedTokens = unrecognizedList.ToArray()
				};
			}

			return new SearchIntent
			{
				SearchText = searchText,
				FileExtensions = extensions.OrderBy(x => x).ToArray(),
				DateModifiedRange = dateModified,
				DateCreatedRange = dateCreated,
				SizeRange = sizeRange,
				UnrecognizedTokens = unrecognizedList.ToArray()
			};
		}

		private static SizeRange? ExtractSize(ref string text)
		{
			// Check Between / Entre
			var mBetween = SizeBetweenRegex.Match(text);
			if (mBetween.Success)
			{
				string val1Str = mBetween.Groups["val1"].Value.Replace(',', '.');
				string val2Str = mBetween.Groups["val2"].Value.Replace(',', '.');
				string unit2Str = mBetween.Groups["unit2"].Value;
				string unit1Str = mBetween.Groups["unit1"].Success ? mBetween.Groups["unit1"].Value : unit2Str;

				if (double.TryParse(val1Str, NumberStyles.Float, CultureInfo.InvariantCulture, out double val1) &&
				    double.TryParse(val2Str, NumberStyles.Float, CultureInfo.InvariantCulture, out double val2) &&
				    NaturalSearchVocabulary.SizeUnits.TryGetValue(unit1Str, out long mult1) &&
				    NaturalSearchVocabulary.SizeUnits.TryGetValue(unit2Str, out long mult2))
				{
					long minBytes = (long)(val1 * mult1);
					long maxBytes = (long)(val2 * mult2);
					if (minBytes > maxBytes)
						(minBytes, maxBytes) = (maxBytes, minBytes);

					text = text.Remove(mBetween.Index, mBetween.Length);
					return new SizeRange(minBytes, maxBytes);
				}
			}

			// Check Greater
			var mGreater = SizeGreaterRegex.Match(text);
			if (mGreater.Success)
			{
				string valStr = mGreater.Groups["val"].Value.Replace(',', '.');
				string unitStr = mGreater.Groups["unit"].Value;

				if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val) &&
				    NaturalSearchVocabulary.SizeUnits.TryGetValue(unitStr, out long mult))
				{
					text = text.Remove(mGreater.Index, mGreater.Length);
					return new SizeRange((long)(val * mult), null);
				}
			}

			// Check Less
			var mLess = SizeLessRegex.Match(text);
			if (mLess.Success)
			{
				string valStr = mLess.Groups["val"].Value.Replace(',', '.');
				string unitStr = mLess.Groups["unit"].Value;

				if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val) &&
				    NaturalSearchVocabulary.SizeUnits.TryGetValue(unitStr, out long mult))
				{
					text = text.Remove(mLess.Index, mLess.Length);
					return new SizeRange(null, (long)(val * mult));
				}
			}

			return null;
		}

		private static void CheckAmbiguousTerms(ref string text, List<string> unrecognizedList)
		{
			foreach (var term in NaturalSearchVocabulary.AmbiguousTerms)
			{
				var regex = new Regex($@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase);
				if (regex.IsMatch(text))
				{
					if (!unrecognizedList.Contains(term, StringComparer.OrdinalIgnoreCase))
						unrecognizedList.Add(term);

					text = regex.Replace(text, " ");
				}
			}
		}

		private static (DateRange? Modified, DateRange? Created) ExtractDates(ref string text, DateTimeOffset referenceDate)
		{
			DateRange? modified = null;
			DateRange? created = null;

			// Date boundary helpers
			DateTimeOffset todayStart = new(referenceDate.Year, referenceDate.Month, referenceDate.Day, 0, 0, 0, referenceDate.Offset);
			DateTimeOffset todayEnd = todayStart.AddDays(1).AddTicks(-1);

			DateTimeOffset yesterdayStart = todayStart.AddDays(-1);
			DateTimeOffset yesterdayEnd = todayStart.AddTicks(-1);

			DateTimeOffset thisMonthStart = new(referenceDate.Year, referenceDate.Month, 1, 0, 0, 0, referenceDate.Offset);
			DateTimeOffset thisMonthEnd = thisMonthStart.AddMonths(1).AddTicks(-1);

			DateTimeOffset lastMonthStart = thisMonthStart.AddMonths(-1);
			DateTimeOffset lastMonthEnd = thisMonthStart.AddTicks(-1);

			DateTimeOffset thisYearStart = new(referenceDate.Year, 1, 1, 0, 0, 0, referenceDate.Offset);
			DateTimeOffset thisYearEnd = thisYearStart.AddYears(1).AddTicks(-1);

			DateTimeOffset lastYearStart = thisYearStart.AddYears(-1);
			DateTimeOffset lastYearEnd = thisYearStart.AddTicks(-1);

			// Match static patterns first
			var staticDatePatterns = new (string Pattern, DateRange Range)[]
			{
				// Semana passada (Q8: semana passada = últimos 7 dias)
				(@"\b(?:(?:na|da)\s+)?semana\s+passada\b|\b(?:last|past)\s+week\b", new DateRange(referenceDate.AddDays(-7), referenceDate)),
				// Hoje / Today
				(@"\bhoje\b|\btoday\b", new DateRange(todayStart, todayEnd)),
				// Ontem / Yesterday
				(@"\bontem\b|\byesterday\b", new DateRange(yesterdayStart, yesterdayEnd)),
				// Este mês / This month
				(@"\b(?:(?:neste|desse|deste|este|esse)\s+m[êe]s|this\s+month)\b", new DateRange(thisMonthStart, thisMonthEnd)),
				// Mês passado / Last month
				(@"\b(?:(?:no|do)\s+)?m[êe]s\s+passado\b|\b(?:last|past)\s+month\b", new DateRange(lastMonthStart, lastMonthEnd)),
				// Este ano / This year
				(@"\b(?:(?:neste|deste|este|esse)\s+ano|this\s+year)\b", new DateRange(thisYearStart, thisYearEnd)),
				// Ano passado / Last year
				(@"\b(?:(?:no|do)\s+)?ano\s+passado\b|\b(?:last|past)\s+year\b", new DateRange(lastYearStart, lastYearEnd)),
			};

			foreach (var (pattern, range) in staticDatePatterns)
			{
				var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					bool isCreation = DetectTargetNearIndex(text, match.Index, match.Length);
					if (isCreation)
						created = range;
					else
						modified = range;

					text = text.Remove(match.Index, match.Length);
				}
			}

			// Match relative spans (últimos N dias/semanas/meses/anos)
			var mSpan = RelativeSpanRegex.Match(text);
			if (mSpan.Success)
			{
				int num = int.Parse(mSpan.Groups["num"].Value, CultureInfo.InvariantCulture);
				string unit = mSpan.Groups["unit"].Value.ToLowerInvariant();

				DateRange? range = null;
				if (unit.StartsWith("dia") || unit.StartsWith("day"))
					range = new DateRange(referenceDate.AddDays(-num), referenceDate);
				else if (unit.StartsWith("semana") || unit.StartsWith("week"))
					range = new DateRange(referenceDate.AddDays(-7 * num), referenceDate);
				else if (unit.StartsWith("m") || unit.StartsWith("month"))
					range = new DateRange(referenceDate.AddMonths(-num), referenceDate);
				else if (unit.StartsWith("ano") || unit.StartsWith("year"))
					range = new DateRange(referenceDate.AddYears(-num), referenceDate);

				if (range is not null)
				{
					bool isCreation = DetectTargetNearIndex(text, mSpan.Index, mSpan.Length);
					if (isCreation)
						created = range;
					else
						modified = range;

					text = text.Remove(mSpan.Index, mSpan.Length);
				}
			}

			// Remove date qualifiers that have now been processed
			foreach (var q in NaturalSearchVocabulary.ModificationQualifiers.Concat(NaturalSearchVocabulary.CreationQualifiers))
			{
				text = Regex.Replace(text, $@"\b{Regex.Escape(q)}\b", " ", RegexOptions.IgnoreCase);
			}

			return (modified, created);
		}

		private static bool DetectTargetNearIndex(string text, int matchIndex, int matchLength)
		{
			// Check nearby text before or after for creation words
			int start = Math.Max(0, matchIndex - 30);
			int length = Math.Min(text.Length - start, matchLength + 60);
			string snippet = text.Substring(start, length);

			foreach (var c in NaturalSearchVocabulary.CreationQualifiers)
			{
				if (Regex.IsMatch(snippet, $@"\b{Regex.Escape(c)}\b", RegexOptions.IgnoreCase))
					return true;
			}

			return false;
		}

		private static void ExtractExtensions(ref string text, HashSet<string> extensions)
		{
			// 0. Combined descriptors like "documentos pdf" / "pdf documents"
			if (Regex.IsMatch(text, @"\b(?:documentos?|documents?)\s+pdfs?\b|\bpdfs?\s+(?:documentos?|documents?)\b", RegexOptions.IgnoreCase))
			{
				extensions.Add(".pdf");
				text = Regex.Replace(text, @"\b(?:documentos?|documents?)\s+pdfs?\b|\bpdfs?\s+(?:documentos?|documents?)\b", " ", RegexOptions.IgnoreCase);
			}

			// 1. Extension groups (fotos, planilhas, pdf, etc.)
			foreach (var (groupName, extList) in NaturalSearchVocabulary.ExtensionGroups)
			{
				var regex = new Regex($@"\b{Regex.Escape(groupName)}\b", RegexOptions.IgnoreCase);
				if (regex.IsMatch(text))
				{
					foreach (var ext in extList)
						extensions.Add(ext);

					text = regex.Replace(text, " ");
				}
			}

			// 2. Multi-extension pair like "txt e log" / "txt and log"
			var mMulti = MultiExtensionPairRegex.Match(text);
			if (mMulti.Success)
			{
				string ext1 = mMulti.Groups["ext1"].Value.TrimStart('.');
				string ext2 = mMulti.Groups["ext2"].Value.TrimStart('.');

				if (NaturalSearchVocabulary.KnownExtensions.Contains(ext1) &&
				    NaturalSearchVocabulary.KnownExtensions.Contains(ext2))
				{
					extensions.Add("." + ext1.ToLowerInvariant());
					extensions.Add("." + ext2.ToLowerInvariant());
					text = text.Remove(mMulti.Index, mMulti.Length);
				}
			}

			// 3. Explicit extension keyword (e.g. "extensão ini", "extension .ini")
			var mExplicit = ExplicitExtensionKeywordRegex.Match(text);
			while (mExplicit.Success)
			{
				string ext = mExplicit.Groups["ext"].Value.TrimStart('.');
				extensions.Add("." + ext.ToLowerInvariant());
				text = text.Remove(mExplicit.Index, mExplicit.Length);
				mExplicit = ExplicitExtensionKeywordRegex.Match(text);
			}

			// 4. Dot extensions (e.g. ".ini", ".log")
			var mDot = DotExtensionRegex.Match(text);
			while (mDot.Success)
			{
				string ext = mDot.Groups["ext"].Value.TrimStart('.');
				extensions.Add("." + ext.ToLowerInvariant());
				text = text.Remove(mDot.Index, mDot.Length);
				mDot = DotExtensionRegex.Match(text);
			}

			// 5. File extension prefix/suffix (e.g. "arquivos ini", "ini files", "todos os ini")
			var mPrefixSuffix = FileExtensionPrefixOrSuffixRegex.Match(text);
			while (mPrefixSuffix.Success)
			{
				string candidate = mPrefixSuffix.Groups["ext1"].Value;
				if (string.IsNullOrEmpty(candidate)) candidate = mPrefixSuffix.Groups["ext2"].Value;
				if (string.IsNullOrEmpty(candidate)) candidate = mPrefixSuffix.Groups["ext3"].Value;
				if (string.IsNullOrEmpty(candidate)) candidate = mPrefixSuffix.Groups["ext4"].Value;

				candidate = candidate.TrimStart('.');
				if (NaturalSearchVocabulary.KnownExtensions.Contains(candidate))
				{
					extensions.Add("." + candidate.ToLowerInvariant());
					text = text.Remove(mPrefixSuffix.Index, mPrefixSuffix.Length);
				}
				else
				{
					break;
				}

				mPrefixSuffix = FileExtensionPrefixOrSuffixRegex.Match(text);
			}
		}

		private static string? ExtractRemainingText(string text, List<string> unrecognizedList)
		{
			// Split remaining text into tokens
			var tokens = text.Split(new[] { ' ', '\t', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
			var remainingTokens = new List<string>();

			foreach (var token in tokens)
			{
				string clean = token.Trim();
				if (string.IsNullOrEmpty(clean))
					continue;

				// Skip noise words
				if (NaturalSearchVocabulary.NoiseWords.Contains(clean))
					continue;

				// Skip qualifiers if any remained
				if (NaturalSearchVocabulary.ModificationQualifiers.Contains(clean) ||
				    NaturalSearchVocabulary.CreationQualifiers.Contains(clean))
					continue;

				remainingTokens.Add(clean);
			}

			if (remainingTokens.Count == 0)
				return null;

			return string.Join(" ", remainingTokens);
		}
	}
}
