using Microsoft.Extensions.DependencyInjection;
using TakeTool.Core.Configuration;
using TakeTool.Core.Services;

namespace TakeTool.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTakeToolCore(this IServiceCollection services)
    {
        services.AddSingleton<IAppDataPathProvider, AppDataPathProvider>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.AddSingleton<UtilityRegistry>();
        return services;
    }
}
