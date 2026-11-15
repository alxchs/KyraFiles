# Test runner for RenameRuleEngine (F037 T2)
# Run from repository root: pwsh -File tests/test-rename-engine.ps1

$ErrorActionPreference = 'Stop'

$extHelperPath = Join-Path $PSScriptRoot "..\src\KyraFiles.App\Helpers\FileNameExtensionHelper.cs"
$enginePath = Join-Path $PSScriptRoot "..\src\KyraFiles.App\Helpers\RenameRuleEngine.cs"

if (-not (Test-Path $extHelperPath)) { throw "File not found: $extHelperPath" }
if (-not (Test-Path $enginePath)) { throw "File not found: $enginePath" }

function Clean-CsSource([string]$content) {
    # Remove using statements and copyright comments
    $lines = $content -split "`r?`n" | Where-Object {
        $_ -notmatch '^\s*using\s+' -and $_ -notmatch '^\s*//'
    }
    return ($lines -join "`r`n")
}

$extClean = Clean-CsSource (Get-Content $extHelperPath -Raw)
$engineClean = Clean-CsSource (Get-Content $enginePath -Raw)

$src = @"
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

$extClean

$engineClean

public static class RenameEngineTests
{
    public static int RunAll()
    {
        int failed = 0;

        void Assert(bool condition, string testName, string? details = null)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {testName}");
            }
            else
            {
                Console.WriteLine($"[FAIL] {testName}: {details}");
                failed++;
            }
        }

        // Test 1: Substituicao simples de texto preservando extensao
        {
            var items = new[] { KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\foto_ferias.jpg", 0) };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = "foto_", Replace = "viagem_" }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].NewFullName == "viagem_ferias.jpg" && !res[0].HasConflict, "FindReplace simples de texto");
        }

        // Test 2: Regex com grupos e substituicao
        {
            var items = new[] { KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\doc_20261115_final.pdf", 0) };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = @"doc_(\d{4})(\d{2})(\d{2})", Replace = "relatorio_`$1-`$2-`$3", UseRegex = true }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].NewFullName == "relatorio_2026-11-15_final.pdf" && !res[0].HasConflict, "FindReplace com Regex");
        }

        // Test 3: Regex invalida (AC-5) - nao quebra, mantem o nome original
        {
            var items = new[] { KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\item.txt", 0) };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = "[unclosed_bracket", Replace = "xyz", UseRegex = true }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].NewFullName == "item.txt" && !res[0].HasConflict, "Regex invalida trata graciosamente (AC-5)");
        }

        // Test 4: Preservacao de extensao composta .tar.gz e dotfile (AC-4)
        {
            var items = new[]
            {
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\archive.tar.gz", 0),
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\.gitignore", 1)
            };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.NumberingRule { Position = KyraFiles.App.Helpers.InsertPosition.Suffix, StartIndex = 1, PaddingDigits = 2, Separator = "_" }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].NewFullName == "archive_01.tar.gz", "Extensao composta .tar.gz preservada (AC-4)", res[0].NewFullName);
            Assert(res[1].NewFullName == ".gitignore_02", "Dotfile sem extensao preservado (AC-4)", res[1].NewFullName);
        }

        // Test 5: Numeracao sequencial com zeros a esquerda (AC-7)
        {
            var items = new[]
            {
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\imgA.png", 0),
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\imgB.png", 1),
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\imgC.png", 2)
            };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.NumberingRule { Position = KyraFiles.App.Helpers.InsertPosition.Replace, StartIndex = 1, PaddingDigits = 3 }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].NewFullName == "001.png" && res[1].NewFullName == "002.png" && res[2].NewFullName == "003.png", "Numeracao sequencial 001.. (AC-7)");
        }

        // Test 6: Deteccao de colisao no lote (AC-2)
        {
            var items = new[]
            {
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\foto1.jpg", 0),
                KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\foto2.jpg", 1)
            };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = @"foto\d+", Replace = "foto_comum", UseRegex = true }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].HasConflict && res[1].HasConflict, "Deteccao de conflito de nomes duplicados no lote (AC-2)");
        }

        // Test 7: Deteccao de caracteres invalidos do Windows
        {
            var items = new[] { KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\normal.txt", 0) };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = "normal", Replace = "invalido:nome?*" }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].HasConflict, "Deteccao de caracteres proibidos no Windows (: ? *)");
        }

        // Test 8: Nomes reservados do Windows (CON, AUX, NUL, COM1)
        {
            var items = new[] { KyraFiles.App.Helpers.RenameItem.FromPath(@"C:\data\teste.txt", 0) };
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = "teste", Replace = "CON" }
            };
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(items, rules);
            Assert(res[0].HasConflict && res[0].ConflictReason!.Contains("reservado"), "Deteccao de nome reservado do Windows (CON.txt)");
        }

        // Test 9: Benchmark de performance: 5.000 itens em <= 500 ms (NFR-001)
        {
            var list = new List<KyraFiles.App.Helpers.RenameItem>(5000);
            for (int i = 0; i < 5000; i++)
            {
                list.Add(KyraFiles.App.Helpers.RenameItem.FromPath($@"C:\fotos\DSC_{i:D6}_viagem.jpg", i));
            }
            var rules = new KyraFiles.App.Helpers.IRenameRule[]
            {
                new KyraFiles.App.Helpers.FindReplaceRule { Find = "viagem", Replace = "europa" },
                new KyraFiles.App.Helpers.NumberingRule { Position = KyraFiles.App.Helpers.InsertPosition.Prefix, StartIndex = 1, PaddingDigits = 5 }
            };

            var sw = Stopwatch.StartNew();
            var res = KyraFiles.App.Helpers.RenameRuleEngine.GeneratePreview(list, rules);
            sw.Stop();

            long ms = sw.ElapsedMilliseconds;
            Console.WriteLine($"[PERF] 5.000 itens avaliados em {ms} ms (limite NFR-001: 500 ms)");
            Assert(ms <= 500 && res.Count == 5000 && !res[0].HasConflict, "Performance NFR-001 (5.000 itens em <= 500 ms)");
        }

        return failed;
    }
}
"@

Add-Type -TypeDefinition $src -Language CSharp
$res = [RenameEngineTests]::RunAll()
if ($res -ne 0) {
    throw "RenameEngineTests failed with $res errors."
}
Write-Host "`nTodos os testes do RenameRuleEngine passaram com sucesso!" -ForegroundColor Green
