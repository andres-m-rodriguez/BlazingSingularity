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
/// Generator for client-side HTTP clients (interface and implementation) and AddSingularityClients.
/// </summary>
[Generator]
public class ClientEndpointGenerator : IIncrementalGenerator
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

        var assemblyName = compilation.AssemblyName ?? "Assembly";
        var clientRegistrationBuilder = new ClientEndpointSourceBuilder(assemblyName);

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

            // Create endpoint source builder for this class
            var endpointBuilder = new EndpointSourceBuilder(namespaceName, className);

            foreach (var method in group)
            {
                var endpointInfo = EndpointInfoExtractor.ExtractEndpointInfo(method, semanticModel);
                if (endpointInfo != null)
                {
                    endpointBuilder.AddEndpoint(endpointInfo);
                }
            }

            if (endpointBuilder.EndpointCount > 0)
            {
                // Generate HTTP client interface and implementation
                var httpClientSource = endpointBuilder.BuildHttpClient();
                context.AddSource($"{className}.HttpClient.g.cs", SourceText.From(httpClientSource, Encoding.UTF8));

                // Track for registration
                clientRegistrationBuilder.AddClientClass(namespaceName, className);
            }
        }

        if (clientRegistrationBuilder.ClientCount > 0)
        {
            // Generate the AddSingularityClients extension
            var registrationSource = clientRegistrationBuilder.Build();
            context.AddSource("SingularityClients.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
        }
    }
}
