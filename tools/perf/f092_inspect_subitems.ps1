# tools/perf/f092_inspect_subitems.ps1
# Script de diagnóstico para F092 T0: inspeção de subitens de hd-roteador e alxchs@gmail.com (lenovo)
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

$sh = New-Object -ComObject Shell.Application
$ns17 = $sh.NameSpace(17)

Write-Output "=== NAVEGANDO ITENS FILHOS DE hd-roteador ==="
$hdRoteadorItem = $null
foreach ($it in $ns17.Items()) {
    if ($it.Name -eq 'hd-roteador') {
        $hdRoteadorItem = $it
        break
    }
}

if ($null -ne $hdRoteadorItem) {
    Write-Output "hd-roteador encontrado. Path: $($hdRoteadorItem.Path)"
    $folder = $hdRoteadorItem.GetFolder
    if ($null -ne $folder) {
        Write-Output "Folder Title: $($folder.Title)"
        Write-Output "Contagem de subitens: $($folder.Items().Count)"
        foreach ($sub in $folder.Items()) {
            [PSCustomObject]@{
                Name = $sub.Name
                Path = $sub.Path
                Type = $sub.Type
                IsFolder = $sub.IsFolder
                IsFileSystem = $sub.IsFileSystem
            } | Format-List | Out-String | Write-Output
            
            # Tentar 1 nível aprofundado se for pasta
            if ($sub.IsFolder) {
                $subFolder = $sub.GetFolder
                if ($null -ne $subFolder) {
                    Write-Output "   Sub-subitens em '$($sub.Name)': $($subFolder.Items().Count)"
                    foreach ($sub2 in $subFolder.Items() | Select-Object -First 3) {
                        Write-Output "     - $($sub2.Name) (Type: $($sub2.Type), IsFolder: $($sub2.IsFolder), Path: $($sub2.Path))"
                    }
                }
            }
        }
    } else {
        Write-Output "GetFolder retornou nulo para hd-roteador."
    }
} else {
    Write-Output "hd-roteador não encontrado no Namespace 17."
}

Write-Output "=== TESTANDO SE Shell.Application.NameSpace CONSEGUE ABRIR DIRETAMENTE PELO PATH ==="
if ($null -ne $hdRoteadorItem) {
    $directNs = $sh.NameSpace($hdRoteadorItem.Path)
    if ($null -ne $directNs) {
        Write-Output "Sucesso ao abrir NameSpace($($hdRoteadorItem.Path)): Title = $($directNs.Title), Count = $($directNs.Items().Count)"
    } else {
        Write-Output "Falha ao abrir NameSpace($($hdRoteadorItem.Path)) diretamente."
    }
}
