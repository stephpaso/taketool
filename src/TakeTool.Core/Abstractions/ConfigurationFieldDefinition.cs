namespace TakeTool.Core.Abstractions;

public sealed class ConfigurationFieldDefinition
{
    public required string Key { get; init; }

    public required string DisplayName { get; init; }

    public string? Description { get; init; }

    public ConfigurationFieldType FieldType { get; init; } = ConfigurationFieldType.String;

    public bool IsRequired { get; init; }

    public string? DefaultValue { get; init; }
}
