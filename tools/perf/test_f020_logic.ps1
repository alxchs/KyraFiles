# Teste unitario/logico das regras da F020 (Normalizacao de caminho, aba unica e Settings na posicao 0)
param()

$tests = @(
    # AC-2: Diferencas de caixa e barra final
    @{ Input = "c:\windows"; Expected = "C:\WINDOWS"; Desc = "Lowercase drive and folder" },
    @{ Input = "C:\WINDOWS"; Expected = "C:\WINDOWS"; Desc = "Uppercase folder" },
    @{ Input = "C:\Windows\"; Expected = "C:\WINDOWS"; Desc = "Trailing backslash" },
    @{ Input = "c:/windows/"; Expected = "C:\WINDOWS"; Desc = "Forward slashes and trailing slash" },
    @{ Input = "C:\"; Expected = "C:\"; Desc = "Drive root" },
    @{ Input = "c:"; Expected = "C:\"; Desc = "Drive letter without slash" },
    @{ Input = "d:/"; Expected = "D:\"; Desc = "Drive letter with forward slash" },
    
    # AC-5: Home
    @{ Input = $null; Expected = "Home"; Desc = "Null path" },
    @{ Input = ""; Expected = "Home"; Desc = "Empty string" },
    @{ Input = "   "; Expected = "Home"; Desc = "Whitespace" },
    @{ Input = "Home"; Expected = "Home"; Desc = "Home keyword" },
    @{ Input = "home"; Expected = "Home"; Desc = "Lowercase home" },
    
    # AC-3 e AC-4: Settings
    @{ Input = "Settings"; Expected = "Settings"; Desc = "Settings keyword" },
    @{ Input = "settings"; Expected = "Settings"; Desc = "Lowercase settings" },
    
    # ReleaseNotes
    @{ Input = "ReleaseNotes"; Expected = "ReleaseNotes"; Desc = "ReleaseNotes keyword" },
    @{ Input = "releasenotes"; Expected = "ReleaseNotes"; Desc = "Lowercase releasenotes" }
)

function Normalize-NavPath([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return "Home" }
    $trimmed = $path.Trim()
    if ($trimmed.EndsWith("\?")) { $trimmed = $trimmed.Substring(0, $trimmed.Length - 1) }
    if ($trimmed -eq "Home" -or $trimmed -eq "home") { return "Home" }
    if ($trimmed -eq "Settings" -or $trimmed -eq "settings") { return "Settings" }
    if ($trimmed -eq "ReleaseNotes" -or $trimmed -eq "releasenotes") { return "ReleaseNotes" }

    $trimmed = $trimmed.Replace('/', '\')
    if ($trimmed.Length -eq 2 -and $trimmed[1] -eq ':') {
        return "$([char]::ToUpperInvariant($trimmed[0])):\"
    }
    if ($trimmed.Length -eq 3 -and $trimmed[1] -eq ':' -and $trimmed[2] -eq '\') {
        return "$([char]::ToUpperInvariant($trimmed[0])):\"
    }

    $clean = $trimmed.TrimEnd('\')
    if ($clean.Length -eq 2 -and $clean[1] -eq ':') {
        return "$([char]::ToUpperInvariant($clean[0])):\"
    }
    return $clean.ToUpperInvariant()
}

$passed = 0
$failed = 0

Write-Host "Executando testes logicos da F020..."
foreach ($t in $tests) {
    $actual = Normalize-NavPath $t.Input
    if ($actual -eq $t.Expected) {
        Write-Host "  [PASS] $($t.Desc): '$($t.Input)' -> '$actual'" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  [FAIL] $($t.Desc): '$($t.Input)' -> esperado '$($t.Expected)', obtido '$actual'" -ForegroundColor Red
        $failed++
    }
}

Write-Host "`nResumo: $passed passaram, $failed falharam."
if ($failed -gt 0) { exit 1 } else { exit 0 }
