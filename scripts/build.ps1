param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    dotnet restore .\JmagDesignerAgenticToolkit.sln --configfile .\NuGet.Config
    dotnet build .\JmagDesignerAgenticToolkit.sln --configuration $Configuration --no-restore
} finally {
    Pop-Location
}
