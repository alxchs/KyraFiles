// Copyright (c) Files Community. Licensed under the MIT License.

using System.Text.RegularExpressions;
using KyraFiles.App.Search.Index.Interop;

namespace KyraFiles.App.Search.Index.Volumes;

/// <summary>
/// Mantém a tabela de mapeamento de volumes físicos locais e apelidos de unidades subst.
/// Atende:
/// - FR-001 / FR-002: volumes locais ativos com letra ou montagem (sem volumes ocultos de sistema).
/// - FR-010: indexação única por volume físico.
/// - FR-011: substs como apelidos.
/// - FR-013: raiz de exibição preferencial.
/// - FR-017: rede fora do índice.
/// </summary>
public sealed class VolumeTable
{
	private static readonly Regex VolumeGuidRegex = new(
		@"^\\\\\?\\Volume\{([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\}\\?$",
		RegexOptions.Compiled);

	private readonly List<VolumeInfo> _volumes = [];
	private readonly Dictionary<string, string> _substAliases = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyList<VolumeInfo> Volumes => _volumes;
	public IReadOnlyDictionary<string, string> SubstAliases => _substAliases;

	/// <summary>
	/// Cria uma tabela de volumes obtendo as informações reais do Windows.
	/// </summary>
	public VolumeTable()
	{
		Refresh();
	}

	/// <summary>
	/// Construtor para testes unitários com volumes e apelidos simulados.
	/// </summary>
	public VolumeTable(IEnumerable<VolumeInfo> volumes, IDictionary<string, string>? substAliases = null)
	{
		_volumes.AddRange(volumes);
		if (substAliases is not null)
		{
			foreach (var (k, v) in substAliases)
				_substAliases[NormalizeDriveLetter(k)] = v.TrimEnd('\\');
		}
	}

	/// <summary>
	/// Atualiza a tabela com o estado atual do sistema operacional.
	/// </summary>
	public void Refresh()
	{
		_volumes.Clear();
		_substAliases.Clear();

		EnumerateVolumes();
		EnumerateSubstDrives();
	}

	/// <summary>
	/// Localiza o volume pelo GUID físico.
	/// </summary>
	public VolumeInfo? FindVolumeByGuid(Guid guid)
	{
		return _volumes.FirstOrDefault(v => v.VolumeGuid == guid);
	}

	/// <summary>
	/// Localiza o volume que contém determinado caminho de arquivo ou pasta.
	/// Se o caminho usar uma letra subst, traduz o caminho antes da correspondência.
	/// </summary>
	public VolumeInfo? FindVolumeByPath(string path, out string resolvedPath)
	{
		resolvedPath = ResolveSubstPath(path);
		string normalized = resolvedPath.TrimEnd('\\') + '\\';

		VolumeInfo? bestMatch = null;
		int longestPrefixLength = -1;

		foreach (var volume in _volumes)
		{
			foreach (var mountPath in volume.MountPaths)
			{
				string mount = mountPath.TrimEnd('\\') + '\\';
				if (normalized.StartsWith(mount, StringComparison.OrdinalIgnoreCase))
				{
					if (mount.Length > longestPrefixLength)
					{
						longestPrefixLength = mount.Length;
						bestMatch = volume;
					}
				}
			}
		}

		return bestMatch;
	}

	/// <summary>
	/// Se o caminho começar com uma letra subst, substitui pelo caminho físico real.
	/// </summary>
	public string ResolveSubstPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return string.Empty;

		if (path.Length >= 2 && path[1] == ':')
		{
			string drive = NormalizeDriveLetter(path[..2]);
			if (_substAliases.TryGetValue(drive, out var targetPath))
			{
				string remainder = path.Length > 2 ? path[2..].TrimStart('\\') : string.Empty;
				return string.IsNullOrEmpty(remainder) ? targetPath : Path.Combine(targetPath, remainder);
			}
		}

		return path;
	}

	private void EnumerateVolumes()
	{
		Span<char> volumeNameBuffer = stackalloc char[128];
		IntPtr hFind = NativeMethods.FindFirstVolumeW(volumeNameBuffer, (uint)volumeNameBuffer.Length);
		if (hFind == IntPtr.Zero || hFind == new IntPtr(-1))
			return;

		try
		{
			do
			{
				string volumeGuidPath = new(volumeNameBuffer.Slice(0, IndexOfNull(volumeNameBuffer)));
				if (string.IsNullOrEmpty(volumeGuidPath))
					continue;

				var match = VolumeGuidRegex.Match(volumeGuidPath);
				if (!match.Success || !Guid.TryParse(match.Groups[1].Value, out var guid))
					continue;

				// Obtém caminhos de montagem
				var mountPaths = GetVolumeMountPaths(volumeGuidPath);

				// FR-002: Volumes sem letra e sem ponto de montagem (EFI, recuperação) são descartados
				if (mountPaths.Count == 0)
					continue;

				// Verifica o tipo de drive no primeiro caminho
				uint driveType = NativeMethods.GetDriveTypeW(mountPaths[0]);

				// FR-017: Volumes de rede são descartados
				if (driveType == NativeMethods.DRIVE_REMOTE)
					continue;

				// FR-013: Raiz de exibição prefere letra (ex: "C:\"); se não tiver, o caminho mais curto
				string displayRoot = DetermineDisplayRoot(mountPaths);

				_volumes.Add(new VolumeInfo(guid, volumeGuidPath, displayRoot, mountPaths, driveType));

			} while (NativeMethods.FindNextVolumeW(hFind, volumeNameBuffer, (uint)volumeNameBuffer.Length));
		}
		finally
		{
			NativeMethods.FindVolumeClose(hFind);
		}
	}

	private void EnumerateSubstDrives()
	{
		Span<char> targetBuffer = stackalloc char[512];

		for (char letter = 'A'; letter <= 'Z'; letter++)
		{
			string driveName = $"{letter}:";
			uint length = NativeMethods.QueryDosDeviceW(driveName, targetBuffer, (uint)targetBuffer.Length);
			if (length == 0)
				continue;

			string target = new(targetBuffer.Slice(0, IndexOfNull(targetBuffer)));

			// Unidades subst do Windows retornam destino com prefixo "\??\"
			if (target.StartsWith(@"\??\", StringComparison.Ordinal))
			{
				string realPath = target[4..];
				_substAliases[driveName] = realPath.TrimEnd('\\');
			}
		}
	}

	private static List<string> GetVolumeMountPaths(string volumeGuidPath)
	{
		var result = new List<string>(2);
		Span<char> pathsBuffer = stackalloc char[512];

		if (NativeMethods.GetVolumePathNamesForVolumeNameW(volumeGuidPath, pathsBuffer, (uint)pathsBuffer.Length, out _))
		{
			int start = 0;
			for (int i = 0; i < pathsBuffer.Length; i++)
			{
				if (pathsBuffer[i] == '\0')
				{
					if (i > start)
					{
						string path = new(pathsBuffer.Slice(start, i - start));
						result.Add(path);
					}
					start = i + 1;
					if (start < pathsBuffer.Length && pathsBuffer[start] == '\0')
						break;
				}
			}
		}

		return result;
	}

	private static string DetermineDisplayRoot(List<string> mountPaths)
	{
		// 1. Procura caminho que seja letra de drive (ex: "C:\")
		foreach (var path in mountPaths)
		{
			if (path.Length == 3 && path[1] == ':' && path[2] == '\\')
				return path;
		}

		// 2. Se nenhuma letra, pega o caminho de montagem mais curto
		return mountPaths.OrderBy(p => p.Length).First();
	}

	private static string NormalizeDriveLetter(string drive)
	{
		if (drive.Length >= 2 && drive[1] == ':')
			return char.ToUpperInvariant(drive[0]) + ":";
		return drive.ToUpperInvariant();
	}

	private static int IndexOfNull(ReadOnlySpan<char> span)
	{
		int idx = span.IndexOf('\0');
		return idx >= 0 ? idx : span.Length;
	}
}

