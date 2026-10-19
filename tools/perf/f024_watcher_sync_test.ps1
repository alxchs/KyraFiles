# Teste Automatizado de Sincronismo Imediato do FileSystemWatcher (F024)
param(
    [string]$TargetFolder = 'C:\FilesUXLab\perf\1600'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

Write-Host "Testando sincronismo concorrente do Watcher em $TargetFolder..."

$testFiles = 1..5 | ForEach-Object { Join-Path $TargetFolder "sync_test_novo_$($_).txt" }
$results = [ordered]@{}

# 1. Criação concorrente
$sw = [System.Diagnostics.Stopwatch]::StartNew()
foreach ($f in $testFiles) {
    [System.IO.File]::WriteAllText($f, "Arquivo de sincronismo concorrente")
}
$sw.Stop()
$results["Criacao5ArquivosMs"] = $sw.ElapsedMilliseconds

# Aguarda 500ms para o Watcher notificar a UI
Start-Sleep -Milliseconds 500

# 2. Remoção concorrente
$sw.Restart()
foreach ($f in $testFiles) {
    if (Test-Path $f) {
        [System.IO.File]::Delete($f)
    }
}
$sw.Stop()
$results["Exclusao5ArquivosMs"] = $sw.ElapsedMilliseconds

Write-Host "--- RESULTADOS DO SINCRONISMO DO WATCHER ---"
$results | Format-Table -AutoSize
return $results

