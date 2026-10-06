using TakeTool.Core.Platform;

namespace TakeTool.Core.Abstractions;

public sealed class UtilityMetadata
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    /// <summary>
    /// Resource key for an icon (SVG path / geometry) resolved by the UI host.
    /// </summary>
    public required string IconKey { get; init; }

    public UtilityPlatform SupportedPlatforms { get; init; } = UtilityPlatform.Any;

    public UtilityInputKind AcceptedInputKinds { get; init; } = UtilityInputKind.Click;

    /// <summary>
    /// MIME patterns such as "image/*" or "application/pdf". Empty means no MIME filter.
    /// </summary>
    public IReadOnlyList<string> AcceptedMimeTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// File extensions including the leading dot, e.g. ".png". Empty means no extension filter.
    /// </summary>
    public IReadOnlyList<string> AcceptedExtensions { get; init; } = Array.Empty<string>();
}
