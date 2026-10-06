using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TakeTool.Core.Configuration;
using TakeTool.UI.Windows.ViewModels;

namespace TakeTool.UI.Windows.Views;

public partial class OverlayWindow : Window
{
    private readonly OverlayViewModel _viewModel;
    private readonly IConfigurationStore _configurationStore;
    private Point? _dragStart;
    private bool _isDraggingWindow;

    public OverlayWindow(OverlayViewModel viewModel, IConfigurationStore configurationStore)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _configurationStore = configurationStore;
        DataContext = _viewModel;
        Loaded += OnLoaded;
        SourceInitialized += (_, _) =>
        {
            // Keep HUD from stealing activation when possible.
            ShowActivated = false;
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await PositionOverlayAsync().ConfigureAwait(true);
        await _viewModel.InitializeAsync().ConfigureAwait(true);
    }

    private async Task PositionOverlayAsync()
    {
        var settings = await _configurationStore.LoadSettingsAsync().ConfigureAwait(true);
        if (settings.OverlayLeft is double left && settings.OverlayTop is double top)
        {
            Left = left;
            Top = top;
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 8;
        Top = workArea.Bottom - Height - 8;
    }

    private async Task PersistPositionAsync()
    {
        var settings = await _configurationStore.LoadSettingsAsync().ConfigureAwait(true);
        settings.OverlayLeft = Left;
        settings.OverlayTop = Top;
        await _configurationStore.SaveSettingsAsync(settings).ConfigureAwait(true);
    }

    private void RootGrid_OnMouseEnter(object sender, MouseEventArgs e)
    {
        _viewModel.RequestExpand(true);
    }

    private void RootGrid_OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_isDraggingWindow)
        {
            _viewModel.RequestExpand(false);
        }
    }

    private void RootGrid_OnDragEnter(object sender, DragEventArgs e)
    {
        if (HasFileDrop(e))
        {
            _viewModel.IsExpanded = true;
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void RootGrid_OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = HasFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void RootGrid_OnDragLeave(object sender, DragEventArgs e)
    {
        // Keep expanded while user aims at a slot.
    }

    private async void RootGrid_OnDrop(object sender, DragEventArgs e)
    {
        var files = GetDroppedFiles(e);
        if (files.Count == 0)
        {
            return;
        }

        await _viewModel.HandleDropAsync(null, files).ConfigureAwait(true);
    }

    private async void UtilitySlot_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: UtilitySlotViewModel slot })
        {
            await _viewModel.ExecuteUtilityCommand.ExecuteAsync(slot).ConfigureAwait(true);
        }
    }

    private void UtilitySlot_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (sender is Button { Tag: UtilitySlotViewModel slot } && HasFileDrop(e))
        {
            var files = GetDroppedFiles(e);
            e.Effects = slot.CanAcceptDrop(files) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }
    }

    private async void UtilitySlot_OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not Button { Tag: UtilitySlotViewModel slot })
        {
            return;
        }

        var files = GetDroppedFiles(e);
        e.Handled = true;
        await _viewModel.HandleDropAsync(slot, files).ConfigureAwait(true);
    }

    private void HubButton_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _isDraggingWindow = false;
    }

    private void HubButton_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        var dx = Math.Abs(current.X - _dragStart.Value.X);
        var dy = Math.Abs(current.Y - _dragStart.Value.Y);
        if (dx > 4 || dy > 4)
        {
            _isDraggingWindow = true;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // Ignore if mouse button released mid-drag.
            }
        }
    }

    private async void HubButton_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingWindow)
        {
            await PersistPositionAsync().ConfigureAwait(true);
        }

        _dragStart = null;
        _isDraggingWindow = false;
    }

    private void HubButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_isDraggingWindow)
        {
            return;
        }

        if (!_viewModel.ExpandOnHover)
        {
            _viewModel.ToggleExpand();
        }
    }

    private void TrayIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        => ShowOverlay();

    private void TrayShow_OnClick(object sender, RoutedEventArgs e)
        => ShowOverlay();

    private void TrayHide_OnClick(object sender, RoutedEventArgs e)
        => Hide();

    private void TraySettings_OnClick(object sender, RoutedEventArgs e)
        => OpenSettings();

    private void TrayExit_OnClick(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    private void ShowOverlay()
    {
        Show();
        // Do not Activate() to avoid stealing focus aggressively.
    }

    private void OpenSettings()
    {
        var app = (App)Application.Current;
        var window = app.GetRequiredService<SettingsWindow>();
        window.Owner = this;
        window.Show();
        window.Activate();
    }

    private static bool HasFileDrop(DragEventArgs e)
        => e.Data.GetDataPresent(DataFormats.FileDrop);

    private static IReadOnlyList<string> GetDroppedFiles(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return Array.Empty<string>();
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return Array.Empty<string>();
        }

        // Security: only existing files, no directories, reject path traversal segments.
        return paths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Where(p => !p.Contains("..", StringComparison.Ordinal))
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Where(File.Exists)
            .ToList();
    }
}
