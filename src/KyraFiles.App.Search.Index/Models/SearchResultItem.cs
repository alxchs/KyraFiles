// Copyright (c) Files Community. Licensed under the MIT License.

namespace Files.App.Search.Index.Models;

/// <summary>
/// Representa um resultado de busca obtido diretamente do índice com todos os metadados pré-carregados.
/// Atende ao requisito FR-024 (sem necessidade de tocar o disco para cada item retornado).
/// </summary>
public readonly record struct SearchResultItem(
	int Index,
	ulong FileReference,
	string Name,
	string FullPath,
	long FileSize,
	DateTimeOffset CreationTime,
	DateTimeOffset LastWriteTime,
	uint FileAttributes,
	bool IsDirectory);
