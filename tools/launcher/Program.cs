// Copyright (c) Files Community / Files Custom
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;

namespace FilesAppLauncher
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string rawArgs = Environment.CommandLine;
            string targetPath = null;
            string extraArgs = string.Empty;

            // Extrair argumentos após o nome do executável
            if (args != null && args.Length > 0)
            {
                targetPath = args[0];
                if (args.Length > 1)
                {
                    extraArgs = string.Join(" ", args, 1, args.Length - 1);
                }
            }

            // Localizar o executável ou alias do Files Custom
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filesDevAlias = Path.Combine(localAppData, @"Microsoft\WindowsApps\files-dev.exe");
            string filesProdAlias = Path.Combine(localAppData, @"Microsoft\WindowsApps\files.exe");

            string filesExecutable = null;
            if (File.Exists(filesDevAlias))
            {
                filesExecutable = filesDevAlias;
            }
            else if (File.Exists(filesProdAlias))
            {
                filesExecutable = filesProdAlias;
            }

            // Se o Files Custom estiver disponível, tentar despachar
            if (!string.IsNullOrEmpty(filesExecutable))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = filesExecutable;
                    psi.UseShellExecute = true;

                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        // Repassar parâmetros formatados
                        if (!string.IsNullOrEmpty(extraArgs))
                        {
                            psi.Arguments = string.Format("\"{0}\" {1}", targetPath, extraArgs);
                        }
                        else
                        {
                            psi.Arguments = string.Format("\"{0}\"", targetPath);
                        }
                    }

                    Process proc = Process.Start(psi);
                    if (proc != null)
                    {
                        return; // Sucesso absoluto: Files Custom acionado
                    }
                }
                catch
                {
                    // Se falhar a inicialização do Files Custom, cair silenciosamente no fallback abaixo
                }
            }

            // FALLBACK INCONDICIONAL: Windows Explorer nativo
            // Invocado quando o Files Custom está desinstalado, ausente, inacessível ou falha.
            try
            {
                string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string explorerExe = Path.Combine(windir, "explorer.exe");

                ProcessStartInfo explorerPsi = new ProcessStartInfo();
                explorerPsi.FileName = explorerExe;
                explorerPsi.UseShellExecute = true;

                if (!string.IsNullOrEmpty(targetPath))
                {
                    if (!string.IsNullOrEmpty(extraArgs))
                    {
                        explorerPsi.Arguments = string.Format("\"{0}\" {1}", targetPath, extraArgs);
                    }
                    else
                    {
                        explorerPsi.Arguments = string.Format("\"{0}\"", targetPath);
                    }
                }

                Process.Start(explorerPsi);
            }
            catch
            {
                // Fallback final: ShellExecute neutro
            }
        }
    }
}

