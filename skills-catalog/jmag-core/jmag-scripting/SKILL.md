# JMAG Scripting Skill

Use this skill when the user asks to automate, inspect, generate scripts for, or analyze results from JMAG Designer.

## Workflow

1. Determine the capability mode:
   - full script-agentic if JMAG Designer and license are available;
   - semi-agentic if only exports/scripts/results are available;
   - advisory if there is no local JMAG access.
2. Prefer MCP tools:
   - `jmag_environment_status` before direct automation;
   - `jmag_analyze_project_folder` for copied project/export folders;
   - `jmag_generate_script` for JMAG Python script starters;
   - `jmag_analyze_csv_results` for exported results.
3. Treat generated scripts as engineering artifacts that require review before running.
4. Never ask the user to upload licensed `.jproj` projects to a public repo.
5. For first validation, generate `current_study_report` before any script that modifies or runs a model.

## Useful prompts

- "Generate a JMAG script to report the current study."
- "Generate a run-all-studies script for a copied project."
- "Analyze this folder of JMAG result CSV files."
- "Create a run plan for torque ripple optimization."
- "Create a parameter sweep skeleton for this design variable."
