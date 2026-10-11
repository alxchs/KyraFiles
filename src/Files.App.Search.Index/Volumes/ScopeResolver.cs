// Copyright (c) Files Community. Licensed under the MIT License.

using Files.App.Search.Index.Interop;

namespace Files.App.Search.Index.Volumes;

/// <summary>
/// Resolve escopos de busca a partir do caminho navegado pelo usuário, tratando letras subst,
/// junções e pontos de montagem, e reescrevendo resultados de volta para o caminho de visualização do usuário.
/// Atende:
/// - FR-011: subst como apelido.
/// - FR-015: tradução de escopo e reescrita de prefixo nos resultados (AC-4, AC-6).
/// - FR-022: escopo na pasta atual e subpastas, ou global.
/// </summary>
public static class ScopeResolver
{
	/// <summary>
	/// Resolve o caminho de busca fornecido pelo usuário para o volume físico real e seus prefixos de tradução.
	/// </summary>
	public static ResolvedScope Resolve(string? userPath, VolumeTable volumeTable)
	{
		ArgumentNullException.ThrowIfNull(volumeTable);

		// Busca global ("Este computador" ou "Home")
		if (string.IsNullOrWhiteSpace(userPath) ||
			userPath.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
			userPath.Equals("This PC", StringComparison.OrdinalIgnoreCase))
		{
			return new ResolvedScope(
				Volume: null,
				RealScopePath: string.Empty,
				UserPrefix: string.Empty,
				RealPrefix: string.Empty,
				IsGlobal: true);
		}

		string trimmedPath = userPath.Trim();

		// 1. Verifica se inicia com uma letra subst
		if (trimmedPath.Length >= 2 && trimmedPath[1] == ':')
		{
			string driveLetter = char.ToUpperInvariant(trimmedPath[0]) + ":";
			if (volumeTable.SubstAliases.TryGetValue(driveLetter, out var substTarget))
			{
				string userPrefix = driveLetter + "\\";
				string realPrefix = substTarget.TrimEnd('\\') + "\\";

				string remainder = trimmedPath.Length > 2 ? trimmedPath[2..].TrimStart('\\') : string.Empty;
				string realScope = string.IsNullOrEmpty(remainder) ? realPrefix : Path.Combine(realPrefix, remainder);

				var volume = volumeTable.FindVolumeByPath(realScope, out string finalPath);

				return new ResolvedScope(
					Volume: volume,
					RealScopePath: finalPath.TrimEnd('\\') + '\\',
					UserPrefix: userPrefix,
					RealPrefix: realPrefix,
					IsGlobal: false);
			}
		}

		// 2. Tenta resolver caminho canônico via Win32 (atravessando junções e symlinks se existirem)
		string canonicalPath = ResolveCanonicalPath(trimmedPath);

		var matchedVolume = volumeTable.FindVolumeByPath(canonicalPath, out string resolvedCanonical);
		if (matchedVolume is null)
		{
			// Fallback: busca pelo caminho fornecido diretamente
			matchedVolume = volumeTable.FindVolumeByPath(trimmedPath, out resolvedCanonical);
		}

		string userScopePrefix = string.Empty;
		string realScopePrefix = string.Empty;

		if (matchedVolume is not null)
		{
			// Se o caminho canônico atravessou uma junção/link para outro lugar
			if (!trimmedPath.StartsWith(resolvedCanonical, StringComparison.OrdinalIgnoreCase) &&
				!resolvedCanonical.StartsWith(trimmedPath, StringComparison.OrdinalIgnoreCase))
			{
				userScopePrefix = trimmedPath.TrimEnd('\\') + '\\';
				realScopePrefix = resolvedCanonical.TrimEnd('\\') + '\\';
			}
		}

		return new ResolvedScope(
			Volume: matchedVolume,
			RealScopePath: resolvedCanonical.TrimEnd('\\') + '\\',
			UserPrefix: userScopePrefix,
			RealPrefix: realScopePrefix,
			IsGlobal: false);
	}

	/// <summary>
	/// Reescreve o caminho retornado pelo índice para refletir o prefixo pelo qual o usuário navegou (FR-015).
	/// </summary>
	public static string RewriteResultPath(string resultRealPath, in ResolvedScope scope)
	{
		if (scope.IsGlobal || string.IsNullOrEmpty(scope.UserPrefix) || string.IsNullOrEmpty(scope.RealPrefix))
			return resultRealPath;

		if (resultRealPath.StartsWith(scope.RealPrefix, StringComparison.OrdinalIgnoreCase))
		{
			string remainder = resultRealPath[scope.RealPrefix.Length..];
			return scope.UserPrefix + remainder;
		}

		return resultRealPath;
	}

	private static string ResolveCanonicalPath(string path)
	{
		try
		{
			using var handle = NativeMethods.CreateFileW(
				path,
				NativeMethods.FILE_READ_ATTRIBUTES,
				NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
				IntPtr.Zero,
				NativeMethods.OPEN_EXISTING,
				NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
				IntPtr.Zero);

			if (!handle.IsInvalid)
			{
				Span<char> buffer = stackalloc char[512];
				uint length = NativeMethods.GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Length, NativeMethods.FILE_NAME_NORMALIZED | NativeMethods.VOLUME_NAME_DOS);
				if (length > 0 && length < buffer.Length)
				{
					string finalPath = new(buffer.Slice(0, (int)length));
					if (finalPath.StartsWith(@"\\?\", StringComparison.Ordinal))
						return finalPath[4..];
					return finalPath;
				}
			}
		}
		catch
		{
			// Fallback silencioso para o caminho original
		}

		return path;
	}
}

/// <summary>
/// Informações de escopo resolvido para roteamento e reescrita de caminhos na busca.
/// </summary>
public readonly record struct ResolvedScope(
	VolumeInfo? Volume,
	string RealScopePath,
	string UserPrefix,
	string RealPrefix,
	bool IsGlobal);
