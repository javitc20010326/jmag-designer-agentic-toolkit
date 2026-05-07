# Architecture

```mermaid
flowchart LR
    Agent["Codex / Claude Code"] --> MCP["JMAG MCP Server (.NET stdio)"]
    MCP --> Core["Toolkit Core"]
    Core --> Detect["Environment detector"]
    Core --> Analyze["Folder / CSV analyzers"]
    Core --> Generate["JMAG Python script generator"]
    Generate --> JMAG["JMAG Designer script editor or external Python"]
    JMAG --> Results["CSV / logs / images"]
    Results --> Analyze
```

The MCP server does not embed JMAG binaries or require JMAG libraries at compile time. It generates and organizes scripts that call JMAG's own scripting APIs on the licensed machine.

This keeps the repo portable and avoids redistributing proprietary software.
