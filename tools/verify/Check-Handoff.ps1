# Portao do Claude: recusa handoff com CONFIRMED sem prova rastreavel.
# Uso: .\tools\verify\Check-Handoff.ps1 <handoff.md>
param([Parameter(Mandatory)][string]$Handoff)
$root = Split-Path (Split-Path $PSScriptRoot)
$fail = 0
$lines = Get-Content $Handoff
for ($i = 0; $i -lt $lines.Count; $i++) {
    $l = $lines[$i]
    if ($l -notmatch '\bCONFIRMED\b' -or $l -match 'NOT TESTED') { continue }
    $ids = [regex]::Matches($l, '\[prova:\s*([\w\-\.]+)\]') | ForEach-Object { $_.Groups[1].Value }
    if (-not $ids) { Write-Host "L$($i+1): CONFIRMED sem [prova: id] -> $l"; $fail++; continue }
    foreach ($id in $ids) {
        $p = Join-Path $root "docs\evidence\proofs\$id.log"
        if (-not (Test-Path $p)) { Write-Host "L$($i+1): prova '$id' nao existe"; $fail++; continue }
        $h = Get-Content $p -TotalCount 8
        $head = ($h | Where-Object { $_ -like 'HEAD:*' }) -replace 'HEAD:\s*', ''
        $exit = ($h | Where-Object { $_ -like 'EXIT:*' }) -replace 'EXIT:\s*', ''
        git -C $root cat-file -e "$head^{commit}" 2>$null
        if ($LASTEXITCODE -ne 0) { Write-Host "L$($i+1): '$id' cita HEAD inexistente ($head)"; $fail++ }
        if ($exit.Trim() -ne '0') { Write-Host "L$($i+1): '$id' terminou com EXIT=$exit, nao pode ser CONFIRMED"; $fail++ }
        if (($h | Where-Object { $_ -like 'DIRTY: True' })) { Write-Host "L$($i+1): '$id' rodou com arvore suja"; $fail++ }
    }
}
if ($fail) { Write-Host "REPROVADO: $fail problema(s)"; exit 1 } else { Write-Host "APROVADO no portao de evidencia"; exit 0 }
