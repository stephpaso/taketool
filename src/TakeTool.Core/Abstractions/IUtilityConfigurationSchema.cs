namespace TakeTool.Core.Abstractions;

/// <summary>
/// Describes the configurable fields exposed by a utility for the Settings UI.
/// </summary>
public interface IUtilityConfigurationSchema
{
    IReadOnlyList<ConfigurationFieldDefinition> Fields { get; }
}
