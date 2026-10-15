. "$PSScriptRoot\SmokeTest-Helper.ps1"

# Focus Files window
$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)

# Send Home to go to first item
Send-KeyToFiles -Key 0x24 -WaitAfterMs 200 # Home

# Send Down 4 times to land on .gitignore (index 4)
1..4 | ForEach-Object {
    Send-KeyToFiles -Key 0x28 -WaitAfterMs 100 # Down
}

# Send F2 to start rename
Send-KeyToFiles -Key 0x71 -WaitAfterMs 400 # F2

# Capture
Capture-FilesWindow -OutFile 'tools\perf\captures\f001\test_gitignore_f2.png'

# Send Esc to cancel rename
Send-KeyToFiles -Key 0x1B -WaitAfterMs 200 # Esc
