# JMAG Scripting Notes

JMAG supports automation through scripting. Public examples show:

- `import designer` and `designer.GetApplication()` from the built-in script editor;
- `study.Run()` to run studies;
- `app.GetModel(0).GetStudy(0).WriteMeshJcf(...)` followed by `scheduler.exe` for scheduler workflows;
- external Python automation using `from jmag.designer import designer` and `designer.CreateApplication(...)` on configured systems.

The exact API surface can vary by JMAG version, language, installed modules, and license. Generated scripts are therefore starters, not blind production patches.

Preferred first-run process:

1. Copy the JMAG project.
2. Generate a simple `current_study_report` script.
3. Run it in JMAG Designer's script editor.
4. Move to `run_all_studies` or scheduler scripts after basic script execution is confirmed.
