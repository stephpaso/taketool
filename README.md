# TakeTool

Desktop overlay of quick utilities for Windows (macOS host planned later). Modular plugin-style architecture: the core never hardcodes a specific utility.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Solution layout

```
src/
  TakeTool.Core/           # contracts, config, platform flags (net8.0)
  TakeTool.Utilities/      # ImgBB and future utilities (net8.0)
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

Config and secrets are stored under `%APPDATA%/TakeTool/` (never commit API keys).

## ImgBB utility

1. Open **Settings** from the tray menu.
2. Enable **ImgBB Uploader** and paste your API key from [api.imgbb.com](https://api.imgbb.com/).
3. Drag an image (`.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`) onto the floating hub or the ImgBB slot.
4. On success the public HTTPS URL is copied to the clipboard.

## Cross-platform utilities

Each utility declares `SupportedPlatforms` (`Windows`, `MacOS`, `Linux`, or `Any`). The registry only surfaces utilities compatible with the current OS.

- Pure logic (HTTP, files) should use `UtilityPlatform.Any` and depend on Core abstractions (`IClipboardService`, etc.).
- OS-specific utilities set a narrower flag, or inject platform services from the UI host.

A future `TakeTool.UI.Mac` project can reuse `TakeTool.Core` and `TakeTool.Utilities` without rewriting plugins.

## Tests

```bash
dotnet test
```

UI smoke testing is manual on Windows (overlay, tray, drag-and-drop). Automated tests cover Core + Utilities (including ImgBB with a mocked HTTP handler).

## License

See [LICENSE](LICENSE).
