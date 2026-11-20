# Setup do laboratório com 1.600 arquivos reais para reproduzir o cenário do Alexandre (F024)
param([string]$TargetFolder = 'C:\KyraFilesUXLab\perf\1600')

$ErrorActionPreference = 'Stop'

if (Test-Path $TargetFolder) {
    Remove-Item -Recurse -Force $TargetFolder
}
New-Item -ItemType Directory -Force $TargetFolder | Out-Null

Write-Host "Criando 1.600 arquivos em $TargetFolder..."

$exts = @('txt', 'cs', 'json', 'xml', 'md', 'png', 'jpg', 'pdf', 'docx', 'xlsx', 'log', 'zip', 'dll', 'exe', 'ini', 'sql')
$categories = @('relatorio', 'documento', 'foto', 'backup', 'codigo', 'planilha', 'dados', 'config', 'projeto')

for ($i = 1; $i -le 1600; $i++) {
    $cat = $categories[$i % $categories.Length]
    $ext = $exts[$i % $exts.Length]
    $fileName = "{0}\{1}_{2:D4}.{3}" -f $TargetFolder, $cat, $i, $ext
    $content = "Conteudo de teste para arquivo $i ($cat) gerado para benchmark F024."
    [System.IO.File]::WriteAllText($fileName, $content)
}

$count = (Get-ChildItem -File -Path $TargetFolder).Count
Write-Host "Sucesso: $count arquivos criados em $TargetFolder."

