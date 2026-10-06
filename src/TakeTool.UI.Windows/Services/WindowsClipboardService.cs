using System.Windows;
using TakeTool.Core.Abstractions;

namespace TakeTool.UI.Windows.Services;

public sealed class WindowsClipboardService : IClipboardService
{
    public Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                Clipboard.SetText(text);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }
}
