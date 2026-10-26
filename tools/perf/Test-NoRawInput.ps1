# Test-NoRawInput.ps1
# Trava automatica de CI/commit para impedir entrada global crua em tools/perf/
# Regra R4 da metodologia e ORDEM-RIGIDA-AGY:
# Falha com codigo != 0 se qualquer arquivo em tools/perf/
# (exceto Send-GuardedInput.ps1 e a pasta _desativados/) contiver:
# SendKeys, SendInput, keybd_event, mouse_event, SetCursorPos ou SetForegroundWindow.

$ErrorActionPreference = 'Stop'

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }
$perfDir = [System.IO.Path]::GetFullPath($scriptDir)

$pattern = 'SendKeys|SendInput|keybd_event|mouse_event|SetCursorPos|SetForegroundWindow'

Write-Host "Verificando arquivos em '$perfDir' contra chamadas de entrada crua..." -ForegroundColor Cyan

# Lista arquivos em tools/perf excluindo o proprio script, o helper e a pasta _desativados
$files = Get-ChildItem -Path $perfDir -Recurse -File | Where-Object {
    $full = $_.FullName -replace '\\', '/'
    $leaf = $_.Name
    
    # Exclusoes autorizadas
    if ($full -match '/_desativados/') { return $false }
    if ($leaf -eq 'Send-GuardedInput.ps1') { return $false }
    if ($leaf -eq 'Test-NoRawInput.ps1') { return $false }
    return $true
}

$violations = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($f in $files) {
    $lines = Get-Content -Path $f.FullName -ErrorAction SilentlyContinue
    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++
        if ($line -match $pattern) {
            $violations.Add([PSCustomObject]@{
                Arquivo = $f.FullName.Substring($perfDir.Length + 1)
                Linha = $lineNum
                Trecho = $line.Trim()
            })
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor Red
    Write-Host "VIOLACAO DE SEGURANCA: ENTRADA CRUA DETECTADA EM TOOLS/PERF!" -ForegroundColor Red
    Write-Host "Todo teste automatizado DEVE importar e usar Send-GuardedInput.ps1." -ForegroundColor Red
    Write-Host "======================================================================" -ForegroundColor Red
    Write-Host ""
    
    foreach ($v in $violations) {
        Write-Host "  -> [$($v.Arquivo):$($v.Linha)] $($v.Trecho)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Total de violacoes encontradas: $($violations.Count)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "SUCESSO: Nenhum arquivo em tools/perf contem chamadas de entrada crua ou perigosa." -ForegroundColor Green
Write-Host "Todos os scripts legados estao devidamente quarentenados em _desativados/." -ForegroundColor Green
exit 0
