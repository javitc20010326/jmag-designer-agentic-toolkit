$ErrorActionPreference = "Stop"

$repoDefault = "javitc20010326/jmag-designer-agentic-toolkit"
$repo = Read-Host "GitHub repo owner/name [$repoDefault]"
if ([string]::IsNullOrWhiteSpace($repo)) {
    $repo = $repoDefault
}

$token = Read-Host "Paste GitHub token here. It will stay only in this PowerShell process" -AsSecureString
$plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($token))
$env:GITHUB_TOKEN = $plain

try {
    & (Join-Path $PSScriptRoot "upload-github.ps1") -Repository $repo -CreateIfMissing
} finally {
    $env:GITHUB_TOKEN = $null
}

Read-Host "Done. Press Enter to close"
