<#
  Prova P1 do Windows Sandbox (host): sobe o Sandbox, instala o Files Custom lá dentro, abre e devolve o resultado.
  Criado pelo Claude em 25/10/2026. NOT TESTED: o recurso estava desligado nesta data (ver Habilitar-WindowsSandbox.ps1).
  Contexto: C:\Users\alxch\.dev-method\SANDBOX.md. O host NÃO recebe teclas nem cliques: só abre a janela do Sandbox.
#>
param(
  [string]$Repo = 'C:\desenv\utils\FilesApp',
  [string]$ScriptsDir = $PSScriptRoot,
  [int]$MemoryMB = 4096,
  [int]$TimeoutSec = 600,
  [switch]$ComRede                      # a prova começa SEM rede; use só se o app exigir baixar o Windows App Runtime
)
$ErrorActionPreference = 'Stop'

if (-not (Get-Command WindowsSandbox.exe -ErrorAction SilentlyContinue)) {
  throw "WindowsSandbox.exe não existe: ligue o recurso (Habilitar-WindowsSandbox.ps1, como administrador) e reinicie."
}
if (Get-Process WindowsSandbox, WindowsSandboxClient, WindowsSandboxRemoteSession -ErrorAction SilentlyContinue) {
  throw "Já existe um Windows Sandbox aberto (só pode haver um). Feche-o antes."
}
$avail = (Get-Counter '\Memory\Available MBytes').CounterSamples[0].CookedValue
if ($avail -lt ($MemoryMB + 512)) {
  Write-Warning ("Memória disponível {0:N0} MB, menos que o pedido ({1} MB + folga). Feche programas ou o Sandbox ficará lento." -f $avail, $MemoryMB)
}

$setup = Join-Path $Repo 'artifacts\Setup'
if (-not (Get-ChildItem $setup -Filter 'FilesCustomSetup-*.exe' -ErrorAction SilentlyContinue)) { throw "Sem instalador em $setup." }

$guid = [guid]::NewGuid()
$out = Join-Path $env:TEMP "claude\$guid\desenv\utils\FilesApp\Files\sandbox-p1"
New-Item -ItemType Directory -Force $out | Out-Null
$net = if ($ComRede) { 'Default' } else { 'Disable' }
$wsb = Join-Path $out 'p1.wsb'
@"
<Configuration>
  <VGpu>Default</VGpu>
  <Networking>$net</Networking>
  <ProtectedClient>Enable</ProtectedClient>
  <ClipboardRedirection>Disable</ClipboardRedirection>
  <AudioInput>Disable</AudioInput>
  <VideoInput>Disable</VideoInput>
  <PrinterRedirection>Disable</PrinterRedirection>
  <MemoryInMB>$MemoryMB</MemoryInMB>
  <MappedFolders>
    <MappedFolder><HostFolder>$setup</HostFolder><SandboxFolder>C:\Host\in</SandboxFolder><ReadOnly>true</ReadOnly></MappedFolder>
    <MappedFolder><HostFolder>$ScriptsDir</HostFolder><SandboxFolder>C:\Host\scripts</SandboxFolder><ReadOnly>true</ReadOnly></MappedFolder>
    <MappedFolder><HostFolder>$out</HostFolder><SandboxFolder>C:\Host\out</SandboxFolder><ReadOnly>false</ReadOnly></MappedFolder>
  </MappedFolders>
  <LogonCommand><Command>powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\Host\scripts\run-in-sandbox.ps1</Command></LogonCommand>
</Configuration>
"@ | Set-Content $wsb -Encoding UTF8

Write-Host "Iniciando o Sandbox (rede: $net, memória: $MemoryMB MB). Saída: $out" -ForegroundColor Cyan
Start-Process WindowsSandbox.exe -ArgumentList "`"$wsb`""
$sw = [Diagnostics.Stopwatch]::StartNew()
$res = Join-Path $out 'result.json'
while (-not (Test-Path $res) -and $sw.Elapsed.TotalSeconds -lt $TimeoutSec) { Start-Sleep -Seconds 3 }

if (Test-Path $res) {
  Start-Sleep -Seconds 3
  Get-Content $res -Raw -Encoding UTF8 | ConvertFrom-Json | Format-List
} else {
  Write-Warning "Sem result.json em $TimeoutSec s. Veja $out\transcript.txt (se existir)."
}
# fecha o Sandbox (descartável: nada a limpar no convidado)
Get-Process WindowsSandbox, WindowsSandboxClient, WindowsSandboxRemoteSession -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# guarda a evidência no repositório
$ev = Join-Path $Repo 'docs\agents\evidence\sandbox-p1'
New-Item -ItemType Directory -Force $ev | Out-Null
Get-ChildItem $out -File | Where-Object Name -notmatch '\.wsb$' | Copy-Item -Destination $ev -Force
Write-Host "Evidência copiada para $ev. Temporários em $out (apague a pasta do GUID ao fim)." -ForegroundColor Green
