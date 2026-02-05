namespace BlazingSingularity.Endpoints;

/// <summary>
/// Marks a method as an HTTP endpoint. The generator will create both a server-side
/// minimal API endpoint and a client-side HTTP client method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EndpointAttribute : Attribute
{
    public HttpVerb Method { get; }
    public string Route { get; }

    public EndpointAttribute(HttpVerb method, string route)
    {
        Method = method;
        Route = route;
    }
}

/// <summary>
/// HTTP verbs for endpoints. Named HttpVerb to avoid conflict with System.Net.Http.HttpMethod.
/// </summary>
public enum HttpVerb
{
    Get,
    Post,
    Put,
    Delete,
    Patch
}
