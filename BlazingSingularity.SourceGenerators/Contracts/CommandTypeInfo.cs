namespace BlazingSingularity.SourceGenerators.Contracts;

/// <summary>
/// Represents the type and constructor arguments for a relay command
/// </summary>
public sealed class CommandTypeInfo(string commandType, string constructorArgs)
{
    public string CommandType { get; } = commandType;
    public string ConstructorArgs { get; } = constructorArgs;
}
