using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Security;

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
        if (!SafeFileAccess.IsSafeRegularFile(filePath, out var fullPath) || fullPath is null)
        {
            return false;
        }

        var extension = Path.GetExtension(fullPath);
        if (string.IsNullOrEmpty(extension))
        {
            return Metadata.AcceptedExtensions.Count == 0 && Metadata.AcceptedMimeTypes.Count == 0;
        }

        if (Metadata.AcceptedExtensions.Count > 0)
        {
            var extensionOk = Metadata.AcceptedExtensions.Any(ext =>
                string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
            if (!extensionOk)
            {
                return false;
            }
        }
        else if (Metadata.AcceptedMimeTypes.Count > 0)
        {
            var mime = GuessMimeType(extension);
            if (!Metadata.AcceptedMimeTypes.Any(pattern => MatchesMime(pattern, mime)))
            {
                return false;
            }
        }

        // For image/* utilities, require magic-byte confirmation so renamed
        // non-images cannot pass extension-only checks.
        if (Metadata.AcceptedMimeTypes.Any(m =>
                m.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m, "image/*", StringComparison.OrdinalIgnoreCase)))
        {
            return SafeFileAccess.HasImageMagicBytes(fullPath);
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
