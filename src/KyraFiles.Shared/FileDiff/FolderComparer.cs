using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.FileDiff
{
    public enum FolderDiffStatus
    {
        Identical,
        Different,
        LeftOnly,
        RightOnly
    }

    public sealed record FolderDiffItem(
        string RelativePath,
        FolderDiffStatus Status,
        long LeftSize,
        long RightSize,
        DateTimeOffset? LeftModified,
        DateTimeOffset? RightModified
    );

    public static class FolderComparer
    {
        public static async Task<IReadOnlyList<FolderDiffItem>> CompareFoldersAsync(
            string leftDir,
            string rightDir,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var result = new List<FolderDiffItem>();

                if (!Directory.Exists(leftDir) || !Directory.Exists(rightDir))
                {
                    return (IReadOnlyList<FolderDiffItem>)result;
                }

                var leftFiles = ScanDirectory(leftDir, cancellationToken);
                var rightFiles = ScanDirectory(rightDir, cancellationToken);

                var allKeys = new HashSet<string>(leftFiles.Keys, StringComparer.OrdinalIgnoreCase);
                allKeys.UnionWith(rightFiles.Keys);

                foreach (string relPath in allKeys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bool inLeft = leftFiles.TryGetValue(relPath, out var leftInfo);
                    bool inRight = rightFiles.TryGetValue(relPath, out var rightInfo);

                    if (inLeft && !inRight)
                    {
                        result.Add(new FolderDiffItem(
                            relPath,
                            FolderDiffStatus.LeftOnly,
                            leftInfo!.Length,
                            0,
                            leftInfo.LastWriteTimeUtc,
                            null
                        ));
                    }
                    else if (!inLeft && inRight)
                    {
                        result.Add(new FolderDiffItem(
                            relPath,
                            FolderDiffStatus.RightOnly,
                            0,
                            rightInfo!.Length,
                            null,
                            rightInfo.LastWriteTimeUtc
                        ));
                    }
                    else if (inLeft && inRight)
                    {
                        bool isIdentical = (leftInfo!.Length == rightInfo!.Length) &&
                                          (Math.Abs((leftInfo.LastWriteTimeUtc - rightInfo.LastWriteTimeUtc).TotalSeconds) < 2.0);

                        result.Add(new FolderDiffItem(
                            relPath,
                            isIdentical ? FolderDiffStatus.Identical : FolderDiffStatus.Different,
                            leftInfo.Length,
                            rightInfo.Length,
                            leftInfo.LastWriteTimeUtc,
                            rightInfo.LastWriteTimeUtc
                        ));
                    }
                }

                result.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
                return (IReadOnlyList<FolderDiffItem>)result;
            }, cancellationToken);
        }

        private static Dictionary<string, FileInfo> ScanDirectory(string rootDir, CancellationToken cancellationToken)
        {
            var map = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
            var di = new DirectoryInfo(rootDir);
            if (!di.Exists) return map;

            int rootLength = di.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;

            var queue = new Queue<DirectoryInfo>();
            queue.Enqueue(di);

            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = queue.Dequeue();

                try
                {
                    foreach (var file in current.EnumerateFiles())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string rel = file.FullName.Substring(rootLength);
                        map[rel] = file;
                    }

                    foreach (var subDir in current.EnumerateDirectories())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Ignora diretorios ocultos/sistema especiais para performance estavel
                        if ((subDir.Attributes & FileAttributes.Hidden) != 0 ||
                            (subDir.Attributes & FileAttributes.System) != 0)
                        {
                            continue;
                        }
                        queue.Enqueue(subDir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignora pastas protegidas sem permissao
                }
                catch (DirectoryNotFoundException)
                {
                    // Ignora links quebrados
                }
            }

            return map;
        }
    }
}
