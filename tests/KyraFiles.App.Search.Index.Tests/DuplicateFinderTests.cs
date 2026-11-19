// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.Shared.DuplicateFinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
    [TestClass]
    public sealed class DuplicateFinderTests
    {
        private string _testDir = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "KyraFiles_DupTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch { }
        }

        [TestMethod]
        public async Task FindDuplicates_IdenticalPairs_FindsGroups()
        {
            // AC-1: 2 pastas com pares idênticos
            var folderA = Path.Combine(_testDir, "FolderA");
            var folderB = Path.Combine(_testDir, "FolderB");
            Directory.CreateDirectory(folderA);
            Directory.CreateDirectory(folderB);

            File.WriteAllText(Path.Combine(folderA, "doc1.txt"), "Conteudo do documento 1");
            File.WriteAllText(Path.Combine(folderB, "doc1_copy.txt"), "Conteudo do documento 1");

            File.WriteAllText(Path.Combine(folderA, "doc2.txt"), "Conteudo do documento 2 com mais detalhes");
            File.WriteAllText(Path.Combine(folderB, "doc2_copy.txt"), "Conteudo do documento 2 com mais detalhes");

            File.WriteAllText(Path.Combine(folderA, "unico.txt"), "Arquivo completamente unico");

            var options = new DuplicateFinderOptions
            {
                TargetFolders = { folderA, folderB },
                CompareMode = DuplicateCompareMode.ContentHashSha256
            };

            var result = await DuplicateFinderEngine.FindDuplicatesAsync(options);

            Assert.IsFalse(result.IsCancelled);
            Assert.AreEqual(2, result.Groups.Count);
            Assert.IsTrue(result.Groups.All(g => g.Items.Count == 2));
        }

        [TestMethod]
        public async Task FindDuplicates_SameSizeDifferentContent_NotGrouped()
        {
            // AC-2: mesmo tamanho, conteudo diferente -> NAO deve agrupar
            var folder = Path.Combine(_testDir, "DiffContent");
            Directory.CreateDirectory(folder);

            string s1 = "AAAA_1234567890";
            string s2 = "BBBB_1234567890";
            Assert.AreEqual(s1.Length, s2.Length);

            File.WriteAllText(Path.Combine(folder, "file1.txt"), s1);
            File.WriteAllText(Path.Combine(folder, "file2.txt"), s2);

            var options = new DuplicateFinderOptions
            {
                TargetFolders = { folder },
                CompareMode = DuplicateCompareMode.ContentHashSha256
            };

            var result = await DuplicateFinderEngine.FindDuplicatesAsync(options);

            Assert.AreEqual(0, result.Groups.Count);
        }

        [TestMethod]
        public void SelectionRules_KeepOldest_LeavesOneUnselected()
        {
            // AC-3: grupo com 3 cópias -> mais antiga preservada, outras marcadas para exclusão
            var now = DateTimeOffset.UtcNow;
            var item1 = new DuplicateFileItem("C:\\f1.txt", 100, now, now.AddHours(-10)); // mais antiga
            var item2 = new DuplicateFileItem("C:\\f2.txt", 100, now, now.AddHours(-5));
            var item3 = new DuplicateFileItem("C:\\f3.txt", 100, now, now.AddHours(-1));

            var group = new DuplicateGroup("G1", 100, "HASH", new[] { item1, item2, item3 });

            DuplicateSelectionRules.ApplyKeepOldest(group);

            Assert.IsFalse(item1.IsSelectedForDeletion, "Mais antiga deve ser desmarcada (keeper)");
            Assert.IsTrue(item2.IsSelectedForDeletion, "Deve ser selecionada para exclusão");
            Assert.IsTrue(item3.IsSelectedForDeletion, "Deve ser selecionada para exclusão");
            Assert.AreEqual(200, group.ReclaimableBytes);
        }

        [TestMethod]
        public void SelectionRules_Invariant_CannotSelectLastCopy()
        {
            // AC-4 / FR-002: É impossível marcar TODAS as cópias de um grupo
            var now = DateTimeOffset.UtcNow;
            var item1 = new DuplicateFileItem("C:\\f1.txt", 100, now, now);
            var item2 = new DuplicateFileItem("C:\\f2.txt", 100, now, now);

            var group = new DuplicateGroup("G1", 100, "HASH", new[] { item1, item2 });

            // Marca o primeiro
            bool ok1 = DuplicateSelectionRules.TryToggleSelection(group, item1);
            Assert.IsTrue(ok1);
            Assert.IsTrue(item1.IsSelectedForDeletion);

            // Tenta marcar o segundo (a última cópia restante desmarcada)
            bool ok2 = DuplicateSelectionRules.TryToggleSelection(group, item2);
            Assert.IsFalse(ok2, "Não pode permitir marcar a última cópia do grupo");
            Assert.IsFalse(item2.IsSelectedForDeletion);
        }

        [TestMethod]
        public void SelectionRules_KeepInFolder_PrefersTargetFolder()
        {
            var now = DateTimeOffset.UtcNow;
            var item1 = new DuplicateFileItem("C:\\Downloads\\pic.jpg", 500, now, now);
            var item2 = new DuplicateFileItem("D:\\Fotos\\Viagem\\pic.jpg", 500, now, now);

            var group = new DuplicateGroup("G1", 500, "HASH", new[] { item1, item2 });

            DuplicateSelectionRules.ApplyKeepInFolder(group, "D:\\Fotos");

            Assert.IsTrue(item1.IsSelectedForDeletion, "Downloads deve ser marcado para deleção");
            Assert.IsFalse(item2.IsSelectedForDeletion, "Fotos deve ser mantido como keeper");
        }

        [TestMethod]
        public async Task FindDuplicates_Cancellation_StopsCleanly()
        {
            // AC-5: cancelamento interrompe sem erros
            var folder = Path.Combine(_testDir, "CancelFolder");
            Directory.CreateDirectory(folder);

            for (int i = 0; i < 10; i++)
            {
                File.WriteAllText(Path.Combine(folder, $"file_{i}.txt"), "Mesmo conteudo para teste de cancelamento");
            }

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // cancela imediatamente

            var options = new DuplicateFinderOptions
            {
                TargetFolders = { folder },
                CompareMode = DuplicateCompareMode.ContentHashSha256
            };

            var result = await DuplicateFinderEngine.FindDuplicatesAsync(options, cancellationToken: cts.Token);

            Assert.IsTrue(result.IsCancelled);
            Assert.AreEqual(0, result.Groups.Count);
        }

        [TestMethod]
        public async Task FindDuplicates_IgnoredFolders_Skipped()
        {
            // FR-004 / AC-7: pastas como .git ou node_modules sao ignoradas
            var gitFolder = Path.Combine(_testDir, ".git");
            var validFolder = Path.Combine(_testDir, "Valid");
            Directory.CreateDirectory(gitFolder);
            Directory.CreateDirectory(validFolder);

            File.WriteAllText(Path.Combine(gitFolder, "config.txt"), "conteudo duplicado");
            File.WriteAllText(Path.Combine(validFolder, "config.txt"), "conteudo duplicado");

            var options = new DuplicateFinderOptions
            {
                TargetFolders = { _testDir },
                CompareMode = DuplicateCompareMode.ContentHashSha256
            };

            var result = await DuplicateFinderEngine.FindDuplicatesAsync(options);

            // Como .git foi ignorado, só existe 1 arquivo na pasta Valid, logo 0 grupos de duplicados
            Assert.AreEqual(0, result.Groups.Count);
        }
    }
}
