param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root "jmag-designer-agentic-toolkit-source.zip"
}

function Test-IsSourceFile([System.IO.FileInfo]$File) {
    $relative = $File.FullName.Substring($root.Length + 1).Replace('\', '/')
    $blockedExtensions = @(
        ".zip", ".rar", ".7z", ".pdf", ".xlsx", ".xls", ".xlsm",
        ".jproj", ".jmag", ".jcf", ".jdata", ".jplot", ".jmesh", ".jresult", ".jdc",
        ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"
    )

    if ($blockedExtensions -contains $File.Extension.ToLowerInvariant()) {
        return $false
    }

    if ($relative -match '(^|/)(bin|obj|\.git|exports|results|private|scratch|user-projects|licensed-projects)(/|$)') {
        return $false
    }

    return $true
}

$temp = Join-Path ([System.IO.Path]::GetTempPath()) ("jmag-agentic-pack-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object { Test-IsSourceFile $_ } |
        ForEach-Object {
            $relative = $_.FullName.Substring($root.Length + 1)
            $target = Join-Path $temp $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $target
        }

    if (Test-Path -LiteralPath $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force
    }
    Compress-Archive -Path (Join-Path $temp "*") -DestinationPath $OutputPath
    Write-Output "Created $OutputPath"
} finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
