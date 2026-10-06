namespace TakeTool.Core.Abstractions;

public interface IUtility
{
    UtilityMetadata Metadata { get; }

    IUtilityConfigurationSchema ConfigurationSchema { get; }

    bool CanAccept(UtilityContext context);

    Task<UtilityResult> ExecuteAsync(UtilityContext context, CancellationToken cancellationToken = default);
}
