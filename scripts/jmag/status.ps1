$ErrorActionPreference = "Continue"

Write-Output "=== JMAG environment ==="
Write-Output "User: $env:USERDOMAIN\$env:USERNAME"
Write-Output "JMAG_DESIGNER_EXE: $env:JMAG_DESIGNER_EXE"
Write-Output "JMAG_HOME: $env:JMAG_HOME"
Write-Output ""

Write-Output "=== Running JMAG-like processes ==="
Get-Process |
    Where-Object { $_.ProcessName -match 'jmag|designer|scheduler' } |
    Select-Object Id, ProcessName, Path |
    Format-Table -AutoSize

Write-Output ""
Write-Output "=== Executable candidates ==="
$roots = @(
    $env:JMAG_HOME,
    "C:\Program Files",
    "C:\Program Files (x86)"
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

foreach ($root in $roots) {
    Get-ChildItem -LiteralPath $root -Recurse -File -Include "designer.exe","scheduler.exe","jmag*.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match 'JMAG|JSOL|Designer' } |
        Select-Object -First 80 -ExpandProperty FullName
}

Write-Output ""
Write-Output "If no executable appears, set JMAG_DESIGNER_EXE to the full Designer executable path."
