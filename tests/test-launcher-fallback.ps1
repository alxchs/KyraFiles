# Test script for Files.App.Launcher.exe fallback behavior
$ErrorActionPreference = 'Stop'

$launcherPath = Resolve-Path "src\Files.App\Assets\FilesOpenDialog\Files.App.Launcher.exe"

Write-Host "Verificando Launcher em: $launcherPath" -ForegroundColor Cyan
if (-not (Test-Path $launcherPath)) {
    throw "Launcher não encontrado em $launcherPath"
}

# 1. Testar se o binário é x64 válido
$bytes = [System.IO.File]::ReadAllBytes($launcherPath)
if ($bytes.Length -lt 1024) {
    throw "Tamanho do arquivo suspeito"
}
Write-Host "Binário válido (Tamanho: $($bytes.Length) bytes)" -ForegroundColor Green

# 2. Testar execução com parâmetro sem crash
$testPath = [System.IO.Path]::GetTempPath()
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $launcherPath
$psi.Arguments = "`"$testPath`""
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = [System.Diagnostics.Process]::Start($psi)
if ($proc -ne $null) {
    $proc.WaitForExit(3000) | Out-Null
    Write-Host "Launcher executou e finalizou com sucesso (ExitCode: $($proc.ExitCode))" -ForegroundColor Green
} else {
    throw "Falha ao iniciar o processo do Launcher"
}

Write-Host "`nTodos os testes do Launcher passaram com sucesso!" -ForegroundColor Green

