# TakeTool

Desktop overlay of quick utilities for Windows (macOS host planned later). Modular plugin-style architecture: the core never hardcodes a specific utility.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Solution layout

```
src/
  TakeTool.Core/           # contracts, config, platform flags (net8.0)
  TakeTool.Utilities/      # built-in utilities (net8.0) — see README there
  TakeTool.UI.Windows/     # WPF host: overlay, tray, settings
tests/
  TakeTool.Core.Tests/
  TakeTool.Utilities.Tests/
```

## Run (Windows)

```bash
dotnet restore
dotnet run --project src/TakeTool.UI.Windows
```

Config and secrets are stored under `%APPDATA%/TakeTool/`. Utility config values (including API keys) are encrypted at rest with Windows DPAPI (`CurrentUser` scope). Never commit API keys.

## Utilities

How to implement a new utility, and the list of preinstalled ones:

→ **[src/TakeTool.Utilities/README.md](src/TakeTool.Utilities/README.md)**

## Cross-platform notes

Each utility declares `SupportedPlatforms` (`Windows`, `MacOS`, `Linux`, or `Any`). The registry only surfaces utilities compatible with the current OS. Details and examples are in the utilities README linked above.

A future `TakeTool.UI.Mac` project can reuse `TakeTool.Core` and `TakeTool.Utilities` without rewriting plugins.

## Tests

```bash
dotnet test
```

UI smoke testing is manual on Windows (overlay, tray, drag-and-drop). Automated tests cover Core + Utilities (HTTP mocked where needed).

## License

See [LICENSE](LICENSE).
