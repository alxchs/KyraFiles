using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KyraFiles.Shared.Workspaces
{
    public sealed class NamedWorkspaceStore
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public NamedWorkspaceStore(string? filePath = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                string localFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KyraFiles",
                    "settings"
                );
                Directory.CreateDirectory(localFolder);
                _filePath = Path.Combine(localFolder, "workspaces.json");
            }
            else
            {
                _filePath = filePath;
                string? dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        public List<NamedWorkspace> GetAll()
        {
            lock (_lock)
            {
                if (!File.Exists(_filePath))
                {
                    return new List<NamedWorkspace>();
                }

                try
                {
                    string json = File.ReadAllText(_filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<NamedWorkspace>();
                    }

                    var list = JsonSerializer.Deserialize(json, WorkspacesJsonContext.Default.ListNamedWorkspace);
                    return list ?? new List<NamedWorkspace>();
                }
                catch
                {
                    return new List<NamedWorkspace>();
                }
            }
        }

        public NamedWorkspace? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return GetAll().FirstOrDefault(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public NamedWorkspace? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return GetAll().FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public (bool Success, string ErrorMessage) SaveOrUpdate(NamedWorkspace workspace)
        {
            if (workspace == null)
            {
                return (false, "Espaço de trabalho nulo.");
            }

            if (string.IsNullOrWhiteSpace(workspace.Name))
            {
                return (false, "O nome do espaço de trabalho é obrigatório.");
            }

            lock (_lock)
            {
                var all = GetAll();

                // AC-5: verificar unicidade de nome para outro ID
                var existingWithSameName = all.FirstOrDefault(w =>
                    string.Equals(w.Name, workspace.Name, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(w.Id, workspace.Id, StringComparison.OrdinalIgnoreCase));

                if (existingWithSameName != null)
                {
                    return (false, $"Já existe um espaço de trabalho chamado '{workspace.Name}'.");
                }

                if (string.IsNullOrWhiteSpace(workspace.Id))
                {
                    workspace.Id = Guid.NewGuid().ToString("N");
                    workspace.CreatedAtUtc = DateTimeOffset.UtcNow;
                }

                workspace.UpdatedAtUtc = DateTimeOffset.UtcNow;

                int index = all.FindIndex(w => string.Equals(w.Id, workspace.Id, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    all[index] = workspace;
                }
                else
                {
                    all.Add(workspace);
                }

                WriteAllAtomic(all);
                return (true, string.Empty);
            }
        }

        public bool Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            lock (_lock)
            {
                var all = GetAll();
                int removed = all.RemoveAll(w => string.Equals(w.Id, id, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                {
                    WriteAllAtomic(all);
                    return true;
                }
                return false;
            }
        }

        public (List<string> ValidTabs, List<string> MissingTabs) ValidatePaths(NamedWorkspace workspace)
        {
            var valid = new List<string>();
            var missing = new List<string>();

            if (workspace?.Tabs == null) return (valid, missing);

            foreach (var tab in workspace.Tabs)
            {
                if (!string.IsNullOrWhiteSpace(tab.LeftPath))
                {
                    if (Directory.Exists(tab.LeftPath) || File.Exists(tab.LeftPath))
                    {
                        valid.Add(tab.LeftPath);
                    }
                    else
                    {
                        missing.Add(tab.LeftPath);
                    }
                }

                if (!string.IsNullOrWhiteSpace(tab.RightPath))
                {
                    if (Directory.Exists(tab.RightPath) || File.Exists(tab.RightPath))
                    {
                        valid.Add(tab.RightPath);
                    }
                    else
                    {
                        missing.Add(tab.RightPath);
                    }
                }
            }

            return (valid, missing);
        }

        private void WriteAllAtomic(List<NamedWorkspace> list)
        {
            string tempFile = _filePath + ".tmp";
            string json = JsonSerializer.Serialize(list, WorkspacesJsonContext.Default.ListNamedWorkspace);
            File.WriteAllText(tempFile, json);
            File.Move(tempFile, _filePath, overwrite: true);
        }
    }
}
