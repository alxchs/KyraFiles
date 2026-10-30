# Send-GuardedInput.ps1
# Ponto unico e protegido para envio de entradas em testes automatizados.
# Implementa travas estritas de seguranca (R1 a R12 da ORDEM-RIGIDA-AGY.md):
#  - Proibido qualquer ativacao forcada de primeiro plano (a janela deve ser ativada organicamente pelo SO/teste)
#  - Assert-ForegroundOwned imediato antes de CADA evento da sequencia (modificador, tecla down, tecla up)
#  - Liberacao garantida de modificadores em try/finally (nunca deixar Ctrl/Shift/Alt presos)
#  - Set-GuardedTargetPid valida estritamente identidade do executavel (nome e caminho)
#  - Apenas teclas virtuais (VK_*); texto livre exclusivamente via UI Automation (ValuePattern)
#  - Aborta imediatamente com saida != 0 se o foco nao pertencer ao processo proprio em teste

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class GuardedWin32 {
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_CHAR = 0x0102;
}
"@ -ErrorAction SilentlyContinue

# Estado do helper
$script:GuardedPid = 0
$script:GuardedProcess = $null
$script:GuardedProcessPath = $null
$script:GuardedProcessName = $null
$script:LostFocusCount = 0
$script:GuardedStartTime = [System.Diagnostics.Stopwatch]::StartNew()
$script:GuardedTimeoutSec = 120

function Resolve-GuardedFilesPid {
    [CmdletBinding()]
    param(
        [string]$AumidPath = "shell:AppsFolder\FilesDev_ykqwq8d6ps0ag!App",
        [int]$TimeoutSec = 20
    )

    # 1. Snapshot dos PIDs de Files existentes antes de iniciar
    $beforePids = @(Get-Process Files -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)

    # 2. Inicia via explorer.exe com a AUMID
    Start-Process "explorer.exe" -ArgumentList $AumidPath

    # 3. Aguarda o novo processo Files.exe aparecer (que nao constava em $beforePids)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $newProc = $null
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        Start-Sleep -Milliseconds 400
        $candidates = Get-Process Files -ErrorAction SilentlyContinue | Where-Object { $beforePids -notcontains $_.Id }
        foreach ($c in $candidates) {
            if ($c.MainWindowHandle -ne [IntPtr]::Zero) {
                $newProc = $c
                break
            }
        }
        if ($newProc) { break }
    }

    if (-not $newProc) {
        # Fallback de pesquisa: se nenhum tiver MainWindowHandle ainda, pega qualquer novo Files
        $newProc = Get-Process Files -ErrorAction SilentlyContinue | Where-Object { $beforePids -notcontains $_.Id } | Select-Object -First 1
    }

    if (-not $newProc) {
        throw "FALHA SEGURA: Nao foi possivel resolver o PID real do Files.exe iniciado por '$AumidPath'."
    }

    return $newProc
}

function Start-GuardedApp {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string]$ArgumentList = "",
        [int]$WaitReadyMs = 1000
    )

    $proc = $null

    if ($FilePath -like "shell:AppsFolder*") {
        $proc = Resolve-GuardedFilesPid -AumidPath $FilePath -TimeoutSec 20
    } else {
        $proc = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -PassThru
        if (-not $proc -or $proc.HasExited) {
            throw "FALHA SEGURA: Nao foi possivel iniciar o processo de teste '$FilePath'."
        }
    }

    $targetPath = $null
    try {
        $cim = Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction SilentlyContinue
        if ($cim) { $targetPath = $cim.ExecutablePath }
    } catch { }
    if (-not $targetPath) {
        try { $targetPath = $proc.MainModule.FileName } catch { }
    }

    $script:GuardedPid = $proc.Id
    $script:GuardedProcess = $proc
    $script:GuardedProcessPath = $targetPath
    $script:GuardedProcessName = $proc.ProcessName
    $script:LostFocusCount = 0
    $script:GuardedStartTime.Restart()

    if ($WaitReadyMs -gt 0) {
        Start-Sleep -Milliseconds $WaitReadyMs
    }

    return $proc
}

function Set-GuardedTargetPid {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$TargetPid,
        [string]$ExpectedExecutablePath = $script:GuardedProcessPath,
        [string]$ExpectedProcessName = $script:GuardedProcessName
    )

    $proc = Get-Process -Id $TargetPid -ErrorAction SilentlyContinue
    if (-not $proc -or $proc.HasExited) {
        throw "FALHA SEGURA: Processo PID $TargetPid nao existe ou ja foi encerrado."
    }

    # Validacao estrita de identidade do executavel (ajuste d do Claude):
    # So aceita um PID cujo executavel seja o do app iniciado pelo proprio script (nome e caminho)
    $targetPath = $null
    try {
        $cim = Get-CimInstance Win32_Process -Filter "ProcessId = $TargetPid" -ErrorAction SilentlyContinue
        if ($cim) { $targetPath = $cim.ExecutablePath }
    } catch { }
    if (-not $targetPath) {
        try { $targetPath = $proc.MainModule.FileName } catch { }
    }

    if ($ExpectedExecutablePath) {
        $normTarget = if ($targetPath) { [System.IO.Path]::GetFullPath($targetPath).ToLowerInvariant() } else { "" }
        $normExpected = [System.IO.Path]::GetFullPath($ExpectedExecutablePath).ToLowerInvariant()
        if (-not $targetPath -or ($normTarget -ne $normExpected)) {
            throw "SEGURANCA: O executavel do PID $TargetPid ('$targetPath') nao coincide com o executavel autorizado iniciado pelo script ('$ExpectedExecutablePath')."
        }
    } elseif ($ExpectedProcessName) {
        if ($proc.ProcessName -ne $ExpectedProcessName) {
            throw "SEGURANCA: O nome do processo PID $TargetPid ('$($proc.ProcessName)') nao coincide com o processo autorizado ('$ExpectedProcessName')."
        }
    } else {
        throw "SEGURANCA: Nenhum executavel autorizado registrado previamente por Start-GuardedApp. Proibido anexar a PID arbitrario."
    }

    $script:GuardedPid = $TargetPid
    $script:GuardedProcess = $proc
    $script:GuardedProcessPath = $targetPath
    $script:GuardedProcessName = $proc.ProcessName
    $script:LostFocusCount = 0
    $script:GuardedStartTime.Restart()
}

function Get-GuardedOwnedPids {
    param([int]$ParentPid)
    $pids = [System.Collections.Generic.List[int]]::new()
    $pids.Add($ParentPid)
    try {
        $children = Get-CimInstance Win32_Process -Filter "ParentProcessId = $ParentPid" -ErrorAction SilentlyContinue
        foreach ($c in $children) {
            $pids.Add([int]$c.ProcessId)
        }
    } catch {
        # Fallback caso CIM nao esteja acessivel no contexto
    }
    return $pids
}

function Assert-ForegroundOwned {
    [CmdletBinding()]
    param(
        [int]$TargetPid = $script:GuardedPid
    )

    if ($TargetPid -le 0) {
        throw "SEGURANCA: Nenhum processo proprio foi registrado para o teste (TargetPid <= 0)."
    }

    # Verifica tempo limite global de seguranca
    if ($script:GuardedStartTime.Elapsed.TotalSeconds -gt $script:GuardedTimeoutSec) {
        throw "SEGURANCA: Tempo limite global de seguranca excedido ($($script:GuardedTimeoutSec)s). Abortando automacao."
    }

    # Verifica se o processo alvo ainda esta vivo
    $proc = Get-Process -Id $TargetPid -ErrorAction SilentlyContinue
    if (-not $proc -or $proc.HasExited) {
        throw "SEGURANCA: Processo alvo PID $TargetPid encerrou antes da acao. Abortando imediatamente."
    }

    # Obtem a janela em foco atual no sistema com tolerancia a transicoes rapidas
    $hwnd = [IntPtr]::Zero
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        $hwnd = [GuardedWin32]::GetForegroundWindow()
        if ($hwnd -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 100
    }

    if ($hwnd -eq [IntPtr]::Zero) {
        $script:LostFocusCount++
        throw "SEGURANCA: Nenhuma janela em primeiro plano (GetForegroundWindow retornou 0 apos 1s). Abortando."
    }

    $fgPid = 0
    [void][GuardedWin32]::GetWindowThreadProcessId($hwnd, [ref]$fgPid)

    [int[]]$ownedPids = @(Get-GuardedOwnedPids -ParentPid $TargetPid)
    if ($ownedPids -notcontains [int]$fgPid) {
        $script:LostFocusCount++
        throw "SEGURANCA: Foco violado! Janela em primeiro plano pertence ao PID $fgPid, mas o processo exclusivo do teste e PID $TargetPid. Abortando automacao para proteger janelas do usuario."
    }

    $script:LostFocusCount = 0
    return $true
}

function Send-GuardedKey {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [byte]$VirtualKey,
        [byte]$Modifier = 0,
        [int]$WaitAfterMs = 50,
        [int]$TargetPid = $script:GuardedPid,
        [switch]$InjectFaultAfterModifier = $false
    )

    # Calcula scan code de hardware correspondente para traducao correta de WM_CHAR
    $scan = [byte][GuardedWin32]::MapVirtualKey($VirtualKey, 0)
    $modScan = if ($Modifier -ne 0) { [byte][GuardedWin32]::MapVirtualKey($Modifier, 0) } else { [byte]0 }

    # Ajuste (a) e (b) do Claude:
    # Repete Assert-ForegroundOwned antes de CADA evento da sequencia (modificador abaixo, tecla abaixo, tecla acima).
    # Libera os modificadores em try/finally para NUNCA deixar Ctrl, Shift ou Alt presos.
    $modPressed = $false
    try {
        if ($Modifier -ne 0) {
            Assert-ForegroundOwned -TargetPid $TargetPid
            [GuardedWin32]::keybd_event($Modifier, $modScan, 0, [UIntPtr]::Zero)
            $modPressed = $true

            # Ponto de injecao de falha para testes (C2 do Claude): induz falha DEPOIS de o modificador descer
            if ($InjectFaultAfterModifier) {
                throw "FALHA_INDUZIDA_TESTE: Simulando falha imediatamente apos descida do modificador para testar try/finally."
            }
        }

        Assert-ForegroundOwned -TargetPid $TargetPid
        [GuardedWin32]::keybd_event($VirtualKey, $scan, 0, [UIntPtr]::Zero)

        Start-Sleep -Milliseconds 30

        Assert-ForegroundOwned -TargetPid $TargetPid
        [GuardedWin32]::keybd_event($VirtualKey, $scan, [GuardedWin32]::KEYEVENTF_KEYUP, [UIntPtr]::Zero)
    } finally {
        if ($modPressed) {
            [GuardedWin32]::keybd_event($Modifier, $modScan, [GuardedWin32]::KEYEVENTF_KEYUP, [UIntPtr]::Zero)
            $modPressed = $false
        }
    }

    if ($WaitAfterMs -gt 0) {
        Start-Sleep -Milliseconds $WaitAfterMs
    }

    # Verifica se o foco foi preservado apos o processamento da mensagem
    Assert-ForegroundOwned -TargetPid $TargetPid
}

function Send-GuardedClick {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$X,
        [Parameter(Mandatory = $true)]
        [int]$Y,
        [ValidateSet('Left', 'Right')]
        [string]$Button = 'Left',
        [int]$TargetPid = $script:GuardedPid
    )

    # Assercao mandatoria IMEDIATAMENTE ANTES do clique
    Assert-ForegroundOwned -TargetPid $TargetPid

    # Validacao de contencao espacial: o clique DEVE estar contido nos limites da janela do processo
    $hwnd = [GuardedWin32]::GetForegroundWindow()
    $rect = New-Object GuardedWin32+RECT
    if ([GuardedWin32]::GetWindowRect($hwnd, [ref]$rect)) {
        if ($X -lt $rect.Left -or $X -gt $rect.Right -or $Y -lt $rect.Top -or $Y -gt $rect.Bottom) {
            throw "SEGURANCA: Clique em ($X, $Y) fora dos limites da janela do PID $TargetPid (Left: $($rect.Left), Top: $($rect.Top), Right: $($rect.Right), Bottom: $($rect.Bottom)). Abortando."
        }
    }

    # Posiciona e clica exclusivamente dentro da janela confirmada
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($X, $Y)

    if ($Button -eq 'Left') {
        [GuardedWin32]::mouse_event([GuardedWin32]::MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
        [GuardedWin32]::mouse_event([GuardedWin32]::MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    } else {
        [GuardedWin32]::mouse_event([GuardedWin32]::MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, [UIntPtr]::Zero)
        [GuardedWin32]::mouse_event([GuardedWin32]::MOUSEEVENTF_RIGHTUP, 0, 0, 0, [UIntPtr]::Zero)
    }

    Start-Sleep -Milliseconds 50
    Assert-ForegroundOwned -TargetPid $TargetPid
}

function Set-ElementText {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [System.Windows.Automation.AutomationElement]$Element,
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Text
    )

    if (-not $Element) {
        throw "SEGURANCA: AutomationElement nulo fornecido a Set-ElementText."
    }

    $pattern = $null
    if ($Element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) {
        $valPattern = $pattern -as [System.Windows.Automation.ValuePattern]
        $valPattern.SetValue($Text)
    } else {
        throw "SEGURANCA: O elemento nao suporta ValuePattern para preenchimento protegido de texto."
    }
}

function Assert-NoModifiersStuck {
    # 0x10 = Shift, 0x11 = Control, 0x12 = Menu (Alt), 0x5B = LWin, 0x5C = RWin
    $keys = @(0x10, 0x11, 0x12, 0x5B, 0x5C)
    foreach ($k in $keys) {
        $state = [GuardedWin32]::GetAsyncKeyState($k)
        if (($state -band 0x8000) -ne 0) {
            # Libera preventivamente
            $scan = [byte][GuardedWin32]::MapVirtualKey($k, 0)
            [GuardedWin32]::keybd_event($k, $scan, [GuardedWin32]::KEYEVENTF_KEYUP, [UIntPtr]::Zero)
            throw "SEGURANCA: Tecla modificadora 0x$('{0:X2}' -f $k) detectada como presa!"
        }
    }
    return $true
}

function Send-GuardedKeyHold {
    # Segura a tecla com repeticao automatica (keydown repetido, como o teclado) por HoldMs; Assert-ForegroundOwned antes de CADA evento.
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [byte]$VirtualKey,
        [int]$HoldMs = 1500,
        [int]$RepeatMs = 33,
        [int]$TargetPid = $script:GuardedPid
    )
    $scan = [byte][GuardedWin32]::MapVirtualKey($VirtualKey, 0)
    $down = $false
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        while ($sw.ElapsedMilliseconds -lt $HoldMs) {
            Assert-ForegroundOwned -TargetPid $TargetPid
            [GuardedWin32]::keybd_event($VirtualKey, $scan, 0, [UIntPtr]::Zero)
            $down = $true
            Start-Sleep -Milliseconds $RepeatMs
        }
    } finally {
        if ($down) {
            [GuardedWin32]::keybd_event($VirtualKey, $scan, [GuardedWin32]::KEYEVENTF_KEYUP, [UIntPtr]::Zero)
        }
    }
    Assert-ForegroundOwned -TargetPid $TargetPid
}
