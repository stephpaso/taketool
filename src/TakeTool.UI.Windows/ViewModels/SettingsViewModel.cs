using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Services;

namespace TakeTool.UI.Windows.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly UtilityRegistry _registry;
    private readonly IConfigurationStore _configurationStore;

    public SettingsViewModel(UtilityRegistry registry, IConfigurationStore configurationStore)
    {
        _registry = registry;
        _configurationStore = configurationStore;
        Utilities = new ObservableCollection<UtilitySettingsItemViewModel>();
    }

    public ObservableCollection<UtilitySettingsItemViewModel> Utilities { get; }

    [ObservableProperty]
    private double overlayOpacity = 0.95;

    [ObservableProperty]
    private bool expandOnHover = true;

    [ObservableProperty]
    private bool startWithOperatingSystem;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public async Task LoadAsync()
    {
        var settings = await _configurationStore.LoadSettingsAsync().ConfigureAwait(true);
        OverlayOpacity = settings.OverlayOpacity;
        ExpandOnHover = settings.ExpandOnHover;
        StartWithOperatingSystem = settings.StartWithOperatingSystem;

        var compatible = _registry.GetPlatformCompatible();
        var enabled = new HashSet<string>(settings.EnabledUtilityIds, StringComparer.OrdinalIgnoreCase);
        if (!settings.HasInitializedEnabledUtilities)
        {
            enabled = compatible.Select(u => u.Metadata.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        Utilities.Clear();
        foreach (var utility in compatible)
        {
            var config = await _configurationStore
                .LoadUtilityConfigAsync(utility.Metadata.Id)
                .ConfigureAwait(true);

            var item = new UtilitySettingsItemViewModel(utility)
            {
                IsEnabled = enabled.Contains(utility.Metadata.Id)
            };

            foreach (var field in utility.ConfigurationSchema.Fields)
            {
                config.TryGetValue(field.Key, out var value);
                item.Fields.Add(new ConfigurationFieldViewModel(field, value ?? field.DefaultValue ?? string.Empty));
            }

            Utilities.Add(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = await _configurationStore.LoadSettingsAsync().ConfigureAwait(true);
        settings.OverlayOpacity = Math.Clamp(OverlayOpacity, 0.3, 1.0);
        settings.ExpandOnHover = ExpandOnHover;
        settings.StartWithOperatingSystem = StartWithOperatingSystem;
        settings.EnabledUtilityIds = Utilities.Where(u => u.IsEnabled).Select(u => u.Id).ToList();
        settings.HasInitializedEnabledUtilities = true;
        await _configurationStore.SaveSettingsAsync(settings).ConfigureAwait(true);

        foreach (var utility in Utilities)
        {
            var values = utility.Fields.ToDictionary(f => f.Key, f => f.Value, StringComparer.OrdinalIgnoreCase);
            await _configurationStore.SaveUtilityConfigAsync(utility.Id, values).ConfigureAwait(true);
        }

        StatusMessage = "Settings saved.";
    }
}

public partial class UtilitySettingsItemViewModel : ObservableObject
{
    public UtilitySettingsItemViewModel(IUtility utility)
    {
        Utility = utility;
        Fields = new ObservableCollection<ConfigurationFieldViewModel>();
    }

    public IUtility Utility { get; }

    public string Id => Utility.Metadata.Id;

    public string Name => Utility.Metadata.Name;

    public string Description => Utility.Metadata.Description;

    public string Platforms => Utility.Metadata.SupportedPlatforms.ToString();

    [ObservableProperty]
    private bool isEnabled = true;

    public ObservableCollection<ConfigurationFieldViewModel> Fields { get; }
}

public partial class ConfigurationFieldViewModel : ObservableObject
{
    public ConfigurationFieldViewModel(ConfigurationFieldDefinition definition, string value)
    {
        Definition = definition;
        Value = value;
    }

    public ConfigurationFieldDefinition Definition { get; }

    public string Key => Definition.Key;

    public string DisplayName => Definition.DisplayName;

    public string? Description => Definition.Description;

    public bool IsPassword => Definition.FieldType == ConfigurationFieldType.Password;

    [ObservableProperty]
    private string value;
}
