# Limpeza completa e higiênica de todos os artefatos de teste da F024
$ErrorActionPreference = 'SilentlyContinue'

$folder1600 = 'C:\KyraFilesUXLab\perf\1600'
if (Test-Path $folder1600) {
    Remove-Item -Recurse -Force $folder1600 -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 200
}

$isClean = -not (Test-Path $folder1600)
Write-Host "Limpeza de ${folder1600} - $(if ($isClean) { 'SUCESSO (removido)' } else { 'FALHA (ainda presente)' })"

# Retorna código 0 se estiver limpo
if ($isClean) { exit 0 } else { exit 1 }
