using System.Net;
using System.Text;
using Moq;
using TakeTool.Core.Abstractions;
using TakeTool.Core.Configuration;
using TakeTool.Utilities.ImgBB;

namespace TakeTool.Utilities.Tests;

public sealed class ImgBBClientTests
{
    [Fact]
    public async Task UploadAsync_ReturnsUrl_OnSuccess()
    {
        var handler = new StubHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """{"success":true,"data":{"url":"https://i.ibb.co/abc/test.png"}}"""));
        var client = new ImgBBClient(new HttpClient(handler));
        var file = CreateTempImage();

        try
        {
            var result = await client.UploadAsync("test-key", file);
            Assert.True(result.Success);
            Assert.Equal("https://i.ibb.co/abc/test.png", result.Url);
            Assert.Contains("key=test-key", handler.LastRequestUri, StringComparison.Ordinal);
            Assert.Equal(HttpMethod.Post, handler.LastMethod);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task UploadAsync_Fails_OnApiError()
    {
        var handler = new StubHandler(_ =>
            JsonResponse(HttpStatusCode.BadRequest, """{"success":false,"error":{"message":"Invalid API key"}}"""));
        var client = new ImgBBClient(new HttpClient(handler));
        var file = CreateTempImage();

        try
        {
            var result = await client.UploadAsync("bad-key", file);
            Assert.False(result.Success);
            Assert.Contains("Invalid API key", result.ErrorMessage);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task UploadAsync_RejectsNonHttpsUrl()
    {
        var handler = new StubHandler(_ =>
            JsonResponse(HttpStatusCode.OK, """{"success":true,"data":{"url":"http://insecure.example/a.png"}}"""));
        var client = new ImgBBClient(new HttpClient(handler));
        var file = CreateTempImage();

        try
        {
            var result = await client.UploadAsync("key", file);
            Assert.False(result.Success);
            Assert.Contains("non-HTTPS", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task UploadAsync_Fails_WhenApiKeyMissing()
    {
        var client = new ImgBBClient(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        var result = await client.UploadAsync(" ", Path.GetTempFileName());
        Assert.False(result.Success);
        Assert.Contains("API key", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempImage()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(path, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        return path;
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode code, string json)
        => new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public string? LastRequestUri { get; private set; }

        public HttpMethod? LastMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            LastMethod = request.Method;
            return Task.FromResult(_responder(request));
        }
    }
}

public sealed class ImgBBUploaderUtilityTests
{
    [Fact]
    public void CanAccept_RejectsNonImage()
    {
        var utility = CreateUtility(out _, out _, out _);
        var context = new UtilityContext
        {
            FilePaths = ["C:\\temp\\notes.txt"],
            Trigger = UtilityTrigger.Drop
        };
        Assert.False(utility.CanAccept(context));
    }

    [Fact]
    public void CanAccept_AcceptsPng()
    {
        var utility = CreateUtility(out _, out _, out _);
        var context = new UtilityContext
        {
            FilePaths = ["C:\\temp\\photo.png"],
            Trigger = UtilityTrigger.Drop
        };
        Assert.True(utility.CanAccept(context));
    }

    [Fact]
    public async Task ExecuteAsync_Fails_WhenApiKeyMissing()
    {
        var utility = CreateUtility(out var store, out var clipboard, out var notifications);
        store.Configs["imgbb-uploader"] = new Dictionary<string, string>();

        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        await File.WriteAllBytesAsync(file, [1, 2, 3]);
        try
        {
            var result = await utility.ExecuteAsync(new UtilityContext
            {
                FilePaths = [file],
                Trigger = UtilityTrigger.Drop
            });

            Assert.False(result.Success);
            Assert.Contains("API key", result.Message, StringComparison.OrdinalIgnoreCase);
            clipboard.Verify(c => c.SetTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            notifications.Verify(n => n.ShowAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static ImgBBUploaderUtility CreateUtility(
        out InMemoryStore store,
        out Mock<IClipboardService> clipboard,
        out Mock<INotificationService> notifications)
    {
        store = new InMemoryStore();
        clipboard = new Mock<IClipboardService>();
        notifications = new Mock<INotificationService>();
        notifications
            .Setup(n => n.ShowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FixedHandler("""{"success":true,"data":{"url":"https://i.ibb.co/x/y.png"}}""");
        var client = new ImgBBClient(new HttpClient(handler));
        return new ImgBBUploaderUtility(store, client, clipboard.Object, notifications.Object);
    }

    private sealed class FixedHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FixedHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
    }

    private sealed class InMemoryStore : IConfigurationStore
    {
        public Dictionary<string, Dictionary<string, string>> Configs { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppSettings());

        public Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyDictionary<string, string>> LoadUtilityConfigAsync(
            string utilityId,
            CancellationToken cancellationToken = default)
        {
            if (Configs.TryGetValue(utilityId, out var values))
            {
                return Task.FromResult<IReadOnlyDictionary<string, string>>(values);
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public Task SaveUtilityConfigAsync(
            string utilityId,
            IReadOnlyDictionary<string, string> values,
            CancellationToken cancellationToken = default)
        {
            Configs[utilityId] = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
            return Task.CompletedTask;
        }
    }
}
