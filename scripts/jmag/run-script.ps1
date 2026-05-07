param(
    [Parameter(Mandatory = $true)]
    [string]$ScriptPath,

    [string]$Python = "python",

    [switch]$ExternalPython
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ScriptPath)) {
    throw "Script not found: $ScriptPath"
}

Write-Output "Script: $ScriptPath"
Write-Output "JMAG_DESIGNER_EXE: $env:JMAG_DESIGNER_EXE"

if ($ExternalPython) {
    Write-Output "Running through external Python. This requires the JMAG Python environment to be configured."
    & $Python $ScriptPath
    exit $LASTEXITCODE
}

Write-Output ""
Write-Output "Default mode is safe/manual:"
Write-Output "1. Open JMAG Designer."
Write-Output "2. Open the generated .py script in the JMAG script editor."
Write-Output "3. Run it against a copied project."
Write-Output ""
Write-Output "If your JMAG installation supports external Python automation, rerun with -ExternalPython."
