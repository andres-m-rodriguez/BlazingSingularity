namespace BlazingSingularity.Endpoints;

/// <summary>
/// Specifies that a parameter should be bound from the query string.
/// Only needed for edge cases - primitives not in route are auto-detected as query params.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromQueryAttribute : Attribute
{
    public string? Name { get; set; }
}

/// <summary>
/// Specifies that a parameter should be bound from the route.
/// Only needed for edge cases - primitives in route pattern are auto-detected.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromRouteAttribute : Attribute
{
    public string? Name { get; set; }
}

/// <summary>
/// Specifies that a parameter should be bound from the request body.
/// Only needed for edge cases - complex types with POST/PUT are auto-detected as body.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromBodyAttribute : Attribute { }

/// <summary>
/// Specifies that a parameter should be resolved from dependency injection.
/// Only needed for edge cases - interfaces and service classes are auto-detected.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class FromServicesAttribute : Attribute { }
