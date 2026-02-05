using System.Linq;

namespace BlazingSingularity.SourceGenerators.Contracts;

/// <summary>
/// Represents an endpoint to be generated
/// </summary>
public sealed class EndpointInfo(
    string methodName,
    string httpMethod,
    string route,
    string returnType,
    EndpointParameterInfo[] parameters,
    string methodBody
)
{
    public string MethodName { get; } = methodName;
    public string HttpMethod { get; } = httpMethod;
    public string Route { get; } = route;
    public string ReturnType { get; } = returnType;
    public EndpointParameterInfo[] Parameters { get; } = parameters;
    public string MethodBody { get; } = methodBody;

    /// <summary>
    /// Parameters that are request params (query, route, body) - used for HTTP client
    /// </summary>
    public EndpointParameterInfo[] RequestParameters =>
        Parameters.Where(p => p.Source != ParameterSource.Services).ToArray();

    /// <summary>
    /// Parameters that are DI services - server only
    /// </summary>
    public EndpointParameterInfo[] ServiceParameters =>
        Parameters.Where(p => p.Source == ParameterSource.Services).ToArray();
}

/// <summary>
/// Represents a parameter in an endpoint method
/// </summary>
public sealed class EndpointParameterInfo(
    string name,
    string type,
    ParameterSource source,
    bool isNullable
)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public ParameterSource Source { get; } = source;
    public bool IsNullable { get; } = isNullable;
}

/// <summary>
/// Where a parameter value comes from
/// </summary>
public enum ParameterSource
{
    Query,
    Route,
    Body,
    Services
}
