#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BlazingSingularity.SourceGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;

/// <summary>
/// Helper methods for endpoint source generation
/// </summary>
public static class EndpointHelpers
{
    private static readonly HashSet<string> PrimitiveTypes = new()
    {
        "string", "String", "System.String",
        "int", "Int32", "System.Int32",
        "long", "Int64", "System.Int64",
        "short", "Int16", "System.Int16",
        "byte", "Byte", "System.Byte",
        "bool", "Boolean", "System.Boolean",
        "decimal", "Decimal", "System.Decimal",
        "double", "Double", "System.Double",
        "float", "Single", "System.Single",
        "char", "Char", "System.Char",
        "Guid", "System.Guid",
        "DateTime", "System.DateTime",
        "DateTimeOffset", "System.DateTimeOffset",
        "DateOnly", "System.DateOnly",
        "TimeOnly", "System.TimeOnly",
        "TimeSpan", "System.TimeSpan"
    };

    /// <summary>
    /// Extracts route parameters from a route template (e.g., "/api/todos/{id}" -> ["id"])
    /// </summary>
    public static HashSet<string> ExtractRouteParameters(string route)
    {
        var result = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(route, @"\{(\w+)\}");
        foreach (Match match in matches)
        {
            result.Add(match.Groups[1].Value);
        }
        return result;
    }

    /// <summary>
    /// Determines the source of a parameter based on conventions and attributes
    /// </summary>
    public static ParameterSource DetermineParameterSource(
        IParameterSymbol parameter,
        string httpMethod,
        HashSet<string> routeParams)
    {
        // Check for explicit attributes first
        foreach (var attr in parameter.GetAttributes())
        {
            var attrName = attr.AttributeClass?.ToDisplayString();
            if (attrName == "BlazingSingularity.Endpoints.FromQueryAttribute")
                return ParameterSource.Query;
            if (attrName == "BlazingSingularity.Endpoints.FromRouteAttribute")
                return ParameterSource.Route;
            if (attrName == "BlazingSingularity.Endpoints.FromBodyAttribute")
                return ParameterSource.Body;
            if (attrName == "BlazingSingularity.Endpoints.FromServicesAttribute")
                return ParameterSource.Services;
        }

        // Convention-based detection
        var typeName = parameter.Type.ToDisplayString();
        var isNullableValueType = parameter.Type.IsValueType &&
            parameter.Type.NullableAnnotation == NullableAnnotation.Annotated;
        var baseTypeName = isNullableValueType ?
            typeName.TrimEnd('?') : typeName;

        // Check if it's in the route
        if (routeParams.Contains(parameter.Name))
            return ParameterSource.Route;

        // Primitives are query params
        if (IsPrimitiveType(baseTypeName))
            return ParameterSource.Query;

        // Interfaces and abstract classes are services
        if (parameter.Type.TypeKind == TypeKind.Interface ||
            (parameter.Type is INamedTypeSymbol namedType && namedType.IsAbstract))
            return ParameterSource.Services;

        // Classes ending with common service suffixes are services
        if (typeName.EndsWith("Service") ||
            typeName.EndsWith("Repository") ||
            typeName.EndsWith("Client") ||
            typeName.EndsWith("Handler") ||
            typeName.EndsWith("Manager") ||
            typeName.EndsWith("Provider"))
            return ParameterSource.Services;

        // Complex types with POST/PUT/PATCH are body
        if (httpMethod is "Post" or "Put" or "Patch")
            return ParameterSource.Body;

        // Default to services for other complex types
        return ParameterSource.Services;
    }

    /// <summary>
    /// Checks if a type is considered primitive for parameter binding
    /// </summary>
    public static bool IsPrimitiveType(string typeName)
    {
        var cleanName = typeName.TrimEnd('?');
        return PrimitiveTypes.Contains(cleanName);
    }

    /// <summary>
    /// Gets the endpoint property name from method name
    /// </summary>
    public static string GetEndpointPropertyName(string methodName)
    {
        return methodName + "Endpoint";
    }

    /// <summary>
    /// Gets the HTTP client interface name from class name
    /// </summary>
    public static string GetHttpClientInterfaceName(string className)
    {
        return "I" + className + "HttpClient";
    }

    /// <summary>
    /// Gets the HTTP client class name from class name
    /// </summary>
    public static string GetHttpClientClassName(string className)
    {
        return className + "HttpClient";
    }

    /// <summary>
    /// Maps our HttpMethod enum to ASP.NET method name
    /// </summary>
    public static string GetAspNetMethodName(string httpMethod)
    {
        return httpMethod switch
        {
            "Get" => "MapGet",
            "Post" => "MapPost",
            "Put" => "MapPut",
            "Delete" => "MapDelete",
            "Patch" => "MapPatch",
            _ => "MapGet"
        };
    }

    /// <summary>
    /// Maps our HttpMethod to HttpClient method
    /// </summary>
    public static string GetHttpClientMethod(string httpMethod, bool hasBody)
    {
        return httpMethod switch
        {
            "Get" => "GetFromJsonAsync",
            "Post" => "PostAsJsonAsync",
            "Put" => "PutAsJsonAsync",
            "Delete" => hasBody ? "DeleteAsync" : "DeleteAsync",
            "Patch" => "PatchAsJsonAsync",
            _ => "GetFromJsonAsync"
        };
    }
}
