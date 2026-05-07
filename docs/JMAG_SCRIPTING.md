# JMAG Scripting Notes

JMAG supports automation through scripting. Public examples show:

- `import designer` and `designer.GetApplication()` from the built-in script editor;
- `study.Run()` to run studies;
- `app.GetModel(0).GetStudy(0).WriteMeshJcf(...)` followed by `scheduler.exe` for scheduler workflows;
- external Python automation using `from jmag.designer import designer` and `designer.CreateApplication(...)` on configured systems.
- Windows COM automation through registered ProgIDs such as `designer.Application.181`, with methods exposed by JMAG including `RunScriptFile`, `Load`, `SaveAs`, `NumStudies`, `GetCurrentStudy`, `CreateMaterial`, and `ExportImage`.

The exact API surface can vary by JMAG version, language, installed modules, and license. Generated scripts are therefore starters, not blind production patches.

Preferred first-run process:

1. Copy the JMAG project.
2. Run `scripts/jmag/com-probe.ps1`.
3. Generate a simple `current_study_report` script.
4. Run it in JMAG Designer's script editor or through `jmag_run_script_via_com`.
5. Move to `run_all_studies` or scheduler scripts after basic script execution is confirmed.
