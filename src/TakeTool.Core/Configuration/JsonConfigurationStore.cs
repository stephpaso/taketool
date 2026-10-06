using System.Text.Json;
using System.Text.RegularExpressions;

namespace TakeTool.Core.Configuration;

public sealed class JsonConfigurationStore : IConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex SafeUtilityId = new("^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

    private readonly IAppDataPathProvider _pathProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonConfigurationStore(IAppDataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureDirectories();
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            await using var stream = File.OpenRead(path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            return settings ?? new AppSettings();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureDirectories();
            var path = GetSettingsPath();
            var tempPath = path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> LoadUtilityConfigAsync(
        string utilityId,
        CancellationToken cancellationToken = default)
    {
        ValidateUtilityId(utilityId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureDirectories();
            var path = GetUtilityConfigPath(utilityId);
            if (!File.Exists(path))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            await using var stream = File.OpenRead(path);
            var values = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(
                    stream,
                    JsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);

            return values ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveUtilityConfigAsync(
        string utilityId,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ValidateUtilityId(utilityId);
        ArgumentNullException.ThrowIfNull(values);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureDirectories();
            var path = GetUtilityConfigPath(utilityId);
            var tempPath = path + ".tmp";
            var payload = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, payload, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EnsureDirectories()
    {
        var root = _pathProvider.GetRootDirectory();
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "utilities"));
        // Reserved for future external plugin manifests.
        Directory.CreateDirectory(Path.Combine(root, "plugins"));
    }

    private string GetSettingsPath()
        => Path.Combine(_pathProvider.GetRootDirectory(), "settings.json");

    private string GetUtilityConfigPath(string utilityId)
        => Path.Combine(_pathProvider.GetRootDirectory(), "utilities", utilityId + ".json");

    private static void ValidateUtilityId(string utilityId)
    {
        if (string.IsNullOrWhiteSpace(utilityId) || !SafeUtilityId.IsMatch(utilityId))
        {
            throw new ArgumentException("Utility id contains invalid characters.", nameof(utilityId));
        }
    }
}
