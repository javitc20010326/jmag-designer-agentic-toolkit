# Getting Started

## 1. Build

```powershell
.\scripts\build.ps1
```

If Windows blocks execution policy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

## 2. Register with Codex

```powershell
.\scripts\install-codex.ps1
```

Copy the printed MCP block into `%USERPROFILE%\.codex\config.toml`, then restart Codex.

## 3. Check JMAG

```powershell
.\scripts\jmag\status.ps1
```

If JMAG is not found automatically, set:

```powershell
$env:JMAG_DESIGNER_EXE = "C:\Program Files\JMAG-Designer18.1\designer.exe"
$env:JMAG_HOME = "C:\Program Files\JMAG-Designer18.1"
```

Adjust the path to your real installation.

## 4. First useful test

Ask Codex:

> Use the JMAG toolkit. Generate a current study report script in a temp folder.

Open the generated `.py` inside JMAG Designer's script editor and run it.

## 5. Upload to GitHub

Run:

```powershell
.\upload-to-github.cmd
```

Paste your token only into the PowerShell window. The script can create `javitc20010326/jmag-designer-agentic-toolkit` if the token allows repository creation.
