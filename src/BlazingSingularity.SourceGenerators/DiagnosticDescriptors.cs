#nullable enable
using Microsoft.CodeAnalysis;

namespace BlazingSingularity.SourceGenerators;

/// <summary>
/// Diagnostic descriptors for BlazingSingularity source generators
/// </summary>
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor BLAZING001 = new(
        id: "BLAZING001",
        title: "Class must be partial",
        messageFormat: "Class '{0}' must be declared as partial to use [RelayCommand]",
        category: "BlazingSingularity",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor BLAZING002 = new(
        id: "BLAZING002",
        title: "Too many RelayCommand parameters",
        messageFormat: "Method '{0}' has {1} parameters. [RelayCommand] supports up to 1 parameter. Consider creating a DTO.",
        category: "BlazingSingularity.Commands",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
