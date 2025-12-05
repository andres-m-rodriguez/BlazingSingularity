using Microsoft.CodeAnalysis;

namespace BlazingSingularity.SourceGenerators.Contracts;

/// <summary>
/// Represents a command parameter after filtering out CancellationToken
/// </summary>
public sealed class CommandParameterInfo(
    IParameterSymbol[] commandParameters,
    bool hasCancellationToken
)
{
    public IParameterSymbol[] CommandParameters { get; } = commandParameters;
    public bool HasCancellationToken { get; } = hasCancellationToken;
}
