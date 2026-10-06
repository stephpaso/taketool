using System.Windows;
using System.Windows.Controls;
using TakeTool.UI.Windows.ViewModels;

namespace TakeTool.UI.Windows.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly OverlayViewModel _overlayViewModel;

    public SettingsWindow(SettingsViewModel viewModel, OverlayViewModel overlayViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _overlayViewModel = overlayViewModel;
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync().ConfigureAwait(true);
        SyncPasswordBoxes(this);
    }

    private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Ensure password boxes flush into view models before save.
        FlushPasswordBoxes(this);
        await _viewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(true);
        _overlayViewModel.Opacity = _viewModel.OverlayOpacity;
        _overlayViewModel.ExpandOnHover = _viewModel.ExpandOnHover;
        await _overlayViewModel.ReloadSlotsAsync().ConfigureAwait(true);
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox { Tag: ConfigurationFieldViewModel field })
        {
            field.Value = ((PasswordBox)sender).Password;
        }
    }

    private static void SyncPasswordBoxes(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is PasswordBox { Tag: ConfigurationFieldViewModel field } box)
            {
                box.Password = field.Value;
            }
            else if (child is DependencyObject dep)
            {
                SyncPasswordBoxes(dep);
            }
        }
    }

    private static void FlushPasswordBoxes(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is PasswordBox { Tag: ConfigurationFieldViewModel field } box)
            {
                field.Value = box.Password;
            }
            else if (child is DependencyObject dep)
            {
                FlushPasswordBoxes(dep);
            }
        }
    }
}
