namespace TakeTool.Core.Abstractions;

public interface INotificationService
{
    Task ShowAsync(string title, string message, bool isError = false, CancellationToken cancellationToken = default);
}
