$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$serverDll = Join-Path $root "src\JmagDesignerAgenticToolkit.McpServer\bin\Debug\net8.0\JmagDesignerAgenticToolkit.McpServer.dll"

if (-not (Test-Path -LiteralPath $serverDll)) {
    & (Join-Path $PSScriptRoot "build.ps1")
}

$configDir = Join-Path $env:USERPROFILE ".codex"
$configPath = Join-Path $configDir "config.toml"
New-Item -ItemType Directory -Path $configDir -Force | Out-Null

$snippet = @"

[mcp_servers.jmag-designer-agentic-toolkit]
command = "dotnet"
args = ["$($serverDll.Replace('\','\\'))"]
"@

Write-Output "Add this block to $configPath if Codex has not registered it automatically:"
Write-Output $snippet
Write-Output ""
Write-Output "Restart Codex after editing the config."
