using System;
using System.Collections.Generic;
using System.IO;
using KyraFiles.Shared.Workspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
    [TestClass]
    public sealed class NamedWorkspacesTests
    {
        private string _tempDir = string.Empty;
        private string _storeFile = string.Empty;
        private NamedWorkspaceStore _store = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString("N"), "NamedWorkspacesTests");
            Directory.CreateDirectory(_tempDir);
            _storeFile = Path.Combine(_tempDir, "workspaces.json");
            _store = new NamedWorkspaceStore(_storeFile);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch { }
        }

        [TestMethod]
        public void SaveOrUpdate_RoundTrip_SavesAndLoadsTabsCorrectly()
        {
            var ws = new NamedWorkspace
            {
                Id = "ws-dev",
                Name = "Desenvolvimento Principal",
                Tabs = new List<WorkspaceTabSnapshot>
                {
                    new WorkspaceTabSnapshot(@"C:\desenv\repo1", isPinned: true),
                    new WorkspaceTabSnapshot(@"C:\desenv\repo2", @"C:\desenv\repo2\docs", isDualPane: true, isPinned: false),
                },
                SelectedTabIndex = 1
            };

            var (success, error) = _store.SaveOrUpdate(ws);
            Assert.IsTrue(success, $"Deveria salvar com sucesso: {error}");

            var loaded = _store.GetById("ws-dev");
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Desenvolvimento Principal", loaded.Name);
            Assert.AreEqual(2, loaded.Tabs.Count);
            Assert.IsTrue(loaded.Tabs[0].IsPinned);
            Assert.IsTrue(loaded.Tabs[1].IsDualPane);
            Assert.AreEqual(@"C:\desenv\repo2\docs", loaded.Tabs[1].RightPath);
            Assert.AreEqual(1, loaded.SelectedTabIndex);
        }

        [TestMethod]
        public void SaveOrUpdate_DuplicateName_ReturnsError()
        {
            var ws1 = new NamedWorkspace { Id = "1", Name = "Projeto A" };
            var ws2 = new NamedWorkspace { Id = "2", Name = "Projeto A" };

            var res1 = _store.SaveOrUpdate(ws1);
            Assert.IsTrue(res1.Success);

            var res2 = _store.SaveOrUpdate(ws2);
            Assert.IsFalse(res2.Success, "Não deve permitir dois espaços de trabalho com o mesmo nome.");
            Assert.IsTrue(res2.ErrorMessage.Contains("Já existe", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void ValidatePaths_DetectsMissingFolders_WithoutThrowing()
        {
            string existingFolder = Path.Combine(_tempDir, "ExistingDir");
            Directory.CreateDirectory(existingFolder);
            string missingFolder = Path.Combine(_tempDir, "FolderQueNaoExiste");

            var ws = new NamedWorkspace
            {
                Name = "Teste Pastas",
                Tabs = new List<WorkspaceTabSnapshot>
                {
                    new WorkspaceTabSnapshot(existingFolder),
                    new WorkspaceTabSnapshot(missingFolder)
                }
            };

            var (valid, missing) = _store.ValidatePaths(ws);

            Assert.AreEqual(1, valid.Count);
            Assert.AreEqual(existingFolder, valid[0]);
            Assert.AreEqual(1, missing.Count);
            Assert.AreEqual(missingFolder, missing[0]);
        }

        [TestMethod]
        public void Delete_ExistingWorkspace_RemovesSuccessfully()
        {
            var ws = new NamedWorkspace { Id = "del-1", Name = "Para Apagar" };
            _store.SaveOrUpdate(ws);

            bool deleted = _store.Delete("del-1");
            Assert.IsTrue(deleted);

            var remaining = _store.GetAll();
            Assert.AreEqual(0, remaining.Count);
        }

        [TestMethod]
        public void SaveOrUpdate_EmptyWorkspaceName_ReturnsError()
        {
            var ws = new NamedWorkspace { Name = "   " };
            var res = _store.SaveOrUpdate(ws);
            Assert.IsFalse(res.Success);
            Assert.IsTrue(res.ErrorMessage.Contains("obrigatório", StringComparison.OrdinalIgnoreCase));
        }
    }
}
