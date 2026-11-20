$ErrorActionPreference = 'Stop'

# Importa Send-GuardedInput
. "$PSScriptRoot\Send-GuardedInput.ps1"

$guid = [System.Guid]::NewGuid().ToString()
$tempDir = Join-Path $env:TEMP "agy\$guid\desenv\utils\KyraFiles\Files\test_videos"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {
    # Copia video (leitura estrita) e cria arquivos vizinhos
    $srcVideo = "D:\DriveD\Downloads\Abertura de telas.mp4"
    if (Test-Path $srcVideo) {
        Copy-Item -LiteralPath $srcVideo -Destination (Join-Path $tempDir "02_video.mp4") -Force
    } else {
        Set-Content -Path (Join-Path $tempDir "02_video.mp4") -Value "dummy binary video header"
    }
    Set-Content -Path (Join-Path $tempDir "01_item.txt") -Value "Item anterior ao video"
    Set-Content -Path (Join-Path $tempDir "03_item.txt") -Value "Item posterior ao video"
    Set-Content -Path (Join-Path $tempDir "04_item.txt") -Value "Outro item"

    $results = @()
    $evidenceDir = "$PSScriptRoot\..\..\docs\agents\evidence\crash-reproducao"
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
    $timestamp = Get-Date -Format "yyyyMMdd-HHmm"
    $csvPath = Join-Path $evidenceDir "resultado-$timestamp.csv"

    Write-Host "Iniciando KyraFiles apontando para $tempDir..."
    $encodedPath = [System.Uri]::EscapeDataString($tempDir)
    
    # Roda contagem inicial de eventos de erro
    $baselineErrors = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='Application Error'; Id=1000} -MaxEvents 20 -ErrorAction SilentlyContinue | Where-Object { $_.TimeCreated -gt (Get-Date).AddMinutes(-5) -and $_.Message -like "*KyraFiles.exe*" }).Count

    # Inicia app com o helper protegido
    $proc = Start-GuardedApp -FilePath "shell:AppsFolder\KyraFiles_sxng2gqmgahde!App" -WaitReadyMs 6000
    
    Write-Host "KyraFiles iniciado. PID: $($proc.Id)"
    
    # Navega para a pasta via explorer protocol
    Start-Process "explorer.exe" -ArgumentList "kyrafiles-dev:?folder=$encodedPath"
    Start-Sleep -Seconds 4

    $VK_P = [byte]0x50
    $VK_ALT = [byte]0x12
    $VK_DOWN = [byte]0x28
    $VK_UP = [byte]0x26

    # Tenta ativar preview pane com Alt+P
    Write-Host "Enviando Alt+P para ligar/desligar painel de previa..."
    try {
        Send-GuardedKey -VirtualKey $VK_P -Modifier $VK_ALT -WaitAfterMs 1000
    } catch {
        Write-Warning "Falha ao enviar Alt+P: $_"
    }

    # Bateria curta: 5 rodadas com intervalos de 100ms e 200ms
    $intervals = @(100, 200)
    $crashed = $false

    foreach ($intMs in $intervals) {
        Write-Host "Testando intervalo $intMs ms (5 rodadas)..."
        for ($r = 1; $r -le 5; $r++) {
            if ($proc.HasExited) {
                Write-Host "KyraFiles encerrou inesperadamente!"
                $crashed = $true
                break
            }

            # Envia rajada curta de seta para baixo e seta para cima
            try {
                Send-GuardedKey -VirtualKey $VK_DOWN -WaitAfterMs $intMs
                Send-GuardedKey -VirtualKey $VK_DOWN -WaitAfterMs $intMs
                Send-GuardedKey -VirtualKey $VK_UP -WaitAfterMs $intMs
                Send-GuardedKey -VirtualKey $VK_UP -WaitAfterMs $intMs
                
                $results += [PSCustomObject]@{
                    Timestamp = (Get-Date).ToString("o")
                    Test = "CrashA_PreviewVideo"
                    IntervalMs = $intMs
                    Round = $r
                    Status = "COMPLETED"
                    ProcessAlive = (-not $proc.HasExited)
                }
            } catch {
                Write-Host "Excecao no envio protegido: $_"
                $results += [PSCustomObject]@{
                    Timestamp = (Get-Date).ToString("o")
                    Test = "CrashA_PreviewVideo"
                    IntervalMs = $intMs
                    Round = $r
                    Status = "ABORTED_GUARD"
                    ProcessAlive = (-not $proc.HasExited)
                }
                break
            }
        }
        if ($crashed) { break }
    }

    # Fecha o processo de teste
    if (-not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }

    # Checa se surgiram novos eventos 1000/1026
    $finalErrors = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='Application Error'; Id=1000} -MaxEvents 20 -ErrorAction SilentlyContinue | Where-Object { $_.TimeCreated -gt (Get-Date).AddMinutes(-5) -and $_.Message -like "*KyraFiles.exe*" }).Count

    $newErrors = $finalErrors - $baselineErrors
    Write-Host "Novos eventos 1000 no log: $newErrors"

    $results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding utf8
    Write-Host "CSV gerado em $csvPath"

} finally {
    # Limpa temporarios
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    $guidRoot = Split-Path (Split-Path (Split-Path (Split-Path $tempDir)))
    if (Test-Path $guidRoot) {
        Remove-Item -Path $guidRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
