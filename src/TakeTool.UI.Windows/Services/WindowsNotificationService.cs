using System.Windows;
using TakeTool.Core.Abstractions;
using TakeTool.UI.Windows.ViewModels;

namespace TakeTool.UI.Windows.Services;

public sealed class WindowsNotificationService : INotificationService
{
    public Task ShowAsync(
        string title,
        string message,
        bool isError = false,
        CancellationToken cancellationToken = default)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow is Views.OverlayWindow overlay &&
                overlay.DataContext is OverlayViewModel vm)
            {
                vm.ShowFeedback(title, message, isError);
            }
            else
            {
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    isError ? MessageBoxImage.Error : MessageBoxImage.Information);
            }
        });

        return Task.CompletedTask;
    }
}
