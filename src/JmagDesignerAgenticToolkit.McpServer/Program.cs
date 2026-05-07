using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JmagDesignerAgenticToolkit.Core;

var server = new McpServer(new JmagToolkit());
await server.RunAsync();

internal sealed class McpServer(JmagToolkit jmag)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task RunAsync()
    {
        var stdout = Console.OpenStandardOutput();

        while (true)
        {
            var message = await ReadMessageAsync(Console.In);
            if (message is null)
            {
                return;
            }

            JsonNode? response;
            try
            {
                response = Handle(message);
            }
            catch (Exception ex)
            {
                response = Error(message["id"], -32603, ex.Message);
            }

            if (response is not null)
            {
                await WriteMessageAsync(stdout, response);
            }
        }
    }

    private JsonNode? Handle(JsonNode request)
    {
        var method = request["method"]?.GetValue<string>();
        var id = request["id"];

        return method switch
        {
            "initialize" => Result(id, new
            {
                protocolVersion = "2025-06-18",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "jmag-designer-agentic-toolkit", version = "0.1.0" }
            }),
            "notifications/initialized" => null,
            "tools/list" => Result(id, new { tools = ToolDefinitions.All }),
            "tools/call" => CallTool(id, request["params"]?.AsObject()),
            _ => Error(id, -32601, $"Unknown method: {method}")
        };
    }

    private JsonNode CallTool(JsonNode? id, JsonObject? parameters)
    {
        var name = parameters?["name"]?.GetValue<string>() ?? "";
        var args = parameters?["arguments"]?.AsObject() ?? new JsonObject();

        object result = name switch
        {
            "jmag_capabilities" => jmag.GetCapabilities(),
            "jmag_environment_status" => jmag.GetEnvironmentStatus(),
            "jmag_com_status" => jmag.GetComStatus(),
            "jmag_run_script_via_com" => jmag.RunScriptViaCom(ReadString(args, "scriptPath"), ReadNullableBool(args, "visible") ?? false, ReadNullableInt(args, "waitSeconds") ?? 2, ReadNullableString(args, "progId")),
            "jmag_analyze_project_folder" => jmag.AnalyzeProjectFolder(ReadString(args, "folderPath"), ReadNullableInt(args, "maxFiles") ?? 300),
            "jmag_analyze_csv_results" => jmag.AnalyzeCsvResults(ReadString(args, "filePath"), ReadNullableInt(args, "maxRows") ?? 20),
            "jmag_generate_script" => jmag.GenerateScript(ReadString(args, "outputFolder"), ReadString(args, "scriptKind"), ReadNullableString(args, "projectPath"), ReadNullableString(args, "outputPath"), ReadNullableString(args, "studyName")),
            "jmag_generate_run_plan" => jmag.GenerateRunPlan(ReadString(args, "outputFolder"), ReadNullableString(args, "projectPath"), ReadNullableString(args, "objective"), ReadNullableString(args, "notes")),
            _ => new { error = $"Unknown tool: {name}" }
        };

        return Result(id, new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(result, JsonOptions)
                }
            }
        });
    }

    private static int? ReadNullableInt(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            return null;
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.Number => value.GetValue<int>(),
            JsonValueKind.String when int.TryParse(value.GetValue<string>(), out var result) => result,
            _ => null
        };
    }

    private static string? ReadNullableString(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            return null;
        }

        var text = value.GetValue<string>();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool? ReadNullableBool(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            return null;
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetValue<string>(), out var result) => result,
            _ => null
        };
    }

    private static string ReadString(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return value.GetValue<string>();
    }

    private static JsonNode Result(JsonNode? id, object result)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneOrNull(id),
            ["result"] = JsonSerializer.SerializeToNode(result, JsonOptions)
        };

        return response;
    }

    private static JsonNode Error(JsonNode? id, int code, string message)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneOrNull(id),
            ["error"] = JsonSerializer.SerializeToNode(new { code, message })
        };

        return response;
    }

    private static JsonNode? CloneOrNull(JsonNode? node) =>
        node is null ? null : JsonNode.Parse(node.ToJsonString());

    private static Task<JsonNode?> ReadMessageAsync(TextReader input)
    {
        var headers = new List<string>();
        while (true)
        {
            var line = input.ReadLine();
            if (line is null)
            {
                return Task.FromResult<JsonNode?>(null);
            }

            if (line.Length == 0)
            {
                break;
            }

            headers.Add(line);
        }

        var contentLength = headers
            .Select(header => header.Split(':', 2))
            .Where(parts => parts.Length == 2 && parts[0].Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            .Select(parts => int.TryParse(parts[1].Trim(), out var value) ? value : 0)
            .FirstOrDefault();

        if (contentLength <= 0)
        {
            return Task.FromResult<JsonNode?>(null);
        }

        var buffer = new char[contentLength];
        var offset = 0;
        while (offset < contentLength)
        {
            var read = input.Read(buffer, offset, contentLength - offset);
            if (read == 0)
            {
                return Task.FromResult<JsonNode?>(null);
            }

            offset += read;
        }

        return Task.FromResult(JsonNode.Parse(new string(buffer)));
    }

    private static async Task WriteMessageAsync(Stream output, JsonNode message)
    {
        var json = message.ToJsonString(JsonOptions);
        var payload = Encoding.UTF8.GetBytes(json);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");
        await output.WriteAsync(header);
        await output.WriteAsync(payload);
        await output.FlushAsync();
    }
}

internal static class ToolDefinitions
{
    public static readonly object[] All =
    [
        new
        {
            name = "jmag_capabilities",
            title = "JMAG capabilities",
            description = "Report the automation modes supported by this toolkit: full script-agentic, semi-agentic, and advisory.",
            inputSchema = EmptySchema()
        },
        new
        {
            name = "jmag_environment_status",
            title = "JMAG environment status",
            description = "Detect likely JMAG Designer/scheduler executables, environment variables, and running JMAG processes.",
            inputSchema = EmptySchema()
        },
        new
        {
            name = "jmag_com_status",
            title = "JMAG COM status",
            description = "Check whether JMAG Designer COM automation ProgIDs are registered on this Windows machine.",
            inputSchema = EmptySchema()
        },
        new
        {
            name = "jmag_run_script_via_com",
            title = "Run JMAG script via COM",
            description = "Run a Python/VB/JScript file through JMAG Designer COM automation. Use only reviewed scripts and copied projects.",
            inputSchema = Obj(new
            {
                scriptPath = Str("Script file path to run through JMAG Designer."),
                visible = Bool("Whether to make JMAG visible while running."),
                waitSeconds = Int("Seconds to wait before reading JMAG status. Default 2."),
                progId = Str("Optional COM ProgID. Defaults to designer.Application.181.")
            }, ["scriptPath"])
        },
        new
        {
            name = "jmag_analyze_project_folder",
            title = "Analyze JMAG folder",
            description = "Scan a folder containing copied JMAG projects, scripts, logs, exports, or CSV results and summarize useful automation signals.",
            inputSchema = Obj(new
            {
                folderPath = Str("Folder to scan."),
                maxFiles = Int("Maximum files to scan. Default 300.")
            }, ["folderPath"])
        },
        new
        {
            name = "jmag_analyze_csv_results",
            title = "Analyze JMAG CSV results",
            description = "Preview exported JMAG CSV results and compute basic numeric summaries.",
            inputSchema = Obj(new
            {
                filePath = Str("CSV file path."),
                maxRows = Int("Rows to preview. Default 20.")
            }, ["filePath"])
        },
        new
        {
            name = "jmag_generate_script",
            title = "Generate JMAG Python script",
            description = "Generate a Python script starter for JMAG Designer automation. Supported kinds include run_all_studies, current_study_report, export_mesh_jcf_scheduler, set_condition_by_coordinates, and parameter_sweep_skeleton.",
            inputSchema = Obj(new
            {
                outputFolder = Str("Folder where the generated script will be written."),
                scriptKind = Str("Script kind to generate."),
                projectPath = Str("Optional copied JMAG project path."),
                outputPath = Str("Optional output CSV/folder path."),
                studyName = Str("Optional study name.")
            }, ["outputFolder", "scriptKind"])
        },
        new
        {
            name = "jmag_generate_run_plan",
            title = "Generate JMAG run plan",
            description = "Write a concise run plan for executing an agentic JMAG workflow on a licensed machine.",
            inputSchema = Obj(new
            {
                outputFolder = Str("Folder where the run plan will be written."),
                projectPath = Str("Optional copied JMAG project path."),
                objective = Str("Optional engineering objective."),
                notes = Str("Optional local notes.")
            }, ["outputFolder"])
        }
    ];

    private static object EmptySchema() => new
    {
        type = "object",
        properties = new { },
        additionalProperties = false
    };

    private static object Obj(object properties, string[] required) => new
    {
        type = "object",
        properties,
        required,
        additionalProperties = false
    };

    private static object Str(string description) => new { type = "string", description };

    private static object Int(string description) => new { type = "integer", description };

    private static object Bool(string description) => new { type = "boolean", description };
}
