using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Platform;
using TakeTool.Core.Services;

namespace TakeTool.Core.Tests;

public sealed class UtilityRegistryTests
{
    [Fact]
    public void GetPlatformCompatible_FiltersBySupportedPlatforms()
    {
        var windowsOnly = new FakeUtility("win", UtilityPlatform.Windows);
        var any = new FakeUtility("any", UtilityPlatform.Any);
        var macOnly = new FakeUtility("mac", UtilityPlatform.MacOS);
        var store = new InMemoryConfigurationStore();
        var registry = new UtilityRegistry([windowsOnly, any, macOnly], store);

        var onWindows = registry.GetPlatformCompatible(UtilityPlatform.Windows)
            .Select(u => u.Metadata.Id)
            .ToList();

        Assert.Contains("win", onWindows);
        Assert.Contains("any", onWindows);
        Assert.DoesNotContain("mac", onWindows);
    }

    [Fact]
    public async Task GetAvailableAsync_RespectsEnabledIds()
    {
        var a = new FakeUtility("a", UtilityPlatform.Any);
        var b = new FakeUtility("b", UtilityPlatform.Any);
        var store = new InMemoryConfigurationStore
        {
            Settings = new AppSettings
            {
                HasInitializedEnabledUtilities = true,
                EnabledUtilityIds = ["b"]
            }
        };
        var registry = new UtilityRegistry([a, b], store);

        var available = await registry.GetAvailableAsync(platformOverride: UtilityPlatform.Windows);
        Assert.Single(available);
        Assert.Equal("b", available[0].Metadata.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_InitializesAllEnabledOnFirstRun()
    {
        var a = new FakeUtility("a", UtilityPlatform.Any);
        var b = new FakeUtility("b", UtilityPlatform.Any);
        var store = new InMemoryConfigurationStore();
        var registry = new UtilityRegistry([a, b], store);

        var available = await registry.GetAvailableAsync(platformOverride: UtilityPlatform.Linux);
        Assert.Equal(2, available.Count);
        Assert.True(store.Settings.HasInitializedEnabledUtilities);
        Assert.Contains("a", store.Settings.EnabledUtilityIds);
        Assert.Contains("b", store.Settings.EnabledUtilityIds);
    }

    private sealed class FakeUtility : IUtility
    {
        public FakeUtility(string id, UtilityPlatform platforms)
        {
            Metadata = new UtilityMetadata
            {
                Id = id,
                Name = id,
                Description = id,
                IconKey = id,
                SupportedPlatforms = platforms
            };
        }

        public UtilityMetadata Metadata { get; }

        public IUtilityConfigurationSchema ConfigurationSchema { get; } = new EmptySchema();

        public bool CanAccept(UtilityContext context) => true;

        public Task<UtilityResult> ExecuteAsync(UtilityContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(UtilityResult.Ok("ok"));

        private sealed class EmptySchema : IUtilityConfigurationSchema
        {
            public IReadOnlyList<ConfigurationFieldDefinition> Fields { get; } =
                Array.Empty<ConfigurationFieldDefinition>();
        }
    }

    private sealed class InMemoryConfigurationStore : IConfigurationStore
    {
        public AppSettings Settings { get; set; } = new();

        public Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Settings);

        public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            Settings = settings;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, string>> LoadUtilityConfigAsync(
            string utilityId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public Task SaveUtilityConfigAsync(
            string utilityId,
            IReadOnlyDictionary<string, string> values,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
