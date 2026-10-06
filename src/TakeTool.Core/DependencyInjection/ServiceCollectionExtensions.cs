using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TakeTool.Core.Configuration;
using TakeTool.Core.Security;
using TakeTool.Core.Services;

namespace TakeTool.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTakeToolCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IAppDataPathProvider, AppDataPathProvider>();
        // Hosts should register a platform protector before calling AddTakeToolCore.
        services.TryAddSingleton<ISecretProtector, PassthroughSecretProtector>();
        services.TryAddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.TryAddSingleton<UtilityRegistry>();
        return services;
    }
}
