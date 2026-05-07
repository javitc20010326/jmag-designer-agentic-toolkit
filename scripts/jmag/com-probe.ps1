param(
    [string]$ProgId = "designer.Application.181",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Get-Location) "jmag-com-probe-result.json"
}

$scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) ("jmag_probe_" + [guid]::NewGuid().ToString("N") + ".py")
$escapedOutput = $OutputPath.Replace("\", "\\")

@"
# -*- coding: utf-8 -*-
import designer
import json
import time

def safe_call(obj, name):
    try:
        return getattr(obj, name)()
    except Exception as exc:
        return "ERROR: " + str(exc)

app = designer.GetApplication()
data = {
    "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
    "is_valid": safe_call(app, "IsValid"),
    "visible": safe_call(app, "visible"),
    "project_name": safe_call(app, "GetProjectName"),
    "project_path": safe_call(app, "GetProjectPath"),
    "project_folder": safe_call(app, "GetProjectFolderPath"),
    "num_models": safe_call(app, "NumModels"),
    "num_studies": safe_call(app, "NumStudies"),
    "major_version": safe_call(app, "MajorVersion"),
    "minor_version": safe_call(app, "MinorVersion"),
    "sub_version": safe_call(app, "SubVersion"),
    "app_dir": safe_call(app, "GetAppDir"),
    "main_window_title": safe_call(app, "MainWindowTitle"),
    "has_error": safe_call(app, "HasError"),
    "last_message": safe_call(app, "GetLastMessage")
}

with open(r"$escapedOutput", "w") as f:
    json.dump(data, f, indent=2)
"@ | Set-Content -LiteralPath $scriptPath -Encoding ASCII

if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Force
}

$app = New-Object -ComObject $ProgId
$app.RunScriptFile($scriptPath)
Start-Sleep -Seconds 2

if (Test-Path -LiteralPath $OutputPath) {
    Get-Content -LiteralPath $OutputPath -Raw
} else {
    Write-Output "Probe result was not created."
    try {
        Write-Output ("HasError=" + $app.HasError())
        Write-Output ("LastMessage=" + $app.GetLastMessage())
    } catch {
        Write-Output $_.Exception.Message
    }
}

Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue
