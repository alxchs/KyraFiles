using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.Shared.FileDiff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
    [TestClass]
    public sealed class FileDiffTests
    {
        private string _tempDir = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString("N"), "FileDiffTests");
            Directory.CreateDirectory(_tempDir);
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
        public void BinaryDetector_TextFile_ReturnsFalse()
        {
            string txtPath = Path.Combine(_tempDir, "sample.txt");
            File.WriteAllText(txtPath, "Linha 1\r\nLinha 2 com acentuação: ação, coração.");

            bool isBinary = BinaryDetector.IsBinaryFile(txtPath);
            Assert.IsFalse(isBinary, "Arquivo de texto UTF-8 nao deve ser detectado como binario.");
        }

        [TestMethod]
        public void BinaryDetector_BinaryFileWithNullByte_ReturnsTrue()
        {
            string binPath = Path.Combine(_tempDir, "sample.bin");
            byte[] bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x57, 0x6F, 0x72, 0x6C, 0x64 };
            File.WriteAllBytes(binPath, bytes);

            bool isBinary = BinaryDetector.IsBinaryFile(binPath);
            Assert.IsTrue(isBinary, "Arquivo com byte nulo deve ser detectado como binario.");
        }

        [TestMethod]
        public void BinaryDetector_EmptyFile_ReturnsFalse()
        {
            string emptyPath = Path.Combine(_tempDir, "empty.txt");
            File.WriteAllBytes(emptyPath, Array.Empty<byte>());

            bool isBinary = BinaryDetector.IsBinaryFile(emptyPath);
            Assert.IsFalse(isBinary, "Arquivo vazio nao deve ser considerado binario.");
        }

        [TestMethod]
        public void DiffToolLauncher_BuildStartInfo_SubstitutesTokensProperly()
        {
            var tool = new DiffToolDefinition("test", "Test Tool", "mock-diff.exe", "--left \"{left}\" --right \"{right}\"");
            string left = @"C:\Pasta Com Espaço e Acentuação\arquivo 1.txt";
            string right = @"C:\Outra Pasta com $VAR e 'Aspas'\arquivo 2.txt";

            var psi = FileDiffLauncher.BuildStartInfo(tool, left, right);

            Assert.AreEqual("mock-diff.exe", psi.FileName);
            Assert.IsTrue(psi.Arguments.Contains(left));
            Assert.IsTrue(psi.Arguments.Contains(right));
            Assert.IsFalse(psi.Arguments.Contains("{left}"));
            Assert.IsFalse(psi.Arguments.Contains("{right}"));
        }

        [TestMethod]
        public async Task FolderComparer_ComparesTwoFoldersCorrectly()
        {
            string dirA = Path.Combine(_tempDir, "FolderA");
            string dirB = Path.Combine(_tempDir, "FolderB");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);

            // 1. Arquivo identico
            File.WriteAllText(Path.Combine(dirA, "same.txt"), "Conteudo identico");
            File.WriteAllText(Path.Combine(dirB, "same.txt"), "Conteudo identico");

            // 2. Arquivo diferente
            File.WriteAllText(Path.Combine(dirA, "diff.txt"), "Conteudo A");
            File.WriteAllText(Path.Combine(dirB, "diff.txt"), "Conteudo B com tamanho diferente");

            // 3. So na esquerda
            File.WriteAllText(Path.Combine(dirA, "only_a.txt"), "So em A");

            // 4. So na direita
            File.WriteAllText(Path.Combine(dirB, "only_b.txt"), "So em B");

            var results = await FolderComparer.CompareFoldersAsync(dirA, dirB, CancellationToken.None);

            Assert.AreEqual(4, results.Count, "Devem ser encontrados 4 itens no comparador de pastas.");

            var same = results.FirstOrDefault(r => r.RelativePath.Equals("same.txt", StringComparison.OrdinalIgnoreCase));
            var diff = results.FirstOrDefault(r => r.RelativePath.Equals("diff.txt", StringComparison.OrdinalIgnoreCase));
            var onlyA = results.FirstOrDefault(r => r.RelativePath.Equals("only_a.txt", StringComparison.OrdinalIgnoreCase));
            var onlyB = results.FirstOrDefault(r => r.RelativePath.Equals("only_b.txt", StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(same);
            Assert.AreEqual(FolderDiffStatus.Identical, same.Status);

            Assert.IsNotNull(diff);
            Assert.AreEqual(FolderDiffStatus.Different, diff.Status);

            Assert.IsNotNull(onlyA);
            Assert.AreEqual(FolderDiffStatus.LeftOnly, onlyA.Status);

            Assert.IsNotNull(onlyB);
            Assert.AreEqual(FolderDiffStatus.RightOnly, onlyB.Status);
        }

        [TestMethod]
        public void DiffToolDetector_DetectInstalledTools_DoesNotThrow()
        {
            var tools = DiffToolDetector.DetectInstalledTools();
            Assert.IsNotNull(tools);
            // Deve encontrar ao menos uma ferramenta basica do sistema (fc.exe, git ou vscode)
            Assert.IsTrue(tools.Count > 0, "Deve encontrar ao menos 1 ferramenta de comparacao disponivel no sistema.");
        }
    }
}
