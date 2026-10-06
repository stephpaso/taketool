using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;

namespace TakeTool.Core.Utilities;

public abstract class BaseUtility : IUtility
{
    private readonly IConfigurationStore _configurationStore;

    protected BaseUtility(IConfigurationStore configurationStore)
    {
        _configurationStore = configurationStore;
    }

    public abstract UtilityMetadata Metadata { get; }

    public abstract IUtilityConfigurationSchema ConfigurationSchema { get; }

    public virtual bool CanAccept(UtilityContext context)
    {
        if (context.Trigger == UtilityTrigger.Click)
        {
            return Metadata.AcceptedInputKinds.HasFlag(UtilityInputKind.Click);
        }

        if (context.FilePaths.Count > 0)
        {
            if (!Metadata.AcceptedInputKinds.HasFlag(UtilityInputKind.Files))
            {
                return false;
            }

            return context.FilePaths.All(IsAcceptedFile);
        }

        if (!string.IsNullOrWhiteSpace(context.Text))
        {
            return Metadata.AcceptedInputKinds.HasFlag(UtilityInputKind.Text);
        }

        return false;
    }

    public abstract Task<UtilityResult> ExecuteAsync(
        UtilityContext context,
        CancellationToken cancellationToken = default);

    protected async Task<IReadOnlyDictionary<string, string>> LoadConfigAsync(
        CancellationToken cancellationToken = default)
        => await _configurationStore.LoadUtilityConfigAsync(Metadata.Id, cancellationToken)
            .ConfigureAwait(false);

    protected bool IsAcceptedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        // Reject path traversal / invalid paths early.
        if (filePath.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
        {
            return Metadata.AcceptedExtensions.Count == 0 && Metadata.AcceptedMimeTypes.Count == 0;
        }

        if (Metadata.AcceptedExtensions.Count > 0)
        {
            return Metadata.AcceptedExtensions.Any(ext =>
                string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
        }

        if (Metadata.AcceptedMimeTypes.Count > 0)
        {
            var mime = GuessMimeType(extension);
            return Metadata.AcceptedMimeTypes.Any(pattern => MatchesMime(pattern, mime));
        }

        return true;
    }

    protected static string GuessMimeType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

    private static bool MatchesMime(string pattern, string mime)
    {
        if (string.Equals(pattern, mime, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (pattern.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = pattern[..^1]; // keep trailing '/'
            return mime.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
