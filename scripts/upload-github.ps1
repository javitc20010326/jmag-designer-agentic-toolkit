param(
    [string]$Repository = "javitc20010326/jmag-designer-agentic-toolkit",
    [string]$Branch = "main",
    [switch]$RunCi,
    [switch]$CreateIfMissing
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
    Write-Error "GITHUB_TOKEN is not set. Set it only in your local PowerShell session before running this script."
}

$root = Split-Path -Parent $PSScriptRoot
$headers = @{
    Authorization = "Bearer $env:GITHUB_TOKEN"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

function ConvertTo-Base64Utf8([string]$Text) {
    [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Text))
}

function Test-RepositoryExists {
    try {
        Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$Repository" -Headers $headers | Out-Null
        return $true
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            return $false
        }
        throw
    }
}

function New-RepositoryIfAllowed {
    $parts = $Repository.Split("/")
    if ($parts.Length -ne 2) {
        throw "Repository must use owner/name format."
    }

    $body = @{
        name = $parts[1]
        private = $false
        description = "Agentic MCP toolkit for JMAG Designer automation."
        auto_init = $false
    } | ConvertTo-Json -Depth 5

    Invoke-RestMethod -Method Post -Uri "https://api.github.com/user/repos" -Headers $headers -Body $body -ContentType "application/json" | Out-Null
    Write-Output "Created repository https://github.com/$Repository"
}

function Get-RemoteFileSha([string]$Path) {
    $encodedPath = ($Path -replace '\\','/')
    $uri = "https://api.github.com/repos/$Repository/contents/$encodedPath"
    if (-not [string]::IsNullOrWhiteSpace($Branch)) {
        $uri += "?ref=$Branch"
    }

    try {
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
        return $response.sha
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            return $null
        }
        throw
    }
}

function Test-IsUploadableSourceFile([System.IO.FileInfo]$File) {
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

if (-not (Test-RepositoryExists)) {
    if ($CreateIfMissing) {
        New-RepositoryIfAllowed
    } else {
        throw "Repository https://github.com/$Repository does not exist. Create it on GitHub or rerun with -CreateIfMissing."
    }
}

$files = Get-ChildItem -LiteralPath $root -Recurse -File |
    Where-Object { Test-IsUploadableSourceFile $_ } |
    Sort-Object FullName

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
    $sha = Get-RemoteFileSha $relativePath

    $body = @{
        message = if ($sha) { "Update $relativePath" } else { "Add $relativePath" }
        content = ConvertTo-Base64Utf8 $content
        branch = $Branch
    }
    if (-not $RunCi) {
        $body.message += " [skip ci]"
    }
    if ($sha) {
        $body.sha = $sha
    }

    $json = $body | ConvertTo-Json -Depth 5
    $uri = "https://api.github.com/repos/$Repository/contents/$relativePath"
    Invoke-RestMethod -Method Put -Uri $uri -Headers $headers -Body $json -ContentType "application/json" | Out-Null
    Write-Output "Uploaded $relativePath"
}

Write-Output ""
Write-Output "Upload complete: https://github.com/$Repository"
