namespace TakeTool.Core.Configuration;

public interface IConfigurationStore
{
    Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);

    Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> LoadUtilityConfigAsync(
        string utilityId,
        CancellationToken cancellationToken = default);

    Task SaveUtilityConfigAsync(
        string utilityId,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default);
}
