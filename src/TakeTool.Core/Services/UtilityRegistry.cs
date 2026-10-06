using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Platform;

namespace TakeTool.Core.Services;

public sealed class UtilityRegistry
{
    private readonly IReadOnlyList<IUtility> _utilities;
    private readonly IConfigurationStore _configurationStore;

    public UtilityRegistry(IEnumerable<IUtility> utilities, IConfigurationStore configurationStore)
    {
        _utilities = utilities.ToList();
        _configurationStore = configurationStore;
    }

    public IReadOnlyList<IUtility> GetAll() => _utilities;

    public IUtility? GetById(string id)
        => _utilities.FirstOrDefault(u =>
            string.Equals(u.Metadata.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Utilities supported on the current OS (ignores enabled flag).
    /// </summary>
    public IReadOnlyList<IUtility> GetPlatformCompatible(
        UtilityPlatform? platformOverride = null)
    {
        var platform = platformOverride ?? UtilityPlatformDetector.Current;
        return _utilities
            .Where(u => (u.Metadata.SupportedPlatforms & platform) != 0)
            .ToList();
    }

    /// <summary>
    /// Platform-compatible utilities that are enabled in settings.
    /// </summary>
    public async Task<IReadOnlyList<IUtility>> GetAvailableAsync(
        CancellationToken cancellationToken = default,
        UtilityPlatform? platformOverride = null)
    {
        var settings = await _configurationStore.LoadSettingsAsync(cancellationToken)
            .ConfigureAwait(false);
        var compatible = GetPlatformCompatible(platformOverride);

        if (!settings.HasInitializedEnabledUtilities)
        {
            settings.EnabledUtilityIds = compatible.Select(u => u.Metadata.Id).ToList();
            settings.HasInitializedEnabledUtilities = true;
            await _configurationStore.SaveSettingsAsync(settings, cancellationToken)
                .ConfigureAwait(false);
            return compatible;
        }

        var enabled = new HashSet<string>(settings.EnabledUtilityIds, StringComparer.OrdinalIgnoreCase);
        return compatible.Where(u => enabled.Contains(u.Metadata.Id)).ToList();
    }
}
