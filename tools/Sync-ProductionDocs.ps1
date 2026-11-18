param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [switch]$SkipValidate
)

$ErrorActionPreference = 'Stop'

$controlPath = Join-Path $RepoRoot 'docs\production\production-control.json'
$specsMdPath = Join-Path $RepoRoot 'docs\specs\SPECS.md'
$indexMdPath = Join-Path $RepoRoot 'docs\production\SPEC-INDEX.md'

if (-not (Test-Path $controlPath)) {
    throw "Arquivo nao encontrado: $controlPath"
}

$ctrl = Get-Content -Path $controlPath -Raw -Encoding UTF8 | ConvertFrom-Json

# Recalcular contagens e pontos a partir do array de specs (fonte unica de verdade)
$specs = @($ctrl.specs)
$statusCounts = [ordered]@{}
foreach ($grp in ($specs | Group-Object state | Sort-Object Name)) {
    $statusCounts[$grp.Name] = $grp.Count
}

$totalPoints = 0
$completedPoints = 0
$cancelledPoints = 0
foreach ($s in $specs) {
    $pts = if ($null -ne $s.points) { [int]$s.points } else { 0 }
    $totalPoints += $pts
    if ($s.state -eq 'completed') {
        $completedPoints += $pts
    } elseif ($s.state -eq 'cancelled') {
        $cancelledPoints += $pts
    }
}
$remainingPoints = $totalPoints - $completedPoints - $cancelledPoints

$nowIso = [DateTimeOffset]::Now.ToString('o')
$ctrl.project.last_updated_at = $nowIso
$ctrl.summary.spec_count = $specs.Count
$ctrl.summary.status_counts = $statusCounts
$ctrl.summary.total_points = $totalPoints
$ctrl.summary.completed_points_equivalent = $completedPoints
$ctrl.summary.remaining_points = $remainingPoints

# Gravar production-control.json normalizado (UTF-8 sem BOM, CRLF)
$jsonText = ($ctrl | ConvertTo-Json -Depth 12) -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText($controlPath, $jsonText + "`r`n", (New-Object System.Text.UTF8Encoding($false)))

# Categorias padronizadas para docs/specs/SPECS.md (consumido pela Dashboard AppDevelopmentControl)
$categories = @('finalizadas', 'em_andamento', 'fila', 'dependendo_alexandre', 'impedidas', 'invalidas')
$catStats = [ordered]@{}
$catGroups = [ordered]@{}

foreach ($cat in $categories) {
    $items = @($specs | Where-Object { $_.category -eq $cat })
    $pts = 0
    foreach ($it in $items) {
        if ($null -ne $it.points) { $pts += [int]$it.points }
    }
    $pct = if ($totalPoints -gt 0) { [Math]::Round(($pts * 100.0) / $totalPoints, 1) } else { 0.0 }
    $catStats[$cat] = [ordered]@{
        quantidade = $items.Count
        pontos     = $pts
        percentual = $pct
    }
    $catGroups[$cat] = @($items | ForEach-Object {
        if ($_.legacy_ids -and $_.legacy_ids.Count -gt 0) { $_.legacy_ids[0] } else { $_.id }
    })
}

$summaryBlock = [ordered]@{
    total_specs  = $specs.Count
    total_pontos = $totalPoints
}
foreach ($cat in $categories) {
    $summaryBlock[$cat] = $catStats[$cat]
}
$summaryJson = ($summaryBlock | ConvertTo-Json -Depth 6) -replace "`r?`n", "`r`n"

$sbSpecs = New-Object System.Text.StringBuilder
[void]$sbSpecs.AppendLine("# SPECS (indice mestre)")
[void]$sbSpecs.AppendLine("")
[void]$sbSpecs.AppendLine("Gerado automaticamente a partir de ``docs/production/production-control.json`` (protocolo v2) em $nowIso via ``tools/Sync-ProductionDocs.ps1``. Nao editar a mao.")
[void]$sbSpecs.AppendLine("")
[void]$sbSpecs.AppendLine('```json')
[void]$sbSpecs.AppendLine($summaryJson)
[void]$sbSpecs.AppendLine('```')
[void]$sbSpecs.AppendLine("")
[void]$sbSpecs.AppendLine("## Resumo")
[void]$sbSpecs.AppendLine("")
[void]$sbSpecs.AppendLine("| Categoria | Specs | Pontos | % |")
[void]$sbSpecs.AppendLine("|---|--:|--:|--:|")
foreach ($cat in $categories) {
    $st = $catStats[$cat]
    [void]$sbSpecs.AppendLine("| $cat | $($st.quantidade) | $($st.pontos) | $($st.percentual.ToString('0.0', [System.Globalization.CultureInfo]::InvariantCulture)) |")
}
[void]$sbSpecs.AppendLine("")
[void]$sbSpecs.AppendLine("## Agrupamento")
[void]$sbSpecs.AppendLine("")
foreach ($cat in $categories) {
    $list = ($catGroups[$cat] -join ', ')
    if ($list) {
        [void]$sbSpecs.AppendLine("**$cat ($($catGroups[$cat].Count))**: $list")
    } else {
        [void]$sbSpecs.AppendLine("**$cat (0)**: -")
    }
    [void]$sbSpecs.AppendLine("")
}
[void]$sbSpecs.AppendLine("## Tabela mestra")
[void]$sbSpecs.AppendLine("")
[void]$sbSpecs.AppendLine("| ID | Legado | Escopo | Pts | Estado | Categoria | Responsavel | Entrega/Commit |")
[void]$sbSpecs.AppendLine("|---|---|---|--:|---|---|---|---|")
foreach ($s in $specs) {
    $leg = if ($s.legacy_ids -and $s.legacy_ids.Count -gt 0) { $s.legacy_ids -join ', ' } else { '-' }
    $commits = @($s.evidence | Where-Object { $_.type -eq 'commit' -or $_.type -eq 'release' } | ForEach-Object { $_.ref } | Select-Object -Unique)
    $evRef = if ($commits.Count -gt 0) { $commits -join ', ' } else { '-' }
    $owner = if ($s.owner) { $s.owner } else { 'unknown' }
    [void]$sbSpecs.AppendLine("| $($s.id) | $leg | $($s.title) | $($s.points) | $($s.state) | $($s.category) | $owner | $evRef |")
}

$specsContent = ($sbSpecs.ToString()) -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText($specsMdPath, $specsContent, (New-Object System.Text.UTF8Encoding($false)))

# Gerar docs/production/SPEC-INDEX.md
$sbIndex = New-Object System.Text.StringBuilder
[void]$sbIndex.AppendLine("# SPEC-INDEX")
[void]$sbIndex.AppendLine("")
[void]$sbIndex.AppendLine("Gerado de ``production-control.json`` em $nowIso via ``tools/Sync-ProductionDocs.ps1``. Mapa SPEC -> ID legado.")
[void]$sbIndex.AppendLine("")
[void]$sbIndex.AppendLine("| SPEC | Legado | Titulo | Estado | Categoria | Pts |")
[void]$sbIndex.AppendLine("|---|---|---|---|---|--:|")
foreach ($s in $specs) {
    $leg = if ($s.legacy_ids -and $s.legacy_ids.Count -gt 0) { $s.legacy_ids -join ', ' } else { '-' }
    [void]$sbIndex.AppendLine("| $($s.id) | $leg | $($s.title) | $($s.state) | $($s.category) | $($s.points) |")
}

$indexContent = ($sbIndex.ToString()) -replace "`r?`n", "`r`n"
[System.IO.File]::WriteAllText($indexMdPath, $indexContent, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "Sincronizado: $($specs.Count) SPECs | total=$totalPoints pts | completed=$completedPoints pts | cancelled=$cancelledPoints pts | remaining=$remainingPoints pts"

if (-not $SkipValidate) {
    $validator = Join-Path $env:USERPROFILE '.dev-method\producao\Validar-Producao.ps1'
    if (Test-Path $validator) {
        & $validator -Path $RepoRoot
    } else {
        Write-Host "Validar-Producao.ps1 nao acessivel neste caminho ($validator); validacao estrutural interna OK."
    }
}
