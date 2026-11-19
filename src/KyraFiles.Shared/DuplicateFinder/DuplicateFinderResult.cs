// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace KyraFiles.Shared.DuplicateFinder
{
    public sealed record UnverifiedFileItem(string FilePath, string Reason);

    public sealed class DuplicateFinderResult
    {
        public List<DuplicateGroup> Groups { get; }
        public List<UnverifiedFileItem> UnverifiedFiles { get; }
        public int TotalFilesScanned { get; set; }
        public bool IsCancelled { get; set; }

        public int TotalDuplicatesFound
            => Groups.Sum(g => g.Items.Count);

        public long TotalReclaimableBytes
            => Groups.Sum(g => g.ReclaimableBytes);

        public DuplicateFinderResult(List<DuplicateGroup> groups, List<UnverifiedFileItem> unverifiedFiles, int totalFilesScanned, bool isCancelled)
        {
            Groups = groups ?? new List<DuplicateGroup>();
            UnverifiedFiles = unverifiedFiles ?? new List<UnverifiedFileItem>();
            TotalFilesScanned = totalFilesScanned;
            IsCancelled = isCancelled;
        }
    }
}
