// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace KyraFiles.Shared.RepoMap
{
	/// <summary>
	/// Represents the complete analysis result of a repository map.
	/// </summary>
	public sealed class RepoMapResult
	{
		public string RootPath { get; set; } = string.Empty;
		public bool IsGitRepository { get; set; }
		public string? GitStatusMessage { get; set; }
		public int TotalFiles { get; set; }
		public long TotalLines { get; set; }
		public long TotalBytes { get; set; }
		public List<LanguageInfo> Languages { get; set; } = new();
		public List<RepoFileInfo> AllFiles { get; set; } = new();
		public List<RepoFileInfo> HotSpots { get; set; } = new();
		public TimeSpan Elapsed { get; set; }
		public bool WasCancelled { get; set; }

		public string FormattedTotalBytes
		{
			get
			{
				if (TotalBytes < 1024)
					return $"{TotalBytes} B";
				if (TotalBytes < 1024 * 1024)
					return $"{TotalBytes / 1024.0:F1} KB";
				return $"{TotalBytes / (1024.0 * 1024.0):F2} MB";
			}
		}

		/// <summary>
		/// Formats the summary into standard Markdown (FR-002).
		/// </summary>
		public string ToMarkdownSummary()
		{
			var sb = new StringBuilder();
			var repoName = Path.GetFileName(RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			if (string.IsNullOrEmpty(repoName))
				repoName = RootPath;

			sb.AppendLine($"# Resumo do Repositório: {repoName}");
			sb.AppendLine();
			sb.AppendLine($"**Caminho:** `{RootPath}`  ");
			sb.AppendLine($"**Total de Arquivos:** {TotalFiles:N0} | **Total de Linhas:** {TotalLines:N0} | **Tamanho Total:** {FormattedTotalBytes} | **Tempo:** {Elapsed.TotalSeconds:F2}s");
			sb.AppendLine();

			sb.AppendLine("## Resumo por Linguagem");
			sb.AppendLine("| Linguagem | Arquivos | Linhas | % Linhas | Tamanho | % Tamanho |");
			sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");

			foreach (var lang in Languages)
			{
				sb.AppendLine($"| {lang.LanguageName} | {lang.FileCount:N0} | {lang.TotalLines:N0} | {lang.PercentLines:F1}% | {lang.FormattedTotalBytes} | {lang.PercentBytes:F1}% |");
			}

			sb.AppendLine();
			sb.AppendLine("## Maiores Arquivos");
			sb.AppendLine("| Arquivo | Linguagem | Linhas | Tamanho |");
			sb.AppendLine("| :--- | :--- | :--- | :--- |");

			foreach (var file in AllFiles.Take(15))
			{
				sb.AppendLine($"| `{file.RelativePath}` | {file.Language} | {file.LineCount:N0} | {file.FormattedSize} |");
			}

			sb.AppendLine();
			sb.AppendLine("## Pontos Quentes (Git Churn × Tamanho - Últimos 12 Meses)");
			if (IsGitRepository)
			{
				if (HotSpots.Count > 0)
				{
					sb.AppendLine("| Arquivo | Mudanças (12m) | Linhas | Tamanho | Score |");
					sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
					foreach (var hot in HotSpots.Take(15))
					{
						sb.AppendLine($"| `{hot.RelativePath}` | {hot.CommitCount12Months:N0} | {hot.LineCount:N0} | {hot.FormattedSize} | {hot.HotSpotScore:N0} |");
					}
				}
				else
				{
					sb.AppendLine("Nenhum arquivo com histórico recente detectado nos últimos 12 meses.");
				}
			}
			else
			{
				sb.AppendLine(GitStatusMessage ?? "Esta pasta não é um repositório Git. O resumo por linguagem permanece disponível.");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Exports language metrics to CSV (FR-002).
		/// </summary>
		public string ToCsvLanguages()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Linguagem,Arquivos,Linhas,PercentualLinhas,Bytes,PercentualBytes");

			foreach (var lang in Languages)
			{
				sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
					"\"{0}\",{1},{2},{3:F2},{4},{5:F2}",
					lang.LanguageName.Replace("\"", "\"\""),
					lang.FileCount,
					lang.TotalLines,
					lang.PercentLines,
					lang.TotalBytes,
					lang.PercentBytes));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Exports file-level metrics to CSV (FR-002).
		/// </summary>
		public string ToCsvFiles()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Arquivo,Linguagem,Linhas,Bytes,Mudancas12Meses,PontosQuentesScore");

			foreach (var file in AllFiles)
			{
				sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
					"\"{0}\",\"{1}\",{2},{3},{4},{5:F0}",
					file.RelativePath.Replace("\"", "\"\""),
					file.Language.Replace("\"", "\"\""),
					file.LineCount,
					file.FileSizeBytes,
					file.CommitCount12Months,
					file.HotSpotScore));
			}

			return sb.ToString();
		}
	}
}
