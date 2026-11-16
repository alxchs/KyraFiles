// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace KyraFiles.Shared.Helpers
{
	public enum LinkType
	{
		None = 0,
		SymbolicLink = 1,
		Junction = 2,
		HardLink = 3
	}

	public readonly record struct LinkInfo(
		bool IsLink,
		LinkType Type,
		string TargetPath,
		bool TargetExists);

	public readonly record struct LinkCreationResult(
		bool Success,
		bool PrivilegeDenied,
		string? ErrorMessage,
		string? CreatedPath);

	public static class LinkHelper
	{
		private static readonly ConcurrentDictionary<string, (long CachedTimestamp, LinkInfo Info)> Cache =
			new(StringComparer.OrdinalIgnoreCase);

		private const long CacheTtlMs = 3000;

		public static LinkInfo GetLinkInfo(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return new LinkInfo(false, LinkType.None, string.Empty, false);

			long now = Environment.TickCount64;
			if (Cache.TryGetValue(path, out var cached) && (now - cached.CachedTimestamp < CacheTtlMs))
			{
				return cached.Info;
			}

			try
			{
				if (!Directory.Exists(path) && !File.Exists(path))
				{
					var missing = new LinkInfo(false, LinkType.None, string.Empty, false);
					Cache[path] = (now, missing);
					return missing;
				}

				var attr = File.GetAttributes(path);
				if (!attr.HasFlag(FileAttributes.ReparsePoint))
				{
					var notLink = new LinkInfo(false, LinkType.None, string.Empty, false);
					Cache[path] = (now, notLink);
					return notLink;
				}

				// Is a reparse point (SymLink or Junction)
				LinkType type = LinkType.SymbolicLink;
				string? target = null;

				if (Directory.Exists(path))
				{
					var dirInfo = new DirectoryInfo(path);
					var targetInfo = dirInfo.ResolveLinkTarget(returnFinalTarget: false);
					target = targetInfo?.FullName ?? dirInfo.LinkTarget;
					if (!string.IsNullOrEmpty(target) && target.StartsWith(@"\??\", StringComparison.Ordinal))
					{
						target = target.Substring(4);
						type = LinkType.Junction;
					}
				}
				else if (File.Exists(path))
				{
					var fileInfo = new FileInfo(path);
					var targetInfo = fileInfo.ResolveLinkTarget(returnFinalTarget: false);
					target = targetInfo?.FullName ?? fileInfo.LinkTarget;
					if (!string.IsNullOrEmpty(target) && target.StartsWith(@"\??\", StringComparison.Ordinal))
					{
						target = target.Substring(4);
					}
					type = LinkType.SymbolicLink;
				}

				if (string.IsNullOrEmpty(target))
				{
					var unresolved = new LinkInfo(true, type, string.Empty, false);
					Cache[path] = (now, unresolved);
					return unresolved;
				}

				bool targetExists = Directory.Exists(target) || File.Exists(target);
				var result = new LinkInfo(true, type, target, targetExists);
				Cache[path] = (now, result);
				return result;
			}
			catch
			{
				return new LinkInfo(false, LinkType.None, string.Empty, false);
			}
		}

		public static void InvalidateCache(string path)
		{
			Cache.TryRemove(path, out _);
		}

		public static LinkCreationResult CreateSymbolicLink(string linkPath, string targetPath)
		{
			if (string.IsNullOrWhiteSpace(linkPath) || string.IsNullOrWhiteSpace(targetPath))
				return new LinkCreationResult(false, false, "Caminhos não podem ser vazios.", null);

			bool isDirectory = Directory.Exists(targetPath);

			try
			{
				if (isDirectory)
				{
					Directory.CreateSymbolicLink(linkPath, targetPath);
				}
				else
				{
					File.CreateSymbolicLink(linkPath, targetPath);
				}

				InvalidateCache(linkPath);
				return new LinkCreationResult(true, false, null, linkPath);
			}
			catch (UnauthorizedAccessException ex)
			{
				return new LinkCreationResult(false, true, ex.Message, null);
			}
			catch (Exception ex) when (ex.HResult == unchecked((int)0x80070522) || ex.Message.Contains("privilege", StringComparison.OrdinalIgnoreCase))
			{
				// ERROR_PRIVILEGE_NOT_HELD (1314)
				return new LinkCreationResult(false, true, "Permissão insuficiente (exige Modo Desenvolvedor ou Administrador).", null);
			}
			catch (Exception ex)
			{
				return new LinkCreationResult(false, false, ex.Message, null);
			}
		}

		public static LinkCreationResult CreateJunction(string junctionPath, string targetPath)
		{
			if (string.IsNullOrWhiteSpace(junctionPath) || string.IsNullOrWhiteSpace(targetPath))
				return new LinkCreationResult(false, false, "Caminhos não podem ser vazios.", null);

			if (!Directory.Exists(targetPath))
				return new LinkCreationResult(false, false, "O destino de uma junção precisa ser um diretório existente.", null);

			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				using var process = Process.Start(psi);
				process?.WaitForExit(5000);

				if (Directory.Exists(junctionPath))
				{
					InvalidateCache(junctionPath);
					return new LinkCreationResult(true, false, null, junctionPath);
				}

				string err = process?.StandardError.ReadToEnd() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(err))
					err = process?.StandardOutput.ReadToEnd() ?? "Falha ao criar junção.";

				return new LinkCreationResult(false, false, err, null);
			}
			catch (Exception ex)
			{
				return new LinkCreationResult(false, false, ex.Message, null);
			}
		}

		public static LinkCreationResult CreateHardLink(string linkPath, string targetPath)
		{
			if (string.IsNullOrWhiteSpace(linkPath) || string.IsNullOrWhiteSpace(targetPath))
				return new LinkCreationResult(false, false, "Caminhos não podem ser vazios.", null);

			if (!File.Exists(targetPath))
				return new LinkCreationResult(false, false, "O destino de um hardlink precisa ser um arquivo existente.", null);

			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c mklink /H \"{linkPath}\" \"{targetPath}\"",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				using var process = Process.Start(psi);
				process?.WaitForExit(5000);

				if (File.Exists(linkPath))
				{
					InvalidateCache(linkPath);
					return new LinkCreationResult(true, false, null, linkPath);
				}

				string err = process?.StandardError.ReadToEnd() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(err))
					err = process?.StandardOutput.ReadToEnd() ?? "Falha ao criar hardlink.";

				return new LinkCreationResult(false, false, err, null);
			}
			catch (Exception ex)
			{
				return new LinkCreationResult(false, false, ex.Message, null);
			}
		}

		public static bool SafeDeleteLink(string linkPath)
		{
			if (string.IsNullOrWhiteSpace(linkPath))
				return false;

			try
			{
				if (Directory.Exists(linkPath))
				{
					// Safe delete: for reparse points and junctions, Directory.Delete removes only the junction!
					Directory.Delete(linkPath);
					InvalidateCache(linkPath);
					return true;
				}
				else if (File.Exists(linkPath))
				{
					File.Delete(linkPath);
					InvalidateCache(linkPath);
					return true;
				}

				return false;
			}
			catch
			{
				return false;
			}
		}
	}
}
