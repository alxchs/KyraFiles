// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// Fornece a data e hora exata da compilação do aplicativo para exibição na tela Sobre.
	/// Atende à REGRA DE DATA E HORA DE COMPILAÇÃO NA TELA SOBRE (31/10/2026).
	/// </summary>
	internal static class BuildInfo
	{
		private static string? _buildDateTime;

		public static string BuildDateTime
		{
			get
			{
				if (_buildDateTime is null)
				{
					try
					{
						// 1. Lê o metadado AssemblyMetadataAttribute gravado pelo MSBuild no momento do build
						var attribute = typeof(BuildInfo).Assembly
							.GetCustomAttributes<AssemblyMetadataAttribute>()
							.FirstOrDefault(a => a.Key == "BuildDateTime");

						if (!string.IsNullOrEmpty(attribute?.Value))
						{
							_buildDateTime = attribute.Value;
						}
					}
					catch { }

					// 2. Fallback via File.GetLastWriteTime do executável/assembly
					if (_buildDateTime is null)
					{
						try
						{
							var dllPath = Path.Combine(AppContext.BaseDirectory, "KyraFiles.dll");
							if (!File.Exists(dllPath))
								dllPath = Path.Combine(AppContext.BaseDirectory, "Files.App.dll");
							if (File.Exists(dllPath))
							{
								_buildDateTime = File.GetLastWriteTime(dllPath).ToString("yyyy-MM-dd HH:mm:ss");
							}
							else
							{
								var exePath = Path.Combine(AppContext.BaseDirectory, "KyraFiles.exe");
								if (!File.Exists(exePath))
									exePath = Path.Combine(AppContext.BaseDirectory, "Files.exe");
								if (File.Exists(exePath))
									_buildDateTime = File.GetLastWriteTime(exePath).ToString("yyyy-MM-dd HH:mm:ss");
							}
						}
						catch { }
					}

					_buildDateTime ??= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				}

				return _buildDateTime;
			}
		}
	}
}
