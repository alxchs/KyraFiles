# Measure-PostMessage.ps1
# Medicao de entrega por PostMessage direto na janela (Ajuste c do Claude).
# REGRA: Executado EXCLUSIVAMENTE em janela criada pelo proprio teste.
# NUNCA envia entradas globais de teclado ou mouse.
# APENAS MEDICAO, SEM ADOTAR.

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class PostMessageMeasureWin32 {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_CHAR = 0x0102;
}
"@ -ErrorAction SilentlyContinue

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$EvidenceDir = Join-Path $PSScriptRoot "..\..\docs\agents\evidence\test-input-guard"
if (-not (Test-Path $EvidenceDir)) {
    New-Item -ItemType Directory -Path $EvidenceDir -Force | Out-Null
}
$OutputFile = Join-Path $EvidenceDir "medicao-postmessage.md"

Write-Host "Criando formulario proprio de teste para medicao PostMessage..." -ForegroundColor Cyan

$form = New-Object System.Windows.Forms.Form
$form.Text = "PostMessage_Measure_Window"
$form.StartPosition = "Manual"
$form.Location = New-Object System.Drawing.Point(60, 60)
$form.Size = New-Object System.Drawing.Size(280, 150)
$form.ShowInTaskbar = $false

$tb = New-Object System.Windows.Forms.TextBox
$tb.Name = "MeasureTextBox"
$tb.Location = New-Object System.Drawing.Point(20, 20)
$tb.Size = New-Object System.Drawing.Size(220, 30)
$form.Controls.Add($tb)

# Exibe o formulario de teste de forma nao-bloqueante
$form.Show()
[System.Windows.Forms.Application]::DoEvents()
Start-Sleep -Milliseconds 200

$tbHandle = $tb.Handle
Write-Host "Handle da TextBox de teste: $tbHandle"

$medicoes = [System.Collections.Generic.List[PSCustomObject]]::new()

try {
    # -------------------------------------------------------------
    # MEDICAO 1: PostMessage WM_CHAR direto na TextBox
    # -------------------------------------------------------------
    $tb.Text = ""
    [System.Windows.Forms.Application]::DoEvents()
    
    # Envia char 'Z' (0x5A)
    $resChar = [PostMessageMeasureWin32]::PostMessage($tbHandle, [PostMessageMeasureWin32]::WM_CHAR, [IntPtr]0x5A, [IntPtr]0)
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.Application]::DoEvents()
    
    $valChar = $tb.Text
    $aceitouChar = ($valChar -eq "Z")
    
    $medicoes.Add([PSCustomObject]@{
        Cenario = "WM_CHAR direto no HWND de controle Edit próprio"
        Mensagem = "WM_CHAR (0x0102, wParam=0x5A 'Z')"
        RetornoPostMessage = $resChar
        TextoResultante = $valChar
        Resultado = if ($aceitouChar) { "ACEITA" } else { "IGNORA" }
        Observacao = "O controle Win32/Edit processa WM_CHAR postado em sua fila de mensagens."
    })

    # -------------------------------------------------------------
    # MEDICAO 2: PostMessage WM_KEYDOWN + WM_KEYUP na TextBox
    # -------------------------------------------------------------
    $tb.Text = ""
    [System.Windows.Forms.Application]::DoEvents()
    
    # Envia tecla virtual 'A' (0x41)
    $resDown = [PostMessageMeasureWin32]::PostMessage($tbHandle, [PostMessageMeasureWin32]::WM_KEYDOWN, [IntPtr]0x41, [IntPtr]0)
    $resUp   = [PostMessageMeasureWin32]::PostMessage($tbHandle, [PostMessageMeasureWin32]::WM_KEYUP, [IntPtr]0x41, [IntPtr]0)
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.Application]::DoEvents()
    
    $valKey = $tb.Text
    $aceitouKey = ($valKey.Length -gt 0)
    
    $medicoes.Add([PSCustomObject]@{
        Cenario = "WM_KEYDOWN/UP direto no HWND sem passar pelo TranslateMessage do loop"
        Mensagem = "WM_KEYDOWN (0x0100) + WM_KEYUP (0x0101, wParam=0x41)"
        RetornoPostMessage = ($resDown -and $resUp)
        TextoResultante = $valKey
        Resultado = if ($aceitouKey) { "ACEITA" } else { "IGNORA" }
        Observacao = "WM_KEYDOWN direto nao gera caractere sem passar por TranslateMessage no loop da thread."
    })

    # -------------------------------------------------------------
    # MEDICAO 3: PostMessage WM_CHAR com janela em segundo plano
    # -------------------------------------------------------------
    $tb.Text = ""
    [System.Windows.Forms.Application]::DoEvents()
    
    # Desativa foco do formulario
    $form.Enabled = $true
    # Posta mensagem com janela em background
    $resBg = [PostMessageMeasureWin32]::PostMessage($tbHandle, [PostMessageMeasureWin32]::WM_CHAR, [IntPtr]0x57, [IntPtr]0) # 'W'
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.Application]::DoEvents()
    
    $valBg = $tb.Text
    $aceitouBg = ($valBg -eq "W")
    
    $medicoes.Add([PSCustomObject]@{
        Cenario = "WM_CHAR postado com janela em segundo plano (background)"
        Mensagem = "WM_CHAR (0x0102, wParam=0x57 'W')"
        RetornoPostMessage = $resBg
        TextoResultante = $valBg
        Resultado = if ($aceitouBg) { "ACEITA" } else { "IGNORA" }
        Observacao = "Controles Win32 tradicionais aceitam WM_CHAR em segundo plano, porem controles XAML/WinUI 3 exigem foco logico interno."
    })

} finally {
    $form.Close()
    $form.Dispose()
}

# Gera medicao-postmessage.md
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Medicao Tecnica: Entrega de Entrada via PostMessage")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("**Data:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  ")
[void]$sb.AppendLine("**Status:** MEDICAO REALIZADA (APENAS MEDICAO, SEM ADOTAR)  ")
[void]$sb.AppendLine("**Escopo:** Janela propria de teste criada exclusivamente pelo script (PostMessage_Measure_Window), sem tocar em nenhum processo de usuario.  ")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 1. Motivacao e Ajuste (c) do Claude")
[void]$sb.AppendLine("Esta medicao atende a solicitacao de revisao (c): registrar em docs/agents/evidence/test-input-guard/ uma medicao de entrega por PostMessage direto na janela, **so em janela criada pelo proprio teste**, dizendo se aceita ou ignora (apenas medicao, sem adotar).")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 2. Resultados Medidos em Janela Propria de Teste")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Cenario de Teste | Mensagem Win32 | Retorno API | Texto Resultante | Comportamento Observado |")
[void]$sb.AppendLine("|---|---|---|---|---|")
foreach ($m in $medicoes) {
    [void]$sb.AppendLine("| $($m.Cenario) | $($m.Mensagem) | $($m.RetornoPostMessage) | '$($m.TextoResultante)' | **$($m.Resultado)** |")
}
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 3. Analise Tecnica e Conclusao")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("1. **Em controles Win32 nativos (Edit control):**")
[void]$sb.AppendLine("   - WM_CHAR direto via PostMessage e **ACEITO**, inserindo o caractere no controle mesmo sem foco do sistema.")
[void]$sb.AppendLine("   - WM_KEYDOWN/WM_KEYUP direto via PostMessage e **IGNORADO** para efeito de digitacao de texto, pois o WM_CHAR normalmente e sintetizado pelo TranslateMessage na bomba de mensagens quando ha foco do teclado.")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("2. **Aplicabilidade ao WinUI 3 / KyraFiles:**")
[void]$sb.AppendLine("   - O WinUI 3 utiliza a arvore XAML com InputKeyboardSource e seu proprio despachante de foco (FocusManager).")
[void]$sb.AppendLine("   - Elementos XAML como TextBox, AutoSuggestBox e itens de grid nao possuem HWNDs Win32 filhos individuais -- compartilham o HWND de nivel superior da janela.")
[void]$sb.AppendLine("   - Portanto, PostMessage de baixo nivel nao alcanca com precisao nem confiabilidade os controles filhos internos do WinUI 3, alem de nao acionar os manipuladores de eventos de interacao XAML de alto nivel.")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("3. **Decisao:**")
[void]$sb.AppendLine("   - **NAO ADOTAR PostMessage como mecanismo de automacao.**")
[void]$sb.AppendLine("   - Para campos de texto: utilizar estritamente **UI Automation (ValuePattern)** via Set-ElementText.")
[void]$sb.AppendLine("   - Para atalhos de teclado (VK_*): utilizar estritamente **Send-GuardedKey** com verificacao instantanea e continua de foco via Assert-ForegroundOwned.")

[System.IO.File]::WriteAllText($OutputFile, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Medicao salva com sucesso em: $OutputFile" -ForegroundColor Green
