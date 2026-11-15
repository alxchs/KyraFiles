# Integration test for BulkRenameExecutionService (F037 T3)
# Tests two-phase rename, swap A<->B, partial failure with locked file.
# Run with: pwsh -File tests/test-bulk-rename-execution.ps1

$ErrorActionPreference = 'Stop'

$extHelperPath = Join-Path $PSScriptRoot "..\src\KyraFiles.App\Helpers\FileNameExtensionHelper.cs"
$enginePath = Join-Path $PSScriptRoot "..\src\KyraFiles.App\Helpers\RenameRuleEngine.cs"
$servicePath = Join-Path $PSScriptRoot "..\src\KyraFiles.App\Helpers\BulkRenameExecutionService.cs"

function Clean-CsSource([string]$content) {
    $lines = $content -split "`r?`n" | Where-Object {
        $_ -notmatch '^\s*using\s+' -and $_ -notmatch '^\s*//'
    }
    return ($lines -join "`r`n")
}

$extClean = Clean-CsSource (Get-Content $extHelperPath -Raw)
$engineClean = Clean-CsSource (Get-Content $enginePath -Raw)
$serviceClean = Clean-CsSource (Get-Content $servicePath -Raw)

# Substitui referencias de App.HistoryWrapper e StorageHelpers para mock nos testes
$serviceClean = $serviceClean -replace 'App\.HistoryWrapper\?\.AddHistory\(history\);', '// mock history'
$serviceClean = $serviceClean -replace 'StorageHelpers\.FromPathAndType\(', 'MockStorageHelpers.FromPathAndType('

$src = @"
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.App.Utils.Storage;

namespace KyraFiles.App.Utils.Storage
{
    public enum FileOperationType { Rename }
    public enum FilesystemItemType { File, Directory }
    public interface IStorageItemWithPath { string Path { get; } }
    public class StorageItemWithPathMock : IStorageItemWithPath
    {
        public string Path { get; set; } = string.Empty;
    }
    public class StorageHistory
    {
        public FileOperationType OperationType { get; }
        public IList<IStorageItemWithPath> Source { get; }
        public IList<IStorageItemWithPath>? Destination { get; }
        public StorageHistory(FileOperationType op, IList<IStorageItemWithPath> src, IList<IStorageItemWithPath>? dst)
        {
            OperationType = op;
            Source = src;
            Destination = dst;
        }
    }
    public static class MockStorageHelpers
    {
        public static IStorageItemWithPath FromPathAndType(string path, FilesystemItemType type)
            => new StorageItemWithPathMock { Path = path };
    }
}

$extClean

$engineClean

$serviceClean

public static class BulkRenameExecutionTests
{
    public static async Task<int> RunAll(string testDir)
    {
        int failed = 0;
        void Assert(bool cond, string name, string? details = null)
        {
            if (cond) Console.WriteLine($"[PASS] {name}");
            else { Console.WriteLine($"[FAIL] {name}: {details}"); failed++; }
        }

        // Test 1: Troca de nomes A <-> B (Swap de duas fases)
        {
            string fileA = Path.Combine(testDir, "swap_A.txt");
            string fileB = Path.Combine(testDir, "swap_B.txt");
            await File.WriteAllTextAsync(fileA, "Conteudo Original A");
            await File.WriteAllTextAsync(fileB, "Conteudo Original B");

            var itemA = KyraFiles.App.Helpers.RenameItem.FromPath(fileA, 0);
            var itemB = KyraFiles.App.Helpers.RenameItem.FromPath(fileB, 1);

            var previewA = new KyraFiles.App.Helpers.RenamePreviewItem
            {
                Source = itemA,
                NewFullName = "swap_B.txt",
                NewPath = fileB
            };
            var previewB = new KyraFiles.App.Helpers.RenamePreviewItem
            {
                Source = itemB,
                NewFullName = "swap_A.txt",
                NewPath = fileA
            };

            var result = await KyraFiles.App.Helpers.BulkRenameExecutionService.ExecuteAsync(new[] { previewA, previewB });
            Assert(result.SuccessCount == 2 && result.FailureCount == 0, "Execucao do Swap A <-> B com sucesso");

            string contentNewA = await File.ReadAllTextAsync(fileA);
            string contentNewB = await File.ReadAllTextAsync(fileB);
            Assert(contentNewA == "Conteudo Original B" && contentNewB == "Conteudo Original A", "Conteudos invertidos corretamente no Swap");
            Assert(result.HistoryRecord != null && result.HistoryRecord.Source.Count == 2, "Historico atomico registrado com 2 itens");
        }

        // Test 2: Falha parcial resiliente com arquivo bloqueado (AC-6)
        {
            string normalFile = Path.Combine(testDir, "normal_file.txt");
            string lockedFile = Path.Combine(testDir, "locked_file.txt");
            await File.WriteAllTextAsync(normalFile, "Normal");
            await File.WriteAllTextAsync(lockedFile, "Locked");

            // Bloqueia locked_file exclusivamente
            using var stream = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var itemNormal = KyraFiles.App.Helpers.RenameItem.FromPath(normalFile, 0);
            var itemLocked = KyraFiles.App.Helpers.RenameItem.FromPath(lockedFile, 1);

            var prevNormal = new KyraFiles.App.Helpers.RenamePreviewItem
            {
                Source = itemNormal,
                NewFullName = "normal_renamed.txt",
                NewPath = Path.Combine(testDir, "normal_renamed.txt")
            };
            var prevLocked = new KyraFiles.App.Helpers.RenamePreviewItem
            {
                Source = itemLocked,
                NewFullName = "locked_renamed.txt",
                NewPath = Path.Combine(testDir, "locked_renamed.txt")
            };

            var result = await KyraFiles.App.Helpers.BulkRenameExecutionService.ExecuteAsync(new[] { prevNormal, prevLocked });
            Assert(result.SuccessCount == 1, "Item desimpedido renomeou com sucesso");
            Assert(result.FailureCount == 1, "Item travado falhou sem interromper o outro (AC-6)");
            Assert(File.Exists(Path.Combine(testDir, "normal_renamed.txt")), "Destino normal existe");
            Assert(File.Exists(lockedFile), "Arquivo bloqueado permanece no caminho original");
            Assert(result.HistoryRecord != null && result.HistoryRecord.Source.Count == 1, "Historico cobre apenas o item que mudou");
        }

        return failed;
    }
}
"@

# Pasta temporaria conforme regra: %TEMP%\agy\<GUID>\...
$guid = [Guid]::NewGuid().ToString("N")
$testDir = Join-Path $env:TEMP "agy\$guid\desenv\utils\KyraFiles\test-bulk-rename"
New-Item -ItemType Directory -Path $testDir -Force | Out-Null

try {
    Add-Type -TypeDefinition $src -Language CSharp
    $res = [BulkRenameExecutionTests]::RunAll($testDir).GetAwaiter().GetResult()
    if ($res -ne 0) {
        throw "BulkRenameExecutionTests failed with $res errors."
    }
    Write-Host "`nTodos os testes de execucao (T3) passaram com sucesso!" -ForegroundColor Green
}
finally {
    if (Test-Path $testDir) {
        Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
