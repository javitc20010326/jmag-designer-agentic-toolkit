# GitHub Publishing

The easiest path is:

```powershell
.\upload-to-github.cmd
```

Default target:

```text
javitc20010326/jmag-designer-agentic-toolkit
```

The upload script uses GitHub's REST API and adds `[skip ci]` to each commit by default. It also blocks common JMAG project/result files.

If repository creation fails, create an empty GitHub repository manually with the same name and rerun the script.
