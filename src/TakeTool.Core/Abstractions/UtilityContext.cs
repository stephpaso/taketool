namespace TakeTool.Core.Abstractions;

public sealed class UtilityContext
{
    public IReadOnlyList<string> FilePaths { get; init; } = Array.Empty<string>();

    public string? Text { get; init; }

    public UtilityTrigger Trigger { get; init; } = UtilityTrigger.Click;
}
