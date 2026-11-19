// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.IO;

namespace KyraFiles.Shared.DuplicateFinder
{
    public sealed class DuplicateFileItem
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string FolderPath { get; }
        public long FileSize { get; }
        public DateTimeOffset ModifiedAt { get; }
        public DateTimeOffset CreatedAt { get; }
        public bool IsSelectedForDeletion { get; set; }

        public DuplicateFileItem(string filePath, long fileSize, DateTimeOffset modifiedAt, DateTimeOffset createdAt)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            FileName = Path.GetFileName(filePath);
            FolderPath = Path.GetDirectoryName(filePath) ?? string.Empty;
            FileSize = fileSize;
            ModifiedAt = modifiedAt;
            CreatedAt = createdAt;
            IsSelectedForDeletion = false;
        }
    }
}
