# Portao do Claude: confere uma branch da AGY numa worktree limpa, sem confiar no que ela relatou.
# Uso: .\tools\verify\Verify-Branch.ps1 -Branch feature/f065-x -Allowed 'src/Files.App/**','tests/**'
param([Parameter(Mandatory)][string]$Branch, [string[]]$Allowed = @(), [string]$Base = 'colab')
$root = Split-Path (Split-Path $PSScriptRoot)
$Allowed = @($Allowed | ForEach-Object { $_ -split "," } | Where-Object { $_ })
$changed = git -C $root diff --name-only "$Base...$Branch"
Write-Host "Arquivos tocados ($($changed.Count)):"; $changed | ForEach-Object { "  $_" }
$bad = @()
if ($Allowed) { $bad = $changed | Where-Object { $f = $_; -not ($Allowed | Where-Object { $f -like ($_ -replace '\*\*', '*') }) } }
if ($bad) { Write-Host "FORA DO ESCOPO:"; $bad | ForEach-Object { "  $_" } }
$spec = $changed | Where-Object { $_ -match 'spec\.md$' }
foreach ($s in $spec) { if ((git -C $root diff "$Base...$Branch" -- $s) -match '^\+.*APROVAD[OA]') { Write-Host "ALERTA: $s marca APROVADO (so Claude/Alexandre aprovam)" ; $bad += $s } }
$perigo = git -C $root log "$Base..$Branch" --format=%B | Select-String -Pattern 'CONFIRMED' -SimpleMatch
Write-Host "Mensagens de commit com CONFIRMED: $($perigo.Count) (conferir prova de cada uma)"
if ($bad) { exit 1 } else { Write-Host "Escopo OK. Falta: build e testes limpos (rodar o Claude, nao a AGY)."; exit 0 }
