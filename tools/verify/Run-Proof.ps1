# Executa um comando e grava a prova em docs/evidence/proofs/<Id>.log
# Uso: .\tools\verify\Run-Proof.ps1 -Id F065-T11-ac4 -Command 'dotnet test ...'
# O handoff cita a prova como [prova: <Id>]. Check-Handoff.ps1 recusa CONFIRMED sem ela.
param([Parameter(Mandatory)][string]$Id, [Parameter(Mandatory)][string]$Command)
$ErrorActionPreference = 'Continue'
$root = Split-Path (Split-Path $PSScriptRoot)
$out = Join-Path $root "docs\evidence\proofs\$Id.log"
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
$head = (git -C $root rev-parse HEAD).Trim()
$dirty = [bool](git -C $root status --porcelain)
$start = Get-Date
$text = & pwsh -NoProfile -Command $Command 2>&1 | Out-String
$code = $LASTEXITCODE
$hdr = @("# PROVA $Id", "COMMAND: $Command", "HEAD: $head", "DIRTY: $dirty", "START: $($start.ToString('s'))", "END: $((Get-Date).ToString('s'))", "EXIT: $code", "---")
Set-Content -Path $out -Value ($hdr + $text) -Encoding utf8
"{0}  exit={1}  sha256={2}" -f $out, $code, (Get-FileHash $out).Hash
exit $code
