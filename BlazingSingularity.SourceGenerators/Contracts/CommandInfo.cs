namespace BlazingSingularity.SourceGenerators.Contracts;

/// <summary>
/// Represents a command to be generated with all its properties
/// </summary>
public sealed class CommandInfo(
    string commandType,
    string commandPropertyName,
    string commandFieldName,
    string constructorArgs
)
{
    public string CommandType { get; } = commandType;
    public string CommandPropertyName { get; } = commandPropertyName;
    public string CommandFieldName { get; } = commandFieldName;
    public string ConstructorArgs { get; } = constructorArgs;
}
