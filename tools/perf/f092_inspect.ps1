# tools/perf/f092_inspect.ps1
# Script de diagnóstico para F092 T0: inspeção de namespaces do Shell e DelegateFolders
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

$sh = New-Object -ComObject Shell.Application

Write-Output "=== NAMESPACE 17 (This PC / ssfDRIVES) ==="
$ns17 = $sh.NameSpace(17)
if ($null -ne $ns17) {
    Write-Output "Title: $($ns17.Title)"
    $items17 = @()
    foreach ($item in $ns17.Items()) {
        $items17 += [PSCustomObject]@{
            Name = $item.Name
            Path = $item.Path
            Type = $item.Type
            IsFileSystem = $item.IsFileSystem
            IsFolder = $item.IsFolder
        }
    }
    $items17 | Format-Table -AutoSize | Out-String | Write-Output
}

Write-Output "=== NAMESPACE 18 (Network / ssfNETWORK) ==="
$ns18 = $sh.NameSpace(18)
if ($null -ne $ns18) {
    Write-Output "Title: $($ns18.Title)"
    $items18 = @()
    foreach ($item in $ns18.Items()) {
        $items18 += [PSCustomObject]@{
            Name = $item.Name
            Path = $item.Path
            Type = $item.Type
            IsFileSystem = $item.IsFileSystem
            IsFolder = $item.IsFolder
        }
    }
    $items18 | Format-Table -AutoSize | Out-String | Write-Output
}

Write-Output "=== REGISTRY: MyComputer NameSpace DelegateFolders ==="
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\DelegateFolders"
if (Test-Path $regPath) {
    Get-ChildItem -Path $regPath | ForEach-Object {
        $keyName = $_.PSChildName
        $defVal = (Get-ItemProperty -Path $_.PSPath -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
        [PSCustomObject]@{
            GUID = $keyName
            DefaultValue = $defVal
        }
    } | Format-Table -AutoSize | Out-String | Write-Output
} else {
    Write-Output "Registry path not found."
}

Write-Output "=== DETALHES DE CADA ITEM EM NAMESPACE 17 ==="
if ($null -ne $ns17) {
    foreach ($item in $ns17.Items()) {
        $subCount = -1
        $canGetFolder = $false
        $errorMsg = ""
        try {
            $folder = $item.GetFolder
            if ($null -ne $folder) {
                $canGetFolder = $true
                $subCount = $folder.Items().Count
            }
        } catch {
            $errorMsg = $_.Exception.Message
        }

        [PSCustomObject]@{
            Name = $item.Name
            Path = $item.Path
            Type = $item.Type
            IsFileSystem = $item.IsFileSystem
            IsFolder = $item.IsFolder
            CanGetFolder = $canGetFolder
            SubItemsCount = $subCount
            Error = $errorMsg
        } | Format-List | Out-String | Write-Output
    }
}
