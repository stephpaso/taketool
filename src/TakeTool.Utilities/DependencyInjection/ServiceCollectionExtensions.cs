using Microsoft.Extensions.DependencyInjection;
using TakeTool.Core.Abstractions;
using TakeTool.Utilities.ImgBB;

namespace TakeTool.Utilities.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public const string ImgBBHttpClientName = "ImgBB";

    public static IServiceCollection AddTakeToolUtilities(this IServiceCollection services)
    {
        services.AddHttpClient(ImgBBHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TakeTool/1.0");
        });

        services.AddSingleton<ImgBBClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new ImgBBClient(factory.CreateClient(ImgBBHttpClientName));
        });

        services.AddSingleton<IUtility, ImgBBUploaderUtility>();
        return services;
    }
}
