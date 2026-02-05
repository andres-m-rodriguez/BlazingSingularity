#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BlazingSingularity.SourceGenerators;

/// <summary>
/// Generator for [Endpoint] methods in code-behind files.
/// Generates in-component endpoint proxies.
/// </summary>
[Generator]
public class EndpointGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null);

        var compilationAndMethods = context.CompilationProvider.Combine(
            methodDeclarations.Collect()
        );

        context.RegisterSourceOutput(
            compilationAndMethods,
            static (spc, source) => Execute(source.Left, source.Right!, spc)
        );
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;
    }

    private static MethodDeclarationSyntax? GetSemanticTargetForGeneration(
        GeneratorSyntaxContext context
    )
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == "BlazingSingularity.Endpoints.EndpointAttribute")
                    {
                        return methodDeclaration;
                    }
                }
            }
        }

        return null;
    }

    private static void Execute(
        Compilation compilation,
        IEnumerable<MethodDeclarationSyntax> methods,
        SourceProductionContext context
    )
    {
        if (!methods.Any())
            return;

        var methodsByClass = methods.GroupBy(m => m.FirstAncestorOrSelf<ClassDeclarationSyntax>());

        foreach (var group in methodsByClass)
        {
            var classDeclaration = group.Key;
            if (classDeclaration == null)
                continue;

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol == null)
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var isPartial = classDeclaration.Modifiers.Any(m =>
                m.IsKind(SyntaxKind.PartialKeyword)
            );

            if (!isPartial)
            {
                var diagnostic = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "BLAZING004",
                        "Class must be partial",
                        "Class '{0}' must be declared as partial to use [Endpoint]",
                        "BlazingSingularity",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true
                    ),
                    classDeclaration.GetLocation(),
                    className
                );
                context.ReportDiagnostic(diagnostic);
                continue;
            }

            var source = GenerateEndpointsForClass(
                namespaceName,
                className,
                group,
                semanticModel
            );

            if (!string.IsNullOrEmpty(source))
            {
                context.AddSource($"{className}.Endpoints.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static string GenerateEndpointsForClass(
        string namespaceName,
        string className,
        IEnumerable<MethodDeclarationSyntax> methods,
        SemanticModel semanticModel
    )
    {
        var builder = new EndpointSourceBuilder(namespaceName, className);

        foreach (var method in methods)
        {
            var endpointInfo = EndpointInfoExtractor.ExtractEndpointInfo(method, semanticModel);
            if (endpointInfo != null)
            {
                builder.AddEndpoint(endpointInfo);
            }
        }

        if (builder.EndpointCount == 0)
            return string.Empty;

        return builder.BuildComponentProxies();
    }
}
