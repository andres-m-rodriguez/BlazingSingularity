#nullable enable
using System.Collections.Generic;
using System.Linq;
using BlazingSingularity.SourceGenerators.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;

/// <summary>
/// Extracts EndpointInfo from method declarations with [Endpoint] attribute
/// </summary>
public static class EndpointInfoExtractor
{
    /// <summary>
    /// Extracts endpoint information from a method symbol
    /// </summary>
    public static EndpointInfo? ExtractEndpointInfo(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
        if (methodSymbol == null)
            return null;

        // Get the [Endpoint] attribute
        var endpointAttribute = GetEndpointAttribute(method, semanticModel);
        if (endpointAttribute == null)
            return null;

        // Extract HTTP method and route from attribute
        var (httpMethod, route) = ExtractAttributeValues(endpointAttribute, semanticModel);
        if (httpMethod == null || route == null)
            return null;

        // Get return type
        var returnType = GetReturnType(methodSymbol);

        // Get route parameters for convention-based detection
        var routeParams = EndpointHelpers.ExtractRouteParameters(route);

        // Extract parameters
        var parameters = ExtractParameters(methodSymbol, httpMethod, routeParams);

        // Get method body
        var methodBody = ExtractMethodBody(method);

        return new EndpointInfo(
            methodName: methodSymbol.Name,
            httpMethod: httpMethod,
            route: route,
            returnType: returnType,
            parameters: parameters,
            methodBody: methodBody
        );
    }

    private static AttributeSyntax? GetEndpointAttribute(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
                {
                    var fullName = attributeSymbol.ContainingType.ToDisplayString();
                    if (fullName == "BlazingSingularity.Endpoints.EndpointAttribute")
                    {
                        return attribute;
                    }
                }
            }
        }
        return null;
    }

    private static (string? httpMethod, string? route) ExtractAttributeValues(
        AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        string? httpMethod = null;
        string? route = null;

        if (attribute.ArgumentList?.Arguments.Count >= 2)
        {
            // First argument: HttpMethod enum
            var httpMethodArg = attribute.ArgumentList.Arguments[0];
            var httpMethodValue = semanticModel.GetConstantValue(httpMethodArg.Expression);
            if (httpMethodValue.HasValue && httpMethodValue.Value is int httpMethodInt)
            {
                httpMethod = httpMethodInt switch
                {
                    0 => "Get",
                    1 => "Post",
                    2 => "Put",
                    3 => "Delete",
                    4 => "Patch",
                    _ => "Get"
                };
            }
            else
            {
                // Try to extract from member access expression
                if (httpMethodArg.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    httpMethod = memberAccess.Name.Identifier.Text;
                }
            }

            // Second argument: Route string
            var routeArg = attribute.ArgumentList.Arguments[1];
            var routeValue = semanticModel.GetConstantValue(routeArg.Expression);
            if (routeValue.HasValue && routeValue.Value is string routeString)
            {
                route = routeString;
            }
        }

        return (httpMethod, route);
    }

    // Use fully qualified format to avoid missing using directives in generated code
    private static readonly SymbolDisplayFormat FullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    private static string GetReturnType(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;

        // Handle Task<T> - unwrap to get T
        if (returnType is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>")
        {
            if (namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0].ToDisplayString(FullyQualifiedFormat);
            }
        }

        // Handle ValueTask<T>
        if (returnType is INamedTypeSymbol valueTaskType &&
            valueTaskType.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.ValueTask<TResult>")
        {
            if (valueTaskType.TypeArguments.Length == 1)
            {
                return valueTaskType.TypeArguments[0].ToDisplayString(FullyQualifiedFormat);
            }
        }

        // Handle void/Task
        if (returnType.SpecialType == SpecialType.System_Void ||
            returnType.ToDisplayString() == "System.Threading.Tasks.Task" ||
            returnType.ToDisplayString() == "System.Threading.Tasks.ValueTask")
        {
            return "void";
        }

        return returnType.ToDisplayString(FullyQualifiedFormat);
    }

    private static EndpointParameterInfo[] ExtractParameters(
        IMethodSymbol methodSymbol,
        string httpMethod,
        HashSet<string> routeParams)
    {
        var parameters = new List<EndpointParameterInfo>();

        foreach (var param in methodSymbol.Parameters)
        {
            var source = EndpointHelpers.DetermineParameterSource(param, httpMethod, routeParams);
            var isNullable = param.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                             param.HasExplicitDefaultValue;

            parameters.Add(new EndpointParameterInfo(
                name: param.Name,
                type: param.Type.ToDisplayString(FullyQualifiedFormat),
                source: source,
                isNullable: isNullable
            ));
        }

        return parameters.ToArray();
    }

    private static string ExtractMethodBody(MethodDeclarationSyntax method)
    {
        // Get the body or expression body
        if (method.Body != null)
        {
            // Return the inner statements without the braces
            return method.Body.Statements.ToFullString();
        }
        else if (method.ExpressionBody != null)
        {
            return $"return {method.ExpressionBody.Expression.ToFullString()};";
        }

        return string.Empty;
    }
}
