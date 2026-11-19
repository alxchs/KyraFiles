// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace KyraFiles.Shared.DuplicateFinder
{
    public enum DuplicateCompareMode
    {
        ContentHashSha256,
        QuickHashAndSize,
        NameAndSizeUnverified
    }

    public sealed class DuplicateFinderOptions
    {
        public List<string> TargetFolders { get; set; } = new List<string>();
        public long MinFileSizeBytes { get; set; } = 1; // ignora zero-bytes por padrao
        public bool IgnoreSystemFiles { get; set; } = true;
        public HashSet<string> IgnoredFolderNames { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            "node_modules",
            ".vs",
            "bin",
            "obj"
        };
        public DuplicateCompareMode CompareMode { get; set; } = DuplicateCompareMode.ContentHashSha256;
    }

    public sealed class DuplicateFinderProgress
    {
        public string CurrentStatus { get; set; } = string.Empty;
        public int FilesScanned { get; set; }
        public int CandidateGroups { get; set; }
    }
}
