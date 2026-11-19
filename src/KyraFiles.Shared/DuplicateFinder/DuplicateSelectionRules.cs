// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace KyraFiles.Shared.DuplicateFinder
{
    public static class DuplicateSelectionRules
    {
        /// <summary>
        /// Invariante rígida (FR-002 / AC-4): nunca permite marcar a última cópia de um grupo para exclusão.
        /// </summary>
        public static bool CanSelectForDeletion(DuplicateGroup group, DuplicateFileItem item)
        {
            if (group == null || item == null) return false;
            if (item.IsSelectedForDeletion) return true; // já está selecionado, mantê-lo é seguro

            int unselectedCount = group.Items.Count(i => !i.IsSelectedForDeletion);
            return unselectedCount > 1; // se houver apenas 1 desmarcado (o próprio item), proíbe marcar
        }

        public static bool TryToggleSelection(DuplicateGroup group, DuplicateFileItem item)
        {
            if (group == null || item == null) return false;

            if (item.IsSelectedForDeletion)
            {
                item.IsSelectedForDeletion = false;
                return true;
            }

            if (CanSelectForDeletion(group, item))
            {
                item.IsSelectedForDeletion = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Mantém o arquivo mais antigo (CreatedAt mais antigo) e marca os demais para exclusão (AC-3).
        /// </summary>
        public static void ApplyKeepOldest(DuplicateGroup group)
        {
            if (group == null || group.Items.Count <= 1) return;

            var sorted = group.Items.OrderBy(i => i.CreatedAt).ToList();
            var keeper = sorted.First();

            foreach (var item in group.Items)
            {
                item.IsSelectedForDeletion = !ReferenceEquals(item, keeper);
            }
        }

        /// <summary>
        /// Mantém o arquivo mais recente (ModifiedAt mais recente) e marca os demais para exclusão.
        /// </summary>
        public static void ApplyKeepNewest(DuplicateGroup group)
        {
            if (group == null || group.Items.Count <= 1) return;

            var sorted = group.Items.OrderByDescending(i => i.ModifiedAt).ToList();
            var keeper = sorted.First();

            foreach (var item in group.Items)
            {
                item.IsSelectedForDeletion = !ReferenceEquals(item, keeper);
            }
        }

        /// <summary>
        /// Prefere manter o arquivo localizado dentro da pasta indicada. Se nenhum estiver nela, mantém o primeiro.
        /// </summary>
        public static void ApplyKeepInFolder(DuplicateGroup group, string preferredFolder)
        {
            if (group == null || group.Items.Count <= 1) return;

            var normalizedFolder = Path.GetFullPath(preferredFolder).TrimEnd('\\', '/');
            var keeper = group.Items.FirstOrDefault(i =>
            {
                var itemDir = Path.GetFullPath(i.FolderPath).TrimEnd('\\', '/');
                return itemDir.Equals(normalizedFolder, StringComparison.OrdinalIgnoreCase) ||
                       itemDir.StartsWith(normalizedFolder + "\\", StringComparison.OrdinalIgnoreCase);
            }) ?? group.Items.First();

            foreach (var item in group.Items)
            {
                item.IsSelectedForDeletion = !ReferenceEquals(item, keeper);
            }
        }

        /// <summary>
        /// Desmarca todos os itens de exclusão do grupo.
        /// </summary>
        public static void ClearSelection(DuplicateGroup group)
        {
            if (group == null) return;
            foreach (var item in group.Items)
            {
                item.IsSelectedForDeletion = false;
            }
        }
    }
}
