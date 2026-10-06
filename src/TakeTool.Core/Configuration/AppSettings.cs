namespace TakeTool.Core.Configuration;

public sealed class AppSettings
{
    public double OverlayOpacity { get; set; } = 0.95;

    /// <summary>
    /// When true, expand the arc menu on hover; otherwise require click.
    /// </summary>
    public bool ExpandOnHover { get; set; } = true;

    public bool StartWithOperatingSystem { get; set; }

    public bool OverlayVisible { get; set; } = true;

    /// <summary>
    /// Null means use default bottom-right anchoring.
    /// </summary>
    public double? OverlayLeft { get; set; }

    public double? OverlayTop { get; set; }

    /// <summary>
    /// Utility IDs that are enabled. If empty on first run, all available utilities are treated as enabled.
    /// </summary>
    public List<string> EnabledUtilityIds { get; set; } = new();

    public bool HasInitializedEnabledUtilities { get; set; }
}
