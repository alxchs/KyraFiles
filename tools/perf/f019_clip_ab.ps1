# F019: A/B do custo do ShapeClip no compositor, com o app parado.
# Intercala (backdrop, forma) em N rodadas; cada rodada reinicia o Files, espera o aquecimento e roda idle.ps1.
# Saida: tabela de CPU total e da thread "DWM Compositor Thread" (ms em -WaitS segundos) e mediana por combinacao.
# Restaura o user_settings.json original ao final. Janela deve ficar parada e em primeiro plano.
param(
    [string]$Backdrop = '4',          # 0 Solid, 1 Mica, 2 MicaAlt, 3 Acrylic, 4 ThinAcrylic
    [string]$Shapes = '0,6',          # 0 = sem recorte (referencia)
    [int]$Rounds = 3,
    [int]$WaitS = 10,
    [int]$WarmupS = 8,
    [string]$Aumid = 'KyraFiles_sxng2gqmgahde!App'
)
$ErrorActionPreference = 'Stop'
$settings = Join-Path $env:LOCALAPPDATA "Packages\$($Aumid.Split('!')[0])\LocalState\settings\user_settings.json"
$orig = Get-Content $settings -Raw
$results = @()
try {
    for ($r = 1; $r -le $Rounds; $r++) {
        foreach ($b in ($Backdrop -split ',')) {
            foreach ($s in ($Shapes -split ',')) {
                Stop-Process -Name Files -Force -ErrorAction SilentlyContinue; Start-Sleep 2
                $txt = $orig -replace '"AppThemeBackdropMaterial":\s*\d+', "`"AppThemeBackdropMaterial`": $b" -replace '"ControlShapeStyle":\s*\d+', "`"ControlShapeStyle`": $s"
                [IO.File]::WriteAllText($settings, $txt)
                Start-Process "shell:AppsFolder\$Aumid"
                Start-Sleep $WarmupS
                $out = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'idle.ps1') -WaitS $WaitS
                $tot = [double]([regex]::Match(($out | Select-Object -First 1), 'Parado:\s*([\d\.]+)\s*ms').Groups[1].Value -replace '\.', '')
                $cmp = 0.0
                foreach ($l in $out) { if ($l -match 'Compositor.*?([\d\.]+) ms\s*$') { $cmp = [double]($Matches[1] -replace '\.', '') } }
                $results += [pscustomobject]@{ Round = $r; Backdrop = $b; Shape = $s; CpuMs = $tot; CompositorMs = $cmp }
                "r$r backdrop=$b shape=$s -> total=$tot ms, compositor=$cmp ms"
            }
        }
    }
}
finally {
    Stop-Process -Name Files -Force -ErrorAction SilentlyContinue; Start-Sleep 2
    [IO.File]::WriteAllText($settings, $orig)
    Start-Process "shell:AppsFolder\$Aumid"
}
''
$results | Group-Object Backdrop, Shape | ForEach-Object {
    $c = ($_.Group.CpuMs | Sort-Object)[[int][math]::Floor($_.Count / 2)]
    $m = ($_.Group.CompositorMs | Sort-Object)[[int][math]::Floor($_.Count / 2)]
    "backdrop/shape {0}: mediana total={1} ms, compositor={2} ms em {3}s ({4:N0} ms/s)" -f $_.Name, $c, $m, $WaitS, ($c / $WaitS)
}
