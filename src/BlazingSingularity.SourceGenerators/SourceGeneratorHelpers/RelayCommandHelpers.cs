#nullable enable
using System.Collections.Generic;
using System.Linq;
using BlazingSingularity.SourceGenerators.Contracts;
using Microsoft.CodeAnalysis;

namespace BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;

public static class RelayCommandHelpers
{
    /// <summary>
    /// Extracts command parameters, filtering out CancellationToken if present
    /// </summary>
    public static CommandParameterInfo GetCommandParameters(IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters;

        // Check if the last parameter is CancellationToken
        var hasCancellationToken =
            parameters.Length > 0
            && parameters[parameters.Length - 1].Type.Name == "CancellationToken"
            && parameters[parameters.Length - 1].Type.ContainingNamespace.ToDisplayString()
                == "System.Threading";

        // Get non-CancellationToken parameters for command signature
        var commandParameters = hasCancellationToken
            ? parameters.Take(parameters.Length - 1).ToArray()
            : parameters.ToArray();

        return new CommandParameterInfo(commandParameters, hasCancellationToken);
    }

    /// <summary>
    /// Finds the corresponding CanExecute method for a command method
    /// </summary>
    public static IMethodSymbol? FindCanExecuteMethod(
        IMethodSymbol methodSymbol,
        IParameterSymbol[] commandParameters
    )
    {
        var canExecuteMethodName = "Can" + methodSymbol.Name;
        var classSymbol = methodSymbol.ContainingType;

        return classSymbol
            ?.GetMembers(canExecuteMethodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m =>
            {
                // CanExecute should return bool
                if (m.ReturnType.SpecialType != SpecialType.System_Boolean)
                    return false;

                // Should have same parameter signature as the command method (excluding CancellationToken)
                if (m.Parameters.Length != commandParameters.Length)
                    return false;

                // Check parameter types match
                for (int i = 0; i < commandParameters.Length; i++)
                {
                    if (
                        !SymbolEqualityComparer.Default.Equals(
                            m.Parameters[i].Type,
                            commandParameters[i].Type
                        )
                    )
                        return false;
                }

                return true;
            });
    }

    /// <summary>
    /// Determines the command type and constructor arguments based on method signature
    /// </summary>
    public static CommandTypeInfo? GetCommandTypeInfo(
        IMethodSymbol methodSymbol,
        IParameterSymbol[] commandParameters,
        IMethodSymbol? canExecuteMethod
    )
    {
        var returnType = methodSymbol.ReturnType;
        var isAsync = returnType.Name == "Task";
        var methodName = methodSymbol.Name;
        var canExecuteMethodName = canExecuteMethod != null ? "Can" + methodName : null;

        string commandType;
        string constructorArgs;

        if (commandParameters.Length == 0)
        {
            commandType = "AsyncRelayCommand";
            if (isAsync)
            {
                constructorArgs =
                    canExecuteMethod != null
                        ? $"{methodName}, {canExecuteMethodName}"
                        : $"{methodName}";
            }
            else
            {
                var wrapper = $"() => {{ {methodName}(); return System.Threading.Tasks.Task.CompletedTask; }}";
                constructorArgs =
                    canExecuteMethod != null
                        ? $"{wrapper}, {canExecuteMethodName}"
                        : wrapper;
            }
        }
        else if (commandParameters.Length == 1)
        {
            var paramType = commandParameters[0].Type.ToDisplayString();
            commandType = $"AsyncRelayCommand<{paramType}>";
            if (isAsync)
            {
                constructorArgs =
                    canExecuteMethod != null
                        ? $"{methodName}, {canExecuteMethodName}"
                        : $"{methodName}";
            }
            else
            {
                var wrapper = $"param => {{ {methodName}(param); return System.Threading.Tasks.Task.CompletedTask; }}";
                constructorArgs =
                    canExecuteMethod != null
                        ? $"{wrapper}, {canExecuteMethodName}"
                        : wrapper;
            }
        }
        else if (commandParameters.Length >= 2 && commandParameters.Length <= 8)
        {
            // Multiple parameters (2-8) - use ValueTuple
            var tupleTypes = string.Join(
                ", ",
                commandParameters.Select(p => p.Type.ToDisplayString())
            );
            var tupleType = $"({tupleTypes})";

            commandType = $"AsyncRelayCommand<{tupleType}>";

            // Generate lambda wrapper to unwrap tuple
            var tupleItems = string.Join(
                ", ",
                Enumerable.Range(1, commandParameters.Length).Select(i => $"param.Item{i}")
            );

            string lambdaWrapper;
            if (isAsync)
            {
                lambdaWrapper = $"param => {methodName}({tupleItems})";
            }
            else
            {
                lambdaWrapper = $"param => {{ {methodName}({tupleItems}); return System.Threading.Tasks.Task.CompletedTask; }}";
            }

            // Handle CanExecute
            if (canExecuteMethod != null)
            {
                var canExecuteLambda = $"param => {canExecuteMethodName}({tupleItems})";
                constructorArgs = $"{lambdaWrapper}, {canExecuteLambda}";
            }
            else
            {
                constructorArgs = lambdaWrapper;
            }
        }
        else
        {
            // More than 8 parameters not supported
            return null;
        }

        return new CommandTypeInfo(commandType, constructorArgs);
    }

    /// <summary>
    /// Generates the command property name from a method name
    /// </summary>
    public static string GetCommandPropertyName(string methodName)
    {
        return methodName + "Command";
    }

    /// <summary>
    /// Generates the backing field name for a command property
    /// </summary>
    public static string GetCommandFieldName(string commandPropertyName)
    {
        return $"_{char.ToLower(commandPropertyName[0])}{commandPropertyName.Substring(1)}";
    }
}
