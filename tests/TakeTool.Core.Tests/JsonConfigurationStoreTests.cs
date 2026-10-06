using TakeTool.Core.Configuration;

namespace TakeTool.Core.Tests;

public sealed class JsonConfigurationStoreTests
{
    [Fact]
    public async Task Settings_RoundTrip()
    {
        using var temp = new TempAppData();
        var store = new JsonConfigurationStore(temp);

        var settings = new AppSettings
        {
            OverlayOpacity = 0.7,
            ExpandOnHover = false,
            EnabledUtilityIds = ["imgbb-uploader"],
            HasInitializedEnabledUtilities = true
        };

        await store.SaveSettingsAsync(settings);
        var loaded = await store.LoadSettingsAsync();

        Assert.Equal(0.7, loaded.OverlayOpacity);
        Assert.False(loaded.ExpandOnHover);
        Assert.Contains("imgbb-uploader", loaded.EnabledUtilityIds);
        Assert.True(loaded.HasInitializedEnabledUtilities);
    }

    [Fact]
    public async Task UtilityConfig_RoundTrip()
    {
        using var temp = new TempAppData();
        var store = new JsonConfigurationStore(temp);

        await store.SaveUtilityConfigAsync("imgbb-uploader", new Dictionary<string, string>
        {
            ["ApiKey"] = "secret-value"
        });

        var loaded = await store.LoadUtilityConfigAsync("imgbb-uploader");
        Assert.Equal("secret-value", loaded["ApiKey"]);
    }

    [Fact]
    public async Task UtilityConfig_RejectsUnsafeIds()
    {
        using var temp = new TempAppData();
        var store = new JsonConfigurationStore(temp);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.SaveUtilityConfigAsync("../evil", new Dictionary<string, string> { ["a"] = "b" }));
    }

    private sealed class TempAppData : IAppDataPathProvider, IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "TakeToolTests-" + Guid.NewGuid().ToString("N"));

        public string GetRootDirectory() => _root;

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
