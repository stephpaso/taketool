using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TakeTool.Core.Abstractions;
using TakeTool.Core.DependencyInjection;
using TakeTool.Core.Security;
using TakeTool.UI.Windows.Services;
using TakeTool.UI.Windows.ViewModels;
using TakeTool.UI.Windows.Views;
using TakeTool.Utilities.DependencyInjection;

namespace TakeTool.UI.Windows;

public partial class App : Application
{
    private IHost? _host;
    private OverlayWindow? _overlayWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Register DPAPI before Core so TryAddSingleton keeps this protector.
                services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
                services.AddTakeToolCore();
                services.AddTakeToolUtilities();
                services.AddSingleton<IClipboardService, WindowsClipboardService>();
                services.AddSingleton<INotificationService, WindowsNotificationService>();
                services.AddSingleton<OverlayViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddSingleton<OverlayWindow>();
                services.AddTransient<SettingsWindow>();
            })
            .Build();

        await _host.StartAsync().ConfigureAwait(true);

        _overlayWindow = _host.Services.GetRequiredService<OverlayWindow>();
        MainWindow = _overlayWindow;
        _overlayWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(true);
            _host.Dispose();
        }

        base.OnExit(e);
    }

    public T GetRequiredService<T>() where T : notnull
    {
        if (_host is null)
        {
            throw new InvalidOperationException("Host is not initialized.");
        }

        return _host.Services.GetRequiredService<T>();
    }
}
