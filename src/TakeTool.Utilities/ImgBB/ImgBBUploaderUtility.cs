using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Platform;
using TakeTool.Core.Utilities;

namespace TakeTool.Utilities.ImgBB;

public sealed class ImgBBUploaderUtility : BaseUtility
{
    public const string UtilityId = "imgbb-uploader";
    public const string ApiKeyField = "ApiKey";

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

    private readonly ImgBBClient _client;
    private readonly IClipboardService _clipboard;
    private readonly INotificationService _notifications;

    public ImgBBUploaderUtility(
        IConfigurationStore configurationStore,
        ImgBBClient client,
        IClipboardService clipboard,
        INotificationService notifications)
        : base(configurationStore)
    {
        _client = client;
        _clipboard = clipboard;
        _notifications = notifications;
    }

    public override UtilityMetadata Metadata { get; } = new()
    {
        Id = UtilityId,
        Name = "ImgBB Uploader",
        Description = "Upload an image to ImgBB and copy the public HTTPS link to the clipboard.",
        IconKey = "imgbb",
        SupportedPlatforms = UtilityPlatform.Any,
        AcceptedInputKinds = UtilityInputKind.Files,
        AcceptedMimeTypes = ["image/*"],
        AcceptedExtensions = ImageExtensions
    };

    public override IUtilityConfigurationSchema ConfigurationSchema { get; } =
        new ImgBBConfigurationSchema();

    public override async Task<UtilityResult> ExecuteAsync(
        UtilityContext context,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccept(context))
        {
            return UtilityResult.Fail("ImgBB Uploader accepts image files only (.png, .jpg, .jpeg, .gif, .webp).");
        }

        if (context.FilePaths.Count == 0)
        {
            return UtilityResult.Fail("Drop an image file onto ImgBB Uploader.");
        }

        var filePath = context.FilePaths[0];
        if (!File.Exists(filePath))
        {
            return UtilityResult.Fail("The selected file no longer exists.");
        }

        var config = await LoadConfigAsync(cancellationToken).ConfigureAwait(false);
        if (!config.TryGetValue(ApiKeyField, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            var missing = UtilityResult.Fail("ImgBB API key is not configured. Open Settings to add it.");
            await _notifications.ShowAsync("ImgBB", missing.Message, isError: true, cancellationToken)
                .ConfigureAwait(false);
            return missing;
        }

        var upload = await _client.UploadAsync(apiKey, filePath, cancellationToken).ConfigureAwait(false);
        if (!upload.Success || string.IsNullOrWhiteSpace(upload.Url))
        {
            var fail = UtilityResult.Fail(upload.ErrorMessage ?? "Upload failed.");
            await _notifications.ShowAsync("ImgBB", fail.Message, isError: true, cancellationToken)
                .ConfigureAwait(false);
            return fail;
        }

        await _clipboard.SetTextAsync(upload.Url, cancellationToken).ConfigureAwait(false);
        var ok = UtilityResult.Ok("Image uploaded. Public link copied to clipboard.", upload.Url);
        await _notifications.ShowAsync("ImgBB", ok.Message, isError: false, cancellationToken)
            .ConfigureAwait(false);
        return ok;
    }

    private sealed class ImgBBConfigurationSchema : IUtilityConfigurationSchema
    {
        public IReadOnlyList<ConfigurationFieldDefinition> Fields { get; } =
        [
            new ConfigurationFieldDefinition
            {
                Key = ApiKeyField,
                DisplayName = "API Key",
                Description = "API key from https://api.imgbb.com/",
                FieldType = ConfigurationFieldType.Password,
                IsRequired = true
            }
        ];
    }
}
