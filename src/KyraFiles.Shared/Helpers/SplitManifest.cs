// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Shared.Helpers;

/// <summary>
/// Manifesto versionado de arquivo dividido (F060 M1).
/// Contém metadados do arquivo original, resumos de conferência e a lista ordenada de partes.
/// </summary>
public sealed class SplitManifest
{
	/// <summary>
	/// Versão do formato do manifesto.
	/// </summary>
	public int Version { get; set; } = 1;

	/// <summary>
	/// Nome do arquivo original dividido.
	/// </summary>
	public string OriginalFileName { get; set; } = string.Empty;

	/// <summary>
	/// Tamanho total do arquivo original em bytes.
	/// </summary>
	public long TotalSize { get; set; }

	/// <summary>
	/// Tamanho configurado para cada parte em bytes.
	/// </summary>
	public long PartSize { get; set; }

	/// <summary>
	/// Resumo CRC32 do arquivo completo em formato hexadecimal (8 caracteres maiúsculos).
	/// </summary>
	public string TotalCrc32 { get; set; } = string.Empty;

	/// <summary>
	/// Resumo SHA-256 opcional do arquivo completo em formato hexadecimal minúsculo.
	/// </summary>
	public string? TotalSha256 { get; set; }

	/// <summary>
	/// Data e hora da criação do manifesto em UTC.
	/// </summary>
	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Lista ordenada das partes que compõem o arquivo.
	/// </summary>
	public List<SplitPartInfo> Parts { get; set; } = new();

	/// <summary>
	/// Serializa o manifesto para string JSON formatada.
	/// </summary>
	public string ToJson()
	{
		return JsonSerializer.Serialize(this, SplitManifestJsonContext.Default.SplitManifest);
	}

	/// <summary>
	/// Desserializa um manifesto a partir de string JSON.
	/// </summary>
	public static SplitManifest? FromJson(string json)
	{
		return JsonSerializer.Deserialize(json, SplitManifestJsonContext.Default.SplitManifest);
	}

	/// <summary>
	/// Salva o manifesto em disco no caminho especificado.
	/// </summary>
	public async Task SaveAsync(string filePath, CancellationToken cancellationToken = default)
	{
		var json = ToJson();
		await File.WriteAllTextAsync(filePath, json, cancellationToken);
	}

	/// <summary>
	/// Carrega um manifesto a partir de arquivo em disco.
	/// </summary>
	public static async Task<SplitManifest?> LoadAsync(string filePath, CancellationToken cancellationToken = default)
	{
		var json = await File.ReadAllTextAsync(filePath, cancellationToken);
		return FromJson(json);
	}
}

/// <summary>
/// Informações de cada parte do arquivo dividido.
/// </summary>
public sealed class SplitPartInfo
{
	/// <summary>
	/// Índice da parte (1-based: 1, 2, 3...).
	/// </summary>
	public int Index { get; set; }

	/// <summary>
	/// Nome do arquivo da parte (ex.: <c>arquivo.001</c>).
	/// </summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>
	/// Tamanho da parte em bytes.
	/// </summary>
	public long Size { get; set; }

	/// <summary>
	/// Resumo CRC32 da parte em formato hexadecimal (8 caracteres maiúsculos).
	/// </summary>
	public string Crc32 { get; set; } = string.Empty;

	/// <summary>
	/// Resumo SHA-256 opcional da parte em formato hexadecimal minúsculo.
	/// </summary>
	public string? Sha256 { get; set; }
}

[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SplitManifest))]
[JsonSerializable(typeof(SplitPartInfo))]
[JsonSerializable(typeof(List<SplitPartInfo>))]
internal sealed partial class SplitManifestJsonContext : JsonSerializerContext
{
}
