namespace TakeTool.Utilities.ImgBB;

public sealed class ImgBBUploadResult
{
    public bool Success { get; init; }

    public string? Url { get; init; }

    public string? ErrorMessage { get; init; }

    public static ImgBBUploadResult Ok(string url)
        => new() { Success = true, Url = url };

    public static ImgBBUploadResult Fail(string message)
        => new() { Success = false, ErrorMessage = message };
}
