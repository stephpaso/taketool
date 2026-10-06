# TakeTool Utilities

Guide for adding a new utility to `TakeTool.Utilities`, plus the catalog of built-in utilities.

---

## Create a new utility

### 1. Add a folder and class

Create a folder under `src/TakeTool.Utilities/`, for example `MyUtility/`, and implement `IUtility` by extending `BaseUtility`:

```csharp
using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Platform;
using TakeTool.Core.Utilities;

namespace TakeTool.Utilities.MyUtility;

public sealed class MyUtility : BaseUtility
{
    public const string UtilityId = "my-utility";

    public MyUtility(IConfigurationStore configurationStore)
        : base(configurationStore)
    {
    }

    public override UtilityMetadata Metadata { get; } = new()
    {
        Id = UtilityId,
        Name = "My Utility",
        Description = "Short description shown in Settings.",
        IconKey = "my-utility",
        SupportedPlatforms = UtilityPlatform.Any, // or Windows / MacOS / Linux
        AcceptedInputKinds = UtilityInputKind.Files | UtilityInputKind.Click,
        AcceptedExtensions = [".txt"],
        AcceptedMimeTypes = []
    };

    public override IUtilityConfigurationSchema ConfigurationSchema { get; } =
        new EmptySchema(); // or define fields (see below)

    public override async Task<UtilityResult> ExecuteAsync(
        UtilityContext context,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccept(context))
        {
            return UtilityResult.Fail("This utility cannot handle the current input.");
        }

        // Your logic here. Prefer Core abstractions (IClipboardService, INotificationService).
        return UtilityResult.Ok("Done.");
    }

    private sealed class EmptySchema : IUtilityConfigurationSchema
    {
        public IReadOnlyList<ConfigurationFieldDefinition> Fields { get; } =
            Array.Empty<ConfigurationFieldDefinition>();
    }
}
```

### 2. Metadata checklist

| Field | Purpose |
| --- | --- |
| `Id` | Stable id (`kebab-case`). Used for config file name: `%APPDATA%/TakeTool/utilities/{id}.json` |
| `Name` / `Description` | UI labels |
| `IconKey` | Resource key resolved by the UI host |
| `SupportedPlatforms` | `Any`, or a subset of `Windows` / `MacOS` / `Linux` |
| `AcceptedInputKinds` | `Click`, `Files`, `Text` (flags) |
| `AcceptedExtensions` / `AcceptedMimeTypes` | Filters for drag-and-drop (e.g. `.png`, `image/*`) |

Rules of thumb:

- Pure logic (HTTP, files, JSON) → `UtilityPlatform.Any` and Core abstractions only.
- OS-specific APIs → set a narrower `SupportedPlatforms`, or inject a platform service from the UI host.
- Image utilities that declare `image/*` also go through magic-byte validation in `BaseUtility` / `SafeFileAccess`.

### 3. Optional configuration fields

Expose Settings fields via `IUtilityConfigurationSchema`:

```csharp
public override IUtilityConfigurationSchema ConfigurationSchema { get; } =
    new MySchema();

private sealed class MySchema : IUtilityConfigurationSchema
{
    public IReadOnlyList<ConfigurationFieldDefinition> Fields { get; } =
    [
        new ConfigurationFieldDefinition
        {
            Key = "ApiKey",
            DisplayName = "API Key",
            Description = "Where the user gets the key",
            FieldType = ConfigurationFieldType.Password, // String, Password, Path, Bool, Number
            IsRequired = true
        }
    ];
}
```

Load values in `ExecuteAsync` with `LoadConfigAsync()` from `BaseUtility`. On Windows, utility config values are encrypted at rest (DPAPI).

### 4. Register in DI

Wire the utility in [`DependencyInjection/ServiceCollectionExtensions.cs`](DependencyInjection/ServiceCollectionExtensions.cs):

```csharp
services.AddSingleton<IUtility, MyUtility>();
```

If you need `HttpClient`, register a named client (same pattern as ImgBB) and inject your client type.

### 5. Tests

Add unit tests under `tests/TakeTool.Utilities.Tests/`. Mock HTTP and Core services; do not call real third-party APIs in CI.

### 6. Document it

Add an entry under **Preinstalled utilities** below in this file.

---

## Preinstalled utilities

### ImgBB Uploader

| | |
| --- | --- |
| **Id** | `imgbb-uploader` |
| **Folder** | [`ImgBB/`](ImgBB/) |
| **Platforms** | `Any` |
| **Input** | Image files: `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp` |
| **Config** | `ApiKey` (Password) from [api.imgbb.com](https://api.imgbb.com/) |

**What it does:** uploads the dropped image to ImgBB (multipart HTTPS), copies the public HTTPS URL to the clipboard, and shows success/error feedback.

**How to use:**

1. Open **Settings** from the tray menu.
2. Enable **ImgBB Uploader** and paste your API key.
3. Drag an image onto the floating hub or the ImgBB slot.
4. On success, the public link is on the clipboard.
