using System;
using System.Collections.Generic;
using System.IO;

namespace KyraFiles.Shared.FileDiff
{
    public static class DiffToolDetector
    {
        public static List<DiffToolDefinition> DetectInstalledTools()
        {
            var results = new List<DiffToolDefinition>();

            // 1. VS Code
            string? vsCodePath = FindExecutableInPath("code.cmd") 
                ?? FindExecutableInPath("code.exe")
                ?? FindInKnownPaths(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "bin", "code.cmd"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Microsoft VS Code", "Code.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "bin", "code.cmd"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "Code.exe")
                );

            if (!string.IsNullOrEmpty(vsCodePath))
            {
                results.Add(new DiffToolDefinition(
                    "vscode",
                    "Visual Studio Code",
                    vsCodePath,
                    "--diff \"{left}\" \"{right}\"",
                    isAvailable: true
                ));
            }

            // 2. Git Diff
            string? gitPath = FindExecutableInPath("git.exe")
                ?? FindInKnownPaths(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe")
                );

            if (!string.IsNullOrEmpty(gitPath))
            {
                results.Add(new DiffToolDefinition(
                    "gitdiff",
                    "Git Diff",
                    gitPath,
                    "diff --no-index \"{left}\" \"{right}\"",
                    isAvailable: true
                ));
            }

            // 3. WinMerge
            string? winMergePath = FindExecutableInPath("WinMergeU.exe")
                ?? FindInKnownPaths(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinMerge", "WinMergeU.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinMerge", "WinMergeU.exe")
                );

            if (!string.IsNullOrEmpty(winMergePath))
            {
                results.Add(new DiffToolDefinition(
                    "winmerge",
                    "WinMerge",
                    winMergePath,
                    "\"{left}\" \"{right}\"",
                    isAvailable: true
                ));
            }

            // 4. Beyond Compare
            string? bcompPath = FindExecutableInPath("BComp.exe")
                ?? FindInKnownPaths(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Beyond Compare 5", "BComp.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Beyond Compare 4", "BComp.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Beyond Compare 4", "BComp.exe")
                );

            if (!string.IsNullOrEmpty(bcompPath))
            {
                results.Add(new DiffToolDefinition(
                    "beyondcompare",
                    "Beyond Compare",
                    bcompPath,
                    "\"{left}\" \"{right}\"",
                    isAvailable: true
                ));
            }

            // 5. Fallback padrao do Windows (fc.exe)
            string? fcPath = FindInKnownPaths(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "fc.exe"));
            if (!string.IsNullOrEmpty(fcPath))
            {
                results.Add(new DiffToolDefinition(
                    "fc",
                    "Windows File Compare (fc.exe)",
                    fcPath,
                    "\"{left}\" \"{right}\"",
                    isAvailable: true
                ));
            }

            return results;
        }

        public static string? FindExecutableInPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv)) return null;

            string[] paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string p in paths)
            {
                try
                {
                    string candidate = Path.Combine(p, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch
                {
                    // Ignora caminhos invalidos no PATH
                }
            }

            return null;
        }

        private static string? FindInKnownPaths(params string[] paths)
        {
            foreach (string p in paths)
            {
                if (!string.IsNullOrEmpty(p) && File.Exists(p))
                {
                    return p;
                }
            }
            return null;
        }
    }
}
