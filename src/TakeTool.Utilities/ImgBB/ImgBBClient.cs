using System.Net.Http.Headers;
using System.Text.Json;
using TakeTool.Core.Security;

namespace TakeTool.Utilities.ImgBB;

public sealed class ImgBBClient
{
    public const string UploadEndpoint = "https://api.imgbb.com/1/upload";

    private readonly HttpClient _httpClient;

    public ImgBBClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ImgBBUploadResult> UploadAsync(
        string apiKey,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ImgBBUploadResult.Fail("ImgBB API key is missing. Configure it in Settings.");
        }

        if (!SafeFileAccess.IsSafeRegularFile(filePath, out var fullPath) || fullPath is null)
        {
            return ImgBBUploadResult.Fail("Image file was not found or is not a regular file.");
        }

        if (!SafeFileAccess.HasImageMagicBytes(fullPath))
        {
            return ImgBBUploadResult.Fail("File content is not a recognized image format.");
        }

        var fileInfo = new FileInfo(fullPath);

        // Reasonable size cap (32 MB) to avoid accidental huge uploads.
        const long maxBytes = 32L * 1024 * 1024;
        if (fileInfo.Length > maxBytes)
        {
            return ImgBBUploadResult.Fail("Image exceeds the 32 MB upload limit.");
        }

        await using var fileStream = File.OpenRead(fullPath);
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GuessContentType(fullPath));
        content.Add(streamContent, "image", Path.GetFileName(fullPath));

        var requestUri = $"{UploadEndpoint}?key={Uri.EscapeDataString(apiKey)}";
        using var response = await _httpClient
            .PostAsync(requestUri, content, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return ImgBBUploadResult.Fail(ExtractErrorMessage(body) ?? $"Upload failed ({(int)response.StatusCode}).");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("success", out var successProp) &&
                successProp.ValueKind == JsonValueKind.False)
            {
                return ImgBBUploadResult.Fail(ExtractErrorMessage(body) ?? "ImgBB reported failure.");
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return ImgBBUploadResult.Fail("ImgBB response did not contain data.");
            }

            string? url = null;
            if (data.TryGetProperty("url", out var urlProp))
            {
                url = urlProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(url) && data.TryGetProperty("display_url", out var displayProp))
            {
                url = displayProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return ImgBBUploadResult.Fail("ImgBB response did not contain a public URL.");
            }

            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return ImgBBUploadResult.Fail("ImgBB returned a non-HTTPS URL which was rejected.");
            }

            return ImgBBUploadResult.Ok(url);
        }
        catch (JsonException)
        {
            return ImgBBUploadResult.Fail("Could not parse ImgBB response.");
        }
    }

    private static string GuessContentType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

    private static string? ExtractErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch (JsonException)
        {
            // Ignore parse errors for error extraction.
        }

        return null;
    }
}
