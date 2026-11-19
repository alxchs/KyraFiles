// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.DuplicateFinder
{
    public static class DuplicateFinderEngine
    {
        private const int QuickHashBufferSize = 4096; // 4 KiB
        private const int FullHashBufferSize = 65536;  // 64 KiB buffer streaming (NFR-001)

        public static async Task<DuplicateFinderResult> FindDuplicatesAsync(
            DuplicateFinderOptions options,
            IProgress<DuplicateFinderProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var unverified = new List<UnverifiedFileItem>();
            var scannedItems = new List<DuplicateFileItem>();
            int filesScanned = 0;

            // ESTÁGIO 1: Varredura de metadados e filtragem
            progress?.Report(new DuplicateFinderProgress { CurrentStatus = "Varrendo diretórios...", FilesScanned = 0, CandidateGroups = 0 });

            foreach (var folder in options.TargetFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new DuplicateFinderResult(new List<DuplicateGroup>(), unverified, filesScanned, true);

                if (!Directory.Exists(folder))
                    continue;

                EnumerateFolderRecursive(folder, options, scannedItems, unverified, ref filesScanned, progress, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return new DuplicateFinderResult(new List<DuplicateGroup>(), unverified, filesScanned, true);

            // Se for modo NameAndSizeUnverified
            if (options.CompareMode == DuplicateCompareMode.NameAndSizeUnverified)
            {
                var nameSizeGroups = scannedItems
                    .GroupBy(i => (i.FileName.ToLowerInvariant(), i.FileSize))
                    .Where(g => g.Count() > 1)
                    .Select(g => new DuplicateGroup(
                        Guid.NewGuid().ToString("N"),
                        g.Key.FileSize,
                        $"NameSize:{g.Key.Item1}",
                        g.ToList()
                    ))
                    .ToList();

                return new DuplicateFinderResult(nameSizeGroups, unverified, filesScanned, false);
            }

            // ESTÁGIO 2: Agrupamento inicial por tamanho (Length) - descarta tamanhos únicos sem tocar no disco
            var sizeGroups = scannedItems
                .GroupBy(i => i.FileSize)
                .Where(g => g.Count() > 1)
                .ToList();

            progress?.Report(new DuplicateFinderProgress
            {
                CurrentStatus = $"Filtrando {sizeGroups.Count} grupos por tamanho...",
                FilesScanned = filesScanned,
                CandidateGroups = sizeGroups.Count
            });

            // ESTÁGIO 3: Quick-Hash (primeiros 4 KiB) para grupos do mesmo tamanho
            var quickHashCandidates = new List<List<DuplicateFileItem>>();

            foreach (var group in sizeGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new DuplicateFinderResult(new List<DuplicateGroup>(), unverified, filesScanned, true);

                var subGroups = new Dictionary<string, List<DuplicateFileItem>>(StringComparer.Ordinal);

                foreach (var item in group)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return new DuplicateFinderResult(new List<DuplicateGroup>(), unverified, filesScanned, true);

                    string? qh = await ComputeQuickHashAsync(item.FilePath, unverified);
                    if (qh != null)
                    {
                        if (!subGroups.TryGetValue(qh, out var list))
                        {
                            list = new List<DuplicateFileItem>();
                            subGroups[qh] = list;
                        }
                        list.Add(item);
                    }
                }

                foreach (var sg in subGroups.Values)
                {
                    if (sg.Count > 1)
                    {
                        quickHashCandidates.Add(sg);
                    }
                }
            }

            if (options.CompareMode == DuplicateCompareMode.QuickHashAndSize)
            {
                var resultGroups = quickHashCandidates.Select(g => new DuplicateGroup(
                    Guid.NewGuid().ToString("N"),
                    g[0].FileSize,
                    "QuickHash",
                    g
                )).ToList();

                return new DuplicateFinderResult(resultGroups, unverified, filesScanned, false);
            }

            // ESTÁGIO 4: SHA-256 Streaming completo em blocos de 64 KiB para grupos sobreviventes
            progress?.Report(new DuplicateFinderProgress
            {
                CurrentStatus = "Calculando SHA-256 completo em streaming...",
                FilesScanned = filesScanned,
                CandidateGroups = quickHashCandidates.Count
            });

            var finalGroups = new List<DuplicateGroup>();

            foreach (var qhGroup in quickHashCandidates)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new DuplicateFinderResult(new List<DuplicateGroup>(), unverified, filesScanned, true);

                var fullHashGroups = new Dictionary<string, List<DuplicateFileItem>>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in qhGroup)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return new DuplicateFinderResult(new List<DuplicateGroup>(), unverified, filesScanned, true);

                    string? fullHash = await ComputeFullHashSha256Async(item.FilePath, unverified, cancellationToken);
                    if (fullHash != null)
                    {
                        if (!fullHashGroups.TryGetValue(fullHash, out var list))
                        {
                            list = new List<DuplicateFileItem>();
                            fullHashGroups[fullHash] = list;
                        }
                        list.Add(item);
                    }
                }

                foreach (var kvp in fullHashGroups)
                {
                    if (kvp.Value.Count > 1)
                    {
                        finalGroups.Add(new DuplicateGroup(
                            Guid.NewGuid().ToString("N"),
                            kvp.Value[0].FileSize,
                            kvp.Key,
                            kvp.Value
                        ));
                    }
                }
            }

            return new DuplicateFinderResult(finalGroups, unverified, filesScanned, false);
        }

        private static void EnumerateFolderRecursive(
            string dirPath,
            DuplicateFinderOptions options,
            List<DuplicateFileItem> targetList,
            List<UnverifiedFileItem> unverified,
            ref int filesScanned,
            IProgress<DuplicateFinderProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            DirectoryInfo dirInfo;
            try
            {
                dirInfo = new DirectoryInfo(dirPath);
                if (options.IgnoreSystemFiles && (dirInfo.Attributes & FileAttributes.System) != 0)
                    return;

                if (options.IgnoredFolderNames.Contains(dirInfo.Name))
                    return;
            }
            catch (Exception ex)
            {
                unverified.Add(new UnverifiedFileItem(dirPath, $"Acesso negado à pasta: {ex.Message}"));
                return;
            }

            // Arquivos na pasta
            try
            {
                foreach (var file in dirInfo.EnumerateFiles())
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    try
                    {
                        if (options.IgnoreSystemFiles && (file.Attributes & FileAttributes.System) != 0)
                            continue;

                        if (file.Length < options.MinFileSizeBytes)
                            continue;

                        targetList.Add(new DuplicateFileItem(
                            file.FullName,
                            file.Length,
                            file.LastWriteTimeUtc,
                            file.CreationTimeUtc
                        ));

                        filesScanned++;
                        if (filesScanned % 500 == 0)
                        {
                            progress?.Report(new DuplicateFinderProgress
                            {
                                CurrentStatus = $"Varrendo ({filesScanned} arquivos)...",
                                FilesScanned = filesScanned,
                                CandidateGroups = 0
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        unverified.Add(new UnverifiedFileItem(file.FullName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                unverified.Add(new UnverifiedFileItem(dirPath, $"Erro ao listar arquivos: {ex.Message}"));
            }

            // Subdiretórios
            try
            {
                foreach (var subDir in dirInfo.EnumerateDirectories())
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    if (options.IgnoredFolderNames.Contains(subDir.Name))
                        continue;

                    if (options.IgnoreSystemFiles && (subDir.Attributes & FileAttributes.System) != 0)
                        continue;

                    EnumerateFolderRecursive(subDir.FullName, options, targetList, unverified, ref filesScanned, progress, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                unverified.Add(new UnverifiedFileItem(dirPath, $"Erro ao acessar subdiretórios: {ex.Message}"));
            }
        }

        private static async Task<string?> ComputeQuickHashAsync(string filePath, List<UnverifiedFileItem> unverified)
        {
            try
            {
                byte[] buffer = new byte[QuickHashBufferSize];
                int bytesRead;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, QuickHashBufferSize, useAsync: true))
                {
                    bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);
                }

                using var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(buffer, 0, bytesRead);
                return Convert.ToHexString(hash);
            }
            catch (Exception ex)
            {
                unverified.Add(new UnverifiedFileItem(filePath, $"Falha ao ler quick-hash: {ex.Message}"));
                return null;
            }
        }

        private static async Task<string?> ComputeFullHashSha256Async(
            string filePath,
            List<UnverifiedFileItem> unverified,
            CancellationToken cancellationToken)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FullHashBufferSize, useAsync: true);
                using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

                byte[] buffer = new byte[FullHashBufferSize];
                int bytesRead;

                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    sha.AppendData(buffer, 0, bytesRead);
                }

                byte[] hash = sha.GetHashAndReset();
                return Convert.ToHexString(hash);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                unverified.Add(new UnverifiedFileItem(filePath, $"Falha ao calcular SHA-256 completo: {ex.Message}"));
                return null;
            }
        }
    }
}
