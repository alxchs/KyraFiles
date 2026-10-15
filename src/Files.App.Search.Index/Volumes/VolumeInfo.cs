// Copyright (c) Files Community. Licensed under the MIT License.

namespace Files.App.Search.Index.Volumes;

/// <summary>
/// Descreve a identidade e pontos de montagem de um volume físico local.
/// Atende aos requisitos:
/// - FR-010: Identidade física única do volume.
/// - FR-013: Raiz de exibição preferencial (letra ou caminho mais curto).
/// </summary>
public sealed class VolumeInfo
{
	public Guid VolumeGuid { get; }
	public string VolumeGuidPath { get; }
	public string DisplayRoot { get; }
	public IReadOnlyList<string> MountPaths { get; }
	public uint DriveType { get; }

	public VolumeInfo(
		Guid volumeGuid,
		string volumeGuidPath,
		string displayRoot,
		IReadOnlyList<string> mountPaths,
		uint driveType)
	{
		VolumeGuid = volumeGuid;
		VolumeGuidPath = volumeGuidPath.TrimEnd('\\') + '\\';
		DisplayRoot = displayRoot.TrimEnd('\\') + '\\';
		MountPaths = mountPaths;
		DriveType = driveType;
	}

	public override string ToString() => $"{DisplayRoot} ({VolumeGuid})";
}

