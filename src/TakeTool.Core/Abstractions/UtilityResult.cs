namespace TakeTool.Core.Abstractions;

public sealed class UtilityResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Payload { get; init; }

    public static UtilityResult Ok(string message, string? payload = null)
        => new() { Success = true, Message = message, Payload = payload };

    public static UtilityResult Fail(string message)
        => new() { Success = false, Message = message };
}
