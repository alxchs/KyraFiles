// Copyright (c) Files Community. Licensed under the MIT License.

using Files.App.Search.Index.Interop;

namespace Files.App.Search.Index.Basic;

/// <summary>
/// Resolve o nome primário (primeiro link físico criado no volume) para arquivos com múltiplos
/// links físicos (FileId repetido no mesmo volume — FR-016, AC-21).
/// </summary>
public static class HardLinkResolver
{
	private static readonly IntPtr InvalidHandleValue = new(-1);

	/// <summary>
	/// Retorna o caminho completo do primeiro link físico do arquivo usando <c>FindFirstFileNameW</c>,
	/// ou <c>null</c> se a consulta falhar.
	/// </summary>
	public static string? ResolvePrimaryFullPath(string filePath, string volumeRoot)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return null;

		Span<char> buffer = stackalloc char[1024];
		uint length = (uint)buffer.Length;

		IntPtr hFind = NativeMethods.FindFirstFileNameW(filePath, 0, ref length, buffer);
		if (hFind == IntPtr.Zero || hFind == InvalidHandleValue)
			return null;

		try
		{
			int nullIndex = buffer.IndexOf('\0');
			int actualLen = nullIndex >= 0 ? nullIndex : (int)Math.Min(length, (uint)buffer.Length);
			if (actualLen <= 0)
				return null;

			string relativeLink = new(buffer[..actualLen]);
			string root = ExtractVolumeMountPrefix(filePath, volumeRoot);
			return root + relativeLink.TrimStart('\\');
		}
		finally
		{
			NativeMethods.FindClose(hFind);
		}
	}

	private static string ExtractVolumeMountPrefix(string filePath, string volumeRoot)
	{
		// Se filePath tem letra de unidade (ex.: "C:\..."), usa a raiz de 3 caracteres ("C:\")
		// pois FindFirstFileNameW devolve o caminho relativo à raiz do volume físico.
		if (filePath.Length >= 3 && filePath[1] == ':' && filePath[2] == '\\')
			return filePath[..3];

		return volumeRoot.TrimEnd('\\') + '\\';
	}
}
