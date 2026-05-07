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
$roots = @()
if ($env:JMAG_HOME -and (Test-Path -LiteralPath $env:JMAG_HOME)) {
    $roots += $env:JMAG_HOME
}
foreach ($base in @("C:\Program Files", "C:\Program Files (x86)")) {
    if (Test-Path -LiteralPath $base) {
        $roots += Get-ChildItem -LiteralPath $base -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^JMAG|JSOL|JMAG-Designer' } |
            ForEach-Object { $_.FullName }
    }
}

foreach ($root in $roots) {
    Get-ChildItem -LiteralPath $root -Recurse -File -Include "designer.exe","scheduler.exe","jmag*.exe" -ErrorAction SilentlyContinue |
        Select-Object -First 80 -ExpandProperty FullName
}

Write-Output ""
Write-Output "=== COM automation ==="
foreach ($progId in @("designer.Application.181", "designer.Application", "DesignerStarter.InstanceManager.181", "DesignerStarter.InstanceManager")) {
    try {
        $type = [type]::GetTypeFromProgID($progId)
        if ($type) {
            Write-Output "$progId registered"
        } else {
            Write-Output "$progId not registered"
        }
    } catch {
        Write-Output "$progId error: $($_.Exception.Message)"
    }
}

Write-Output ""
Write-Output "If no executable appears, set JMAG_DESIGNER_EXE to the full Designer executable path."
