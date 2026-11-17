# Roda DENTRO do Windows Sandbox (LogonCommand). PowerShell 5.1 do convidado. Prova P1: o KyraFiles instala e abre aqui?
# Entradas (somente leitura): C:\Host\in (instalador) | Saída (gravável): C:\Host\out. Sem rede, sem área de transferência.
# Criado pelo Claude em 25/10/2026. NOT TESTED: o recurso do Sandbox estava desligado nesta data.
$ErrorActionPreference = 'Continue'
$out = 'C:\Host\out'
New-Item -ItemType Directory -Force $out | Out-Null
Start-Transcript -Path "$out\transcript.txt" -Force | Out-Null
$r = [ordered]@{ inicio = (Get-Date).ToString('s') }
function Step($nome, [scriptblock]$b) {
  try { $v = & $b; $r[$nome] = $v } catch { $r[$nome] = "ERRO: $($_.Exception.Message)" }
  Write-Host ("[{0}] {1}" -f $nome, ($r[$nome] | Out-String).Trim())
}

Step 'so'       { $o = Get-CimInstance Win32_OperatingSystem; "$($o.Caption) build $($o.BuildNumber)" }
Step 'ram_mb'   { [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1MB) }
Step 'rede'     { [bool](Test-Connection 1.1.1.1 -Count 1 -Quiet -ErrorAction SilentlyContinue) }

# modo desenvolvedor no convidado (o instalador registra o pacote por Add-AppxPackage -Register)
Step 'modo_dev' { reg add 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock' /v AllowDevelopmentWithoutDevLicense /t REG_DWORD /d 1 /f | Out-String }

$inst = Get-ChildItem 'C:\Host\in' -Filter 'KyraFilesSetup-*.exe' -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
$r['instalador'] = if ($inst) { "$($inst.Name) $([math]::Round($inst.Length/1MB,1)) MB" } else { 'NAO ENCONTRADO' }
if ($inst) {
  Step 'instalacao_exit' {
    $p = Start-Process $inst.FullName -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=$out\inno.log" -Wait -PassThru
    $p.ExitCode
  }
}
Step 'pacote' { $p = Get-AppxPackage KyraFiles; if ($p) { "$($p.Name) $($p.Version) | $($p.InstallLocation) | PFN $($p.PackageFamilyName)" } else { 'KyraFiles NAO REGISTRADO' } }

$pfn = (Get-AppxPackage KyraFiles -ErrorAction SilentlyContinue).PackageFamilyName
if ($pfn) {
  Step 'abrir' { Start-Process "shell:AppsFolder\$pfn!App"; Start-Sleep -Seconds 20; 'lancado' }
  Step 'processo' {
    $p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { "KyraFiles.exe PID $($p.Id) | janela: '$($p.MainWindowTitle)' | responde: $($p.Responding) | memoria $([math]::Round($p.WorkingSet64/1MB)) MB" } else { 'KyraFiles.exe NAO ESTA RODANDO' }
  }
}
Step 'falhas_dotnet' {
  Get-WinEvent -FilterHashtable @{ LogName = 'Application'; Id = 1026; StartTime = (Get-Date).AddMinutes(-15) } -ErrorAction SilentlyContinue |
    Select-Object -First 3 | ForEach-Object { ($_.Message -split "`r?`n" | Select-Object -First 6) -join ' | ' }
}

# captura de tela do convidado (não é a prova principal)
try {
  Add-Type -AssemblyName System.Windows.Forms, System.Drawing
  $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
  $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
  [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
  $bmp.Save("$out\p1.png", [System.Drawing.Imaging.ImageFormat]::Png)
  $r['captura'] = 'p1.png'
} catch { $r['captura'] = "ERRO: $($_.Exception.Message)" }

$r['fim'] = (Get-Date).ToString('s')
$r['veredito'] = if ($r['processo'] -match 'KyraFiles.exe PID') { 'ABRIU' } else { 'NAO ABRIU' }
$r | ConvertTo-Json | Set-Content "$out\result.json" -Encoding UTF8
Stop-Transcript | Out-Null
