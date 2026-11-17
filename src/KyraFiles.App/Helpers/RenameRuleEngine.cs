// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KyraFiles.App.Helpers
{
	public enum CaseTransformType
	{
		None,
		Uppercase,
		Lowercase,
		TitleCase,
		CapitalizeFirst
	}

	public enum InsertPosition
	{
		Prefix,
		Suffix,
		Replace
	}

	public enum DateSourceType
	{
		ModifiedDate,
		CreatedDate,
		CurrentDate
	}

	public interface IRenameRule
	{
		string Apply(string namePart, string extensionPart, int itemIndex, RenameItem item);
	}

	public sealed class FindReplaceRule : IRenameRule
	{
		private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

		public string Find { get; set; } = string.Empty;
		public string Replace { get; set; } = string.Empty;
		public bool UseRegex { get; set; }
		public bool MatchCase { get; set; }
		public bool ApplyToExtension { get; set; }

		public string Apply(string namePart, string extensionPart, int itemIndex, RenameItem item)
		{
			if (string.IsNullOrEmpty(Find))
				return namePart;

			string target = ApplyToExtension ? $"{namePart}{extensionPart}" : namePart;

			try
			{
				if (UseRegex)
				{
					var options = RegexOptions.CultureInvariant;
					if (!MatchCase)
						options |= RegexOptions.IgnoreCase;

					var regex = new Regex(Find, options, RegexTimeout);
					return regex.Replace(target, Replace ?? string.Empty);
				}
				else
				{
					var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
					return ReplaceString(target, Find, Replace ?? string.Empty, comparison);
				}
			}
			catch (ArgumentException)
			{
				// Regex inválida: retorna target sem modificação
				return target;
			}
			catch (RegexMatchTimeoutException)
			{
				// Timeout de regex (catastrophic backtracking guard): retorna target
				return target;
			}
		}

		private static string ReplaceString(string source, string oldValue, string newValue, StringComparison comparison)
		{
			if (string.IsNullOrEmpty(oldValue))
				return source;

			var sb = new StringBuilder();
			int previousIndex = 0;
			int index = source.IndexOf(oldValue, comparison);

			while (index != -1)
			{
				sb.Append(source, previousIndex, index - previousIndex);
				sb.Append(newValue);
				previousIndex = index + oldValue.Length;
				index = source.IndexOf(oldValue, previousIndex, comparison);
			}

			sb.Append(source, previousIndex, source.Length - previousIndex);
			return sb.ToString();
		}
	}

	public sealed class NumberingRule : IRenameRule
	{
		public InsertPosition Position { get; set; } = InsertPosition.Suffix;
		public int StartIndex { get; set; } = 1;
		public int Step { get; set; } = 1;
		public int PaddingDigits { get; set; } = 3;
		public string Separator { get; set; } = "_";

		public string Apply(string namePart, string extensionPart, int itemIndex, RenameItem item)
		{
			int number = StartIndex + (itemIndex * Step);
			string formattedNumber = number.ToString(new string('0', Math.Max(1, PaddingDigits)), CultureInfo.InvariantCulture);

			return Position switch
			{
				InsertPosition.Prefix => $"{formattedNumber}{Separator}{namePart}",
				InsertPosition.Replace => formattedNumber,
				_ => $"{namePart}{Separator}{formattedNumber}",
			};
		}
	}

	public sealed class CaseTransformRule : IRenameRule
	{
		public CaseTransformType Transform { get; set; } = CaseTransformType.None;

		public string Apply(string namePart, string extensionPart, int itemIndex, RenameItem item)
		{
			if (string.IsNullOrEmpty(namePart) || Transform == CaseTransformType.None)
				return namePart;

			return Transform switch
			{
				CaseTransformType.Uppercase => namePart.ToUpperInvariant(),
				CaseTransformType.Lowercase => namePart.ToLowerInvariant(),
				CaseTransformType.TitleCase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(namePart.ToLowerInvariant()),
				CaseTransformType.CapitalizeFirst => namePart.Length > 1
					? char.ToUpperInvariant(namePart[0]) + namePart.Substring(1)
					: namePart.ToUpperInvariant(),
				_ => namePart
			};
		}
	}

	public sealed class DateRule : IRenameRule
	{
		public InsertPosition Position { get; set; } = InsertPosition.Prefix;
		public DateSourceType DateSource { get; set; } = DateSourceType.ModifiedDate;
		public string Format { get; set; } = "yyyy-MM-dd";
		public string Separator { get; set; } = "_";

		public string Apply(string namePart, string extensionPart, int itemIndex, RenameItem item)
		{
			DateTimeOffset date = DateSource switch
			{
				DateSourceType.CreatedDate => item.CreatedDate ?? item.ModifiedDate ?? DateTimeOffset.Now,
				DateSourceType.CurrentDate => DateTimeOffset.Now,
				_ => item.ModifiedDate ?? item.CreatedDate ?? DateTimeOffset.Now
			};

			string formattedDate;
			try
			{
				formattedDate = date.ToString(Format, CultureInfo.InvariantCulture);
			}
			catch (FormatException)
			{
				formattedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			}

			return Position switch
			{
				InsertPosition.Prefix => $"{formattedDate}{Separator}{namePart}",
				InsertPosition.Replace => formattedDate,
				_ => $"{namePart}{Separator}{formattedDate}"
			};
		}
	}

	public sealed class ReplaceSpacesRule : IRenameRule
	{
		public string Replacement { get; set; } = "_";

		public string Apply(string namePart, string extensionPart, int itemIndex, RenameItem item)
		{
			return namePart.Replace(" ", Replacement ?? string.Empty);
		}
	}

	public sealed class RenameItem
	{
		public string OriginalPath { get; set; } = string.Empty;
		public string OriginalFullName { get; set; } = string.Empty;
		public string DirectoryPath { get; set; } = string.Empty;
		public string NamePart { get; set; } = string.Empty;
		public string ExtensionPart { get; set; } = string.Empty;
		public DateTimeOffset? CreatedDate { get; set; }
		public DateTimeOffset? ModifiedDate { get; set; }
		public bool IsDirectory { get; set; }
		public int Index { get; set; }

		public static RenameItem FromPath(string path, int index = 0, bool isDirectory = false, DateTimeOffset? created = null, DateTimeOffset? modified = null)
		{
			string fullName = Path.GetFileName(path);
			string dir = Path.GetDirectoryName(path) ?? string.Empty;
			var parts = FileNameExtensionHelper.Split(fullName, isFolder: isDirectory);

			return new RenameItem
			{
				OriginalPath = path,
				OriginalFullName = fullName,
				DirectoryPath = dir,
				NamePart = parts.NamePart,
				ExtensionPart = parts.ExtensionPart,
				CreatedDate = created,
				ModifiedDate = modified,
				IsDirectory = isDirectory,
				Index = index
			};
		}
	}

	public sealed class RenamePreviewItem
	{
		public RenameItem Source { get; set; } = null!;
		public string OriginalFullName => Source.OriginalFullName;
		public string NewFullName { get; set; } = string.Empty;
		public string NewPath { get; set; } = string.Empty;
		public bool HasConflict { get; set; }
		public string? ConflictReason { get; set; }
		public bool IsChanged => !string.Equals(Source.OriginalFullName, NewFullName, StringComparison.Ordinal);
	}

	public static class RenameRuleEngine
	{
		private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
		private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"CON", "PRN", "AUX", "NUL",
			"COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
		};

		public static List<RenamePreviewItem> GeneratePreview(
			IReadOnlyList<RenameItem> items,
			IReadOnlyList<IRenameRule> rules,
			Func<string, bool>? externalFileExists = null)
		{
			if (items is null || items.Count == 0)
				return new List<RenamePreviewItem>();

			var results = new List<RenamePreviewItem>(items.Count);
			var collisionTracker = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				string curName = item.NamePart;
				string curExt = item.ExtensionPart;

				if (rules is not null)
				{
					foreach (var rule in rules)
					{
						if (rule is FindReplaceRule fr && fr.ApplyToExtension)
						{
							string full = rule.Apply(curName, curExt, i, item);
							var parts = FileNameExtensionHelper.Split(full, isFolder: item.IsDirectory);
							curName = parts.NamePart;
							curExt = parts.ExtensionPart;
						}
						else
						{
							curName = rule.Apply(curName, curExt, i, item);
						}
					}
				}

				string newFullName = string.IsNullOrEmpty(curExt) ? curName : $"{curName}{curExt}";
				string newPath = string.IsNullOrEmpty(item.DirectoryPath) ? newFullName : Path.Combine(item.DirectoryPath, newFullName);

				var preview = new RenamePreviewItem
				{
					Source = item,
					NewFullName = newFullName,
					NewPath = newPath
				};

				results.Add(preview);

				// Registra para detecção de colisão no mesmo diretório
				string key = newPath.ToLowerInvariant();
				if (!collisionTracker.TryGetValue(key, out var indices))
				{
					indices = new List<int>();
					collisionTracker[key] = indices;
				}
				indices.Add(i);
			}

			// Validação de conflitos e integridade
			for (int i = 0; i < results.Count; i++)
			{
				var preview = results[i];
				string name = preview.NewFullName;

				// 1. Nome vazio
				if (string.IsNullOrWhiteSpace(name))
				{
					preview.HasConflict = true;
					preview.ConflictReason = "O nome do arquivo não pode ser vazio.";
					continue;
				}

				// 2. Caracteres proibidos no Windows
				if (name.IndexOfAny(InvalidChars) >= 0)
				{
					preview.HasConflict = true;
					preview.ConflictReason = "O nome contém caracteres inválidos (\\ / : * ? \" < > |).";
					continue;
				}

				// 3. Termina com ponto ou espaço
				if (name.EndsWith('.') || name.EndsWith(' '))
				{
					preview.HasConflict = true;
					preview.ConflictReason = "O nome do arquivo não pode terminar com ponto ou espaço.";
					continue;
				}

				// 4. Nomes de dispositivos reservados do Windows (CON, NUL, COM1, etc.)
				string baseName = Path.GetFileNameWithoutExtension(name);
				if (ReservedNames.Contains(baseName))
				{
					preview.HasConflict = true;
					preview.ConflictReason = $"O nome '{baseName}' é reservado pelo sistema Windows.";
					continue;
				}

				// 5. Limite de tamanho de caminho MAX_PATH (260)
				if (preview.NewPath.Length >= 260)
				{
					preview.HasConflict = true;
					preview.ConflictReason = "O caminho completo excede o limite de 260 caracteres.";
					continue;
				}

				// 6. Colisão dentro do próprio lote (dois itens gerando o mesmo destino)
				string key = preview.NewPath.ToLowerInvariant();
				if (collisionTracker.TryGetValue(key, out var indices) && indices.Count > 1)
				{
					preview.HasConflict = true;
					preview.ConflictReason = "Conflito: múltiplos arquivos do lote terão o mesmo nome.";
					continue;
				}

				// 7. Colisão externa (arquivo já existe no disco fora da seleção)
				if (preview.IsChanged && externalFileExists is not null && externalFileExists(preview.NewPath))
				{
					preview.HasConflict = true;
					preview.ConflictReason = "Já existe outro arquivo no disco com este nome.";
					continue;
				}
			}

			return results;
		}
	}
}
