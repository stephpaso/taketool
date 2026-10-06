using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Services;

namespace TakeTool.UI.Windows.ViewModels;

public partial class OverlayViewModel : ObservableObject
{
    private readonly UtilityRegistry _registry;
    private readonly IConfigurationStore _configurationStore;
    private CancellationTokenSource? _feedbackCts;

    public OverlayViewModel(UtilityRegistry registry, IConfigurationStore configurationStore)
    {
        _registry = registry;
        _configurationStore = configurationStore;
        Slots = new ObservableCollection<UtilitySlotViewModel>();
    }

    public ObservableCollection<UtilitySlotViewModel> Slots { get; }

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? feedbackTitle;

    [ObservableProperty]
    private string? feedbackMessage;

    [ObservableProperty]
    private bool feedbackIsError;

    [ObservableProperty]
    private bool isFeedbackVisible;

    [ObservableProperty]
    private double opacity = 0.95;

    [ObservableProperty]
    private bool expandOnHover = true;

    public async Task InitializeAsync()
    {
        var settings = await _configurationStore.LoadSettingsAsync().ConfigureAwait(true);
        Opacity = settings.OverlayOpacity;
        ExpandOnHover = settings.ExpandOnHover;
        await ReloadSlotsAsync().ConfigureAwait(true);
    }

    public async Task ReloadSlotsAsync()
    {
        var available = await _registry.GetAvailableAsync().ConfigureAwait(true);
        Slots.Clear();

        const double startAngle = 20;
        const double sweep = 70;
        var count = Math.Max(available.Count, 1);
        var step = available.Count <= 1 ? 0 : sweep / (available.Count - 1);

        for (var i = 0; i < available.Count; i++)
        {
            var angle = startAngle + (step * i);
            Slots.Add(new UtilitySlotViewModel(available[i], angle, radius: 96));
        }
    }

    public void RequestExpand(bool expand)
    {
        if (!ExpandOnHover && expand)
        {
            return;
        }

        IsExpanded = expand;
    }

    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private async Task ExecuteUtilityAsync(UtilitySlotViewModel? slot)
    {
        if (slot is null || IsBusy)
        {
            return;
        }

        await RunUtilityAsync(slot.Utility, new UtilityContext
        {
            Trigger = UtilityTrigger.Click
        }).ConfigureAwait(true);
    }

    public async Task HandleDropAsync(UtilitySlotViewModel? slot, IReadOnlyList<string> files)
    {
        if (IsBusy || files.Count == 0)
        {
            return;
        }

        IUtility? utility = slot?.Utility;
        if (utility is null)
        {
            // Drop on hub: pick first utility that accepts the files.
            utility = Slots.Select(s => s.Utility).FirstOrDefault(u =>
                u.CanAccept(new UtilityContext { FilePaths = files, Trigger = UtilityTrigger.Drop }));
        }

        if (utility is null)
        {
            ShowFeedback("TakeTool", "No enabled utility accepts these files.", isError: true);
            return;
        }

        await RunUtilityAsync(utility, new UtilityContext
        {
            FilePaths = files,
            Trigger = UtilityTrigger.Drop
        }).ConfigureAwait(true);
    }

    public void ShowFeedback(string title, string message, bool isError)
    {
        FeedbackTitle = title;
        FeedbackMessage = message;
        FeedbackIsError = isError;
        IsFeedbackVisible = true;

        _feedbackCts?.Cancel();
        _feedbackCts = new CancellationTokenSource();
        var token = _feedbackCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3500, token).ConfigureAwait(false);
                await Application.Current.Dispatcher.InvokeAsync(() => IsFeedbackVisible = false);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }, token);
    }

    private async Task RunUtilityAsync(IUtility utility, UtilityContext context)
    {
        IsBusy = true;
        try
        {
            var result = await utility.ExecuteAsync(context).ConfigureAwait(true);
            if (!result.Success)
            {
                // Notifications may already have shown feedback; ensure UI updates.
                ShowFeedback(utility.Metadata.Name, result.Message, isError: true);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback(utility.Metadata.Name, ex.Message, isError: true);
        }
        finally
        {
            IsBusy = false;
            IsExpanded = false;
        }
    }
}
