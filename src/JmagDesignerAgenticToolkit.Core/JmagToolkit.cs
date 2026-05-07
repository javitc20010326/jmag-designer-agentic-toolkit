using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JmagDesignerAgenticToolkit.Core;

public sealed class JmagToolkit
{
    private static readonly string[] TextExtensions =
    [
        ".py", ".vbs", ".js", ".json", ".xml", ".txt", ".csv", ".log", ".md", ".bat", ".ps1"
    ];

    private static readonly string[] ProjectExtensions =
    [
        ".jproj", ".jmag", ".jcf"
    ];

    public object GetCapabilities() => new
    {
        toolkit = "jmag-designer-agentic-toolkit",
        target = "JMAG Designer 18.1+",
        modes = new[]
        {
            new
            {
                name = "full-script-agentic",
                requirements = new[] { "JMAG Designer installed", "valid JMAG license", "script execution enabled", "local Codex/Claude process can start or attach to JMAG scripts" },
                capabilities = new[] { "generate JMAG Python scripts", "run/open project scripts through local JMAG", "batch study execution via scheduler where supported", "export/analyze CSV results" }
            },
            new
            {
                name = "semi-agentic",
                requirements = new[] { "project exports, scripts, logs, CSV result files, or screenshots" },
                capabilities = new[] { "review models from exported metadata", "generate scripts for manual execution", "analyze result CSV/log files", "produce runbooks and parameter sweep plans" }
            },
            new
            {
                name = "advisory",
                requirements = new[] { "no local JMAG access" },
                capabilities = new[] { "design studies conceptually", "draft scripts and checklists", "review exported results only" }
            }
        },
        safety = new[]
        {
            "Generated scripts default to explicit file paths and comments before destructive operations.",
            "Project binaries and license data are excluded from source upload by default.",
            "The first real JMAG run should be done on a copied project, not the original."
        }
    };

    public object GetEnvironmentStatus()
    {
        var executables = FindJmagExecutables().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        var processes = Process.GetProcesses()
            .Where(p => p.ProcessName.Contains("jmag", StringComparison.OrdinalIgnoreCase) ||
                        p.ProcessName.Contains("designer", StringComparison.OrdinalIgnoreCase) ||
                        p.ProcessName.Contains("scheduler", StringComparison.OrdinalIgnoreCase))
            .Select(p => new { p.Id, name = p.ProcessName })
            .OrderBy(p => p.name)
            .ToArray();

        return new
        {
            isJmagLikelyInstalled = executables.Length > 0,
            comAutomation = GetComStatus(),
            environment = new
            {
                JMAG_DESIGNER_EXE = Environment.GetEnvironmentVariable("JMAG_DESIGNER_EXE"),
                JMAG_HOME = Environment.GetEnvironmentVariable("JMAG_HOME")
            },
            executableCandidates = executables,
            runningProcesses = processes,
            nextStep = executables.Length == 0
                ? "Set JMAG_DESIGNER_EXE to the Designer executable path or run scripts/jmag/status.ps1 on the licensed machine."
                : "Use jmag_generate_script, then run it from JMAG Designer or scripts/jmag/run-script.ps1."
        };
    }

    public object GetComStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new { supported = false, reason = "COM automation is Windows-only." };
        }

        var progIds = new[] { "designer.Application.181", "designer.Application", "DesignerStarter.InstanceManager.181", "DesignerStarter.InstanceManager" };
#pragma warning disable CA1416
        var available = progIds
            .Select(progId => new { progId, registered = IsComProgIdRegistered(progId) })
            .ToArray();
#pragma warning restore CA1416

        return new
        {
            supported = true,
            available,
            recommendedProgId = available.FirstOrDefault(x => x.registered && x.progId.StartsWith("designer.Application", StringComparison.OrdinalIgnoreCase))?.progId
        };
    }

    public object RunScriptViaCom(string scriptPath, bool visible = false, int waitSeconds = 2, string? progId = null)
    {
        var script = RequireExistingFile(scriptPath);
        if (!OperatingSystem.IsWindows())
        {
            return new { error = "COM automation is Windows-only." };
        }

        var selectedProgId = string.IsNullOrWhiteSpace(progId) ? "designer.Application.181" : progId;
        var type = Type.GetTypeFromProgID(selectedProgId, throwOnError: false) ??
                   Type.GetTypeFromProgID("designer.Application", throwOnError: false);

        if (type is null)
        {
            return new
            {
                error = "JMAG Designer COM ProgID is not registered.",
                hint = "Run JMAG's VBLink.bat as administrator if COM registration is missing."
            };
        }

        object app = Activator.CreateInstance(type)!;
        TryInvoke(type, app, "SetVisible", visible);
        TryInvoke(type, app, "RunScriptFile", script.FullName);
        Thread.Sleep(TimeSpan.FromSeconds(Math.Clamp(waitSeconds, 0, 120)));

        return new
        {
            progId = selectedProgId,
            script = script.FullName,
            isValid = TryInvoke(type, app, "IsValid"),
            visible = TryInvoke(type, app, "visible"),
            projectName = TryInvoke(type, app, "GetProjectName"),
            projectPath = TryInvoke(type, app, "GetProjectPath"),
            projectFolder = TryInvoke(type, app, "GetProjectFolderPath"),
            numModels = TryInvoke(type, app, "NumModels"),
            numStudies = TryInvoke(type, app, "NumStudies"),
            majorVersion = TryInvoke(type, app, "MajorVersion"),
            minorVersion = TryInvoke(type, app, "MinorVersion"),
            subVersion = TryInvoke(type, app, "SubVersion"),
            mainWindowTitle = TryInvoke(type, app, "MainWindowTitle"),
            hasError = TryInvoke(type, app, "HasError"),
            lastMessage = TryInvoke(type, app, "GetLastMessage")
        };
    }

    public object AnalyzeProjectFolder(string folderPath, int maxFiles = 300)
    {
        var folder = RequireExistingDirectory(folderPath);
        var files = folder.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(f => !IsIgnoredPath(f.FullName))
            .Take(Math.Clamp(maxFiles, 1, 5000))
            .ToArray();

        var extensionCounts = files
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Extension) ? "(no extension)" : f.Extension.ToLowerInvariant())
            .Select(g => new { extension = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ThenBy(x => x.extension)
            .ToArray();

        var candidates = files
            .Where(f => ProjectExtensions.Contains(f.Extension.ToLowerInvariant()))
            .Select(f => DescribeFile(folder.FullName, f))
            .ToArray();

        var scripts = files
            .Where(f => new[] { ".py", ".vbs", ".js" }.Contains(f.Extension.ToLowerInvariant()))
            .Select(f => DescribeFile(folder.FullName, f))
            .ToArray();

        var csv = files
            .Where(f => f.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            .Select(f => DescribeCsv(folder.FullName, f))
            .ToArray();

        var textSignals = files
            .Where(f => TextExtensions.Contains(f.Extension.ToLowerInvariant()))
            .SelectMany(f => ScanTextSignals(folder.FullName, f))
            .Take(80)
            .ToArray();

        return new
        {
            folder = folder.FullName,
            scannedFiles = files.Length,
            extensionCounts,
            jmagProjectCandidates = candidates,
            scripts,
            csvResults = csv,
            textSignals,
            recommendation = candidates.Length > 0
                ? "Use a copied project and generate a script with jmag_generate_script. Do not commit project binaries."
                : "No JMAG project file was detected. Provide script/log/CSV exports for semi-agentic analysis."
        };
    }

    public object AnalyzeCsvResults(string filePath, int maxRows = 20)
    {
        var file = RequireExistingFile(filePath);
        var lines = File.ReadLines(file.FullName).Take(Math.Clamp(maxRows + 1, 2, 1000)).ToArray();
        if (lines.Length == 0)
        {
            return new { file = file.FullName, error = "CSV is empty." };
        }

        var headers = SplitCsvLine(lines[0]);
        var rows = lines.Skip(1).Select(SplitCsvLine).ToArray();
        var numericColumns = new List<object>();

        for (var i = 0; i < headers.Length; i++)
        {
            var values = rows
                .Select(r => i < r.Length ? r[i] : "")
                .Select(v => double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : (double?)null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (values.Length > 0)
            {
                numericColumns.Add(new
                {
                    name = headers[i],
                    samples = values.Length,
                    min = values.Min(),
                    max = values.Max(),
                    average = values.Average()
                });
            }
        }

        return new
        {
            file = file.FullName,
            headers,
            previewRows = rows.Take(10),
            numericColumns,
            nextStep = "Ask Codex to plot, compare, or optimize against these exported JMAG result columns."
        };
    }

    public object GenerateScript(string outputFolder, string scriptKind, string? projectPath = null, string? outputPath = null, string? studyName = null)
    {
        var folder = Directory.CreateDirectory(outputFolder);
        var normalizedKind = NormalizeIdentifier(scriptKind);
        var fileName = normalizedKind + ".py";
        var target = Path.Combine(folder.FullName, fileName);
        var script = BuildScript(normalizedKind, projectPath, outputPath, studyName);
        File.WriteAllText(target, script, new UTF8Encoding(false));

        return new
        {
            script = target,
            scriptKind = normalizedKind,
            projectPath,
            outputPath,
            run = "Open JMAG Designer and run this script from the script editor, or test scripts/jmag/run-script.ps1 on the licensed PC.",
            warning = "Review generated scripts on a copied project before running against production models."
        };
    }

    public object GenerateRunPlan(string outputFolder, string? projectPath, string? objective, string? notes)
    {
        var folder = Directory.CreateDirectory(outputFolder);
        var path = Path.Combine(folder.FullName, "JMAG_RUN_PLAN.md");
        var text = $$"""
        # JMAG Agentic Run Plan

        ## Target

        - Project: {{projectPath ?? "(not provided)"}}
        - Objective: {{objective ?? "Review, automate, solve, and export JMAG results."}}

        ## Preconditions

        - Work on a copy of the `.jproj` project.
        - Confirm JMAG Designer 18.1 starts normally with your license.
        - Confirm scripts can run in JMAG Designer.
        - Export results to CSV or table files for Codex analysis.

        ## Agentic workflow

        1. Run `jmag_environment_status`.
        2. Run `jmag_analyze_project_folder` on a folder containing only copied project/export files.
        3. Ask for a concrete script, for example: generate a run-all-studies script, a response export script, or a parameter sweep skeleton.
        4. Run the generated script in JMAG Designer.
        5. Feed exported CSV/log files back into `jmag_analyze_csv_results` or Codex for interpretation.

        ## Notes

        {{notes ?? "(none)"}}
        """;

        File.WriteAllText(path, text, new UTF8Encoding(false));
        return new { runPlan = path };
    }

    private static string BuildScript(string kind, string? projectPath, string? outputPath, string? studyName)
    {
        var projectLiteral = ToPythonString(projectPath);
        var outputLiteral = ToPythonString(outputPath);
        var studyLiteral = ToPythonString(studyName);

        return kind switch
        {
            "run_all_studies" => $$"""
                # -*- coding: utf-8 -*-
                # Generated by jmag-designer-agentic-toolkit.
                # Run inside JMAG Designer. Test on a copied project first.
                import designer

                app = designer.GetApplication()
                project_path = {{projectLiteral}}
                if project_path:
                    app.Load(project_path)

                count = 0
                total = app.NumStudies()
                while count < total:
                    study = app.GetStudy(count)
                    if study.HasResult() != 1:
                        study.Run()
                    count = count + 1

                print("JMAG agent ran %i studies." % count)
                """,
            "current_study_report" => $$"""
                # -*- coding: utf-8 -*-
                import designer

                app = designer.GetApplication()
                study = app.GetCurrentStudy()
                print("Current study:", study.GetName())
                print("Has result:", study.HasResult())
                print("Study type:", study.GetType())
                """,
            "export_mesh_jcf_scheduler" => $$"""
                # -*- coding: utf-8 -*-
                # Experimental scheduler script. Adjust paths for your JMAG install.
                import designer
                import subprocess
                import os

                app = designer.GetApplication()
                work_dir = {{outputLiteral}} or os.getcwd()
                install_dir = os.environ.get("JMAG_HOME", r"C:/Program Files/JMAG-Designer18.1")
                jcf_path = os.path.join(work_dir, "agent_export.jcf")

                app.GetModel(0).GetStudy(0).WriteMeshJcf(jcf_path)
                command = '"%s/scheduler.exe" "%s" --workdir "%s"' % (install_dir, jcf_path, work_dir)
                print(command)
                subprocess.Popen(command)
                """,
            "set_condition_by_coordinates" => $$"""
                # -*- coding: utf-8 -*-
                # Template based on public JMAG scripting examples. Replace condition names and coordinates.
                import designer

                app = designer.GetApplication()
                condition_name = u"FEMConductor"
                sub_condition_name = u"Conductor Set 1"
                x, y, z = 5, 30, 0

                cond = app.GetModel(0).GetStudy(0).GetCondition(condition_name).GetSubCondition(sub_condition_name)
                cond.ClearParts()
                selection = cond.GetSelection()
                selection.Clear()
                selection.SelectPartByPosition(x, y, z)
                cond.AddSelected(selection)
                print("Updated condition:", condition_name, sub_condition_name)
                """,
            "parameter_sweep_skeleton" => $$"""
                # -*- coding: utf-8 -*-
                # Experimental skeleton. Fill in real design variables and result extraction calls.
                import designer
                import csv
                import os

                app = designer.GetApplication()
                project_path = {{projectLiteral}}
                output_csv = {{outputLiteral}} or os.path.abspath("jmag_agent_sweep.csv")
                if project_path:
                    app.Load(project_path)

                cases = [
                    {"case": "case_001", "variable": "example_dimension", "value": 1.0},
                    {"case": "case_002", "variable": "example_dimension", "value": 1.2},
                ]

                with open(output_csv, "w", newline="") as f:
                    writer = csv.DictWriter(f, fieldnames=["case", "variable", "value", "status"])
                    writer.writeheader()
                    for case in cases:
                        # TODO: set your JMAG parameter here.
                        # TODO: run the target study here.
                        case["status"] = "planned"
                        writer.writerow(case)

                print("Wrote", output_csv)
                """,
            _ => $$"""
                # -*- coding: utf-8 -*-
                # Generic JMAG Designer script starter.
                import designer

                app = designer.GetApplication()
                project_path = {{projectLiteral}}
                study_name = {{studyLiteral}}

                if project_path:
                    app.Load(project_path)

                print("JMAG application:", app)
                print("Studies:", app.NumStudies())
                if study_name:
                    print("Requested study:", study_name)
                """
        };
    }

    private static IEnumerable<string> FindJmagExecutables()
    {
        var envExe = Environment.GetEnvironmentVariable("JMAG_DESIGNER_EXE");
        if (!string.IsNullOrWhiteSpace(envExe) && File.Exists(envExe))
        {
            yield return Path.GetFullPath(envExe);
        }

        var envHome = Environment.GetEnvironmentVariable("JMAG_HOME");
        foreach (var path in FindExecutablesUnder(envHome))
        {
            yield return path;
        }

        foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            foreach (var dir in Directory.EnumerateDirectories(root, "*JMAG*", SearchOption.TopDirectoryOnly).Concat(Directory.EnumerateDirectories(root, "*JSOL*", SearchOption.TopDirectoryOnly)))
            {
                foreach (var path in FindExecutablesUnder(dir))
                {
                    yield return path;
                }
            }
        }
    }

    private static IEnumerable<string> FindExecutablesUnder(string? root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            yield break;
        }

        foreach (var pattern in new[] { "designer.exe", "jmag*.exe", "scheduler.exe" })
        {
            IEnumerable<string> matches;
            try
            {
                matches = Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories).Take(50).ToArray();
            }
            catch
            {
                matches = [];
            }

            foreach (var match in matches)
            {
                yield return match;
            }
        }
    }

    private static DirectoryInfo RequireExistingDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(path);
        }

        return new DirectoryInfo(path);
    }

    private static FileInfo RequireExistingFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        return new FileInfo(path);
    }

    private static bool IsIgnoredPath(string path) =>
        path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
        path.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static object DescribeFile(string root, FileInfo file) => new
    {
        path = Path.GetRelativePath(root, file.FullName).Replace('\\', '/'),
        sizeBytes = file.Length,
        modifiedUtc = file.LastWriteTimeUtc
    };

    private static object DescribeCsv(string root, FileInfo file)
    {
        var firstLine = File.ReadLines(file.FullName).FirstOrDefault() ?? "";
        return new
        {
            path = Path.GetRelativePath(root, file.FullName).Replace('\\', '/'),
            sizeBytes = file.Length,
            headers = SplitCsvLine(firstLine)
        };
    }

    private static IEnumerable<object> ScanTextSignals(string root, FileInfo file)
    {
        string[] lines;
        try
        {
            lines = File.ReadLines(file.FullName).Take(300).ToArray();
        }
        catch
        {
            yield break;
        }

        var regex = new Regex(@"\b(designer\.GetApplication|GetStudy|Run\(|scheduler\.exe|WriteMeshJcf|GetCondition|CreateContour|GetResult|JMAG)\b", RegexOptions.IgnoreCase);
        for (var i = 0; i < lines.Length; i++)
        {
            if (regex.IsMatch(lines[i]))
            {
                yield return new
                {
                    path = Path.GetRelativePath(root, file.FullName).Replace('\\', '/'),
                    line = i + 1,
                    text = Truncate(lines[i].Trim(), 180)
                };
            }
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
    }

    private static string NormalizeIdentifier(string value)
    {
        var cleaned = Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9_]+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(cleaned) ? "starter" : cleaned;
    }

    private static string ToPythonString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "None";
        }

        return JsonSerializer.Serialize(value.Replace('\\', '/'));
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "...";

    private static object? TryInvoke(Type type, object instance, string name, params object?[] args)
    {
        try
        {
            return type.InvokeMember(name, BindingFlags.InvokeMethod, null, instance, args);
        }
        catch (Exception ex)
        {
            return "ERROR: " + (ex.InnerException?.Message ?? ex.Message);
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool IsComProgIdRegistered(string progId) =>
        Type.GetTypeFromProgID(progId, throwOnError: false) is not null;
}
