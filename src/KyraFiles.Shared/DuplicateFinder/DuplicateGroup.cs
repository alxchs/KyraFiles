// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace KyraFiles.Shared.DuplicateFinder
{
    public sealed class DuplicateGroup
    {
        public string GroupId { get; }
        public long FileSize { get; }
        public string HashOrIdentifier { get; }
        public List<DuplicateFileItem> Items { get; }

        public long ReclaimableBytes
            => Items.Where(i => i.IsSelectedForDeletion).Sum(i => i.FileSize);

        public int SelectedCount
            => Items.Count(i => i.IsSelectedForDeletion);

        public DuplicateGroup(string groupId, long fileSize, string hashOrIdentifier, IEnumerable<DuplicateFileItem> items)
        {
            GroupId = groupId ?? Guid.NewGuid().ToString("N");
            FileSize = fileSize;
            HashOrIdentifier = hashOrIdentifier ?? string.Empty;
            Items = items?.ToList() ?? new List<DuplicateFileItem>();
        }
    }
}
