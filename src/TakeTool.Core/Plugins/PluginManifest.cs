namespace TakeTool.Core.Plugins;

/// <summary>
/// Placeholder contract for future external plugin discovery (folder + manifest).
/// Not loaded dynamically in Phase 1; utilities are registered via DI.
/// </summary>
public sealed class PluginManifest
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = "1.0.0";

    public string EntryAssembly { get; init; } = string.Empty;

    /// <summary>
    /// Comma-separated or flags serialized as string in JSON manifests later.
    /// </summary>
    public string SupportedPlatforms { get; init; } = "Any";
}
