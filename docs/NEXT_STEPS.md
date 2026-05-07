# Next Steps

Recommended validation on a licensed JMAG 18.1 machine:

1. Run `scripts/jmag/status.ps1`.
2. Register MCP with Codex.
3. Ask for `jmag_environment_status`.
4. Generate `current_study_report`.
5. Run it inside JMAG Designer.
6. Generate `run_all_studies` against a copied project.
7. Export a small CSV result and run `jmag_analyze_csv_results`.

Future improvements:

- add verified JMAG 18.1-specific script templates;
- add result plotting helpers;
- add known motor/electromagnetic study templates;
- add regression tests around sample exported CSV files;
- add optional external Python automation when the user's JMAG installation supports it.
