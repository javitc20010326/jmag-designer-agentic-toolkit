# JMAG Designer Agentic Toolkit

Experimental MCP toolkit for controlling and assisting JMAG Designer workflows from Codex, Claude Code, and other local MCP-capable coding agents.

The target version is JMAG Designer 18.1+, but the toolkit is deliberately version-tolerant: it detects installed executables and generates scripts that can be adjusted for newer JMAG releases.

## Why this can work

JMAG publishes scripting support for Designer. The official JMAG pages describe script control of pre-processing, solvers, and post-processing with Python, VBScript, or JScript, including automation from external environments. The public Script Library also includes examples such as `designer.GetApplication()`, `app.NumStudies()`, `study.Run()`, `WriteMeshJcf()`, and `scheduler.exe`.

Useful references:

- [JMAG Script Library](https://www.jmag-international.com/scriptlibrary/)
- [JMAG Designer Scripts](https://www.jmag-international.com/products/jmag-designer/customization/scripts/)
- [Run all studies example](https://www.jmag-international.com/scriptlibrary/d0002_run_all_projects/)
- [JMAG-Scheduler example](https://www.jmag-international.com/scriptlibrary/s8356/)
- [External Python/non-GUI example](https://www.jmag-international.com/scriptlibrary/s8467/)

## Capability modes

- `full-script-agentic`: JMAG Designer is installed and licensed, scripts can run, and the agent can generate/run scripts on a copied project.
- `semi-agentic`: Codex cannot run JMAG directly, but can analyze exported scripts, logs, CSV results, and project folders.
- `advisory`: no local JMAG access; Codex drafts model plans, scripts, checklists, and result analysis logic.

## MCP tools

- `jmag_capabilities`
- `jmag_environment_status`
- `jmag_com_status`
- `jmag_run_script_via_com`
- `jmag_analyze_project_folder`
- `jmag_analyze_csv_results`
- `jmag_generate_script`
- `jmag_generate_run_plan`

## Quick start

```powershell
.\scripts\build.ps1
.\scripts\install-codex.ps1
.\scripts\jmag\status.ps1
.\scripts\jmag\com-probe.ps1
```

Then restart Codex and ask for JMAG tasks, for example:

- "Check whether JMAG is detected."
- "Check JMAG COM automation status."
- "Generate a JMAG script to run all unsolved studies on a copied project."
- "Run this reviewed JMAG script via COM on a copied project."
- "Analyze this folder of JMAG CSV exports."
- "Create a parameter sweep skeleton for rotor/stator dimensions."
- "Generate a run plan for optimizing torque ripple."

## Safety

Do not commit licensed `.jproj` projects, solver result folders, screenshots, or exported confidential data. The `.gitignore` and upload script block common JMAG/project artifacts by default.

First real runs should be done on copied projects.
