#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazingSingularity.SourceGenerators.RazorAnalysis;

/// <summary>
/// Uses Roslyn to parse and analyze synthetic C# classes built from Razor @code blocks.
/// Provides full semantic analysis capabilities including type resolution and symbol information.
/// </summary>
public class RoslynCodeBlockAnalyzer
{
    /// <summary>
    /// Analyzes a synthetic C# class using Roslyn's semantic model.
    /// </summary>
    /// <param name="syntheticClass">The synthetic class built from a @code block</param>
    /// <param name="baseCompilation">The base compilation to use for references</param>
    /// <returns>Analysis results including semantic model and extracted methods</returns>
    public CodeBlockAnalysisResult Analyze(
        SyntheticClass syntheticClass,
        Compilation baseCompilation
    )
    {
        // Parse the synthetic C# code into a syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(
            syntheticClass.SourceCode,
            new CSharpParseOptions(LanguageVersion.Latest)
        );

        // Check for parse errors
        var parseDiagnostics = syntaxTree
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        if (parseDiagnostics.Any())
        {
            return new CodeBlockAnalysisResult(
                Success: false,
                SyntaxTree: syntaxTree,
                Compilation: null,
                SemanticModel: null,
                ClassDeclaration: null,
                CommandMethods: new List<MethodDeclarationSyntax>(),
                ParseDiagnostics: parseDiagnostics,
                SyntheticClass: syntheticClass
            );
        }

        // Create a compilation with the same references as the base compilation
        // This gives us access to all the same types and symbols
        var compilation = CSharpCompilation.Create(
            assemblyName: $"RazorCodeBlockAnalysis_{System.Guid.NewGuid():N}",
            syntaxTrees: new[] { syntaxTree },
            references: baseCompilation.References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        // Get the semantic model for type and symbol resolution
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        // Find the class declaration in the syntax tree
        var root = syntaxTree.GetRoot();
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration == null)
        {
            return new CodeBlockAnalysisResult(
                Success: false,
                SyntaxTree: syntaxTree,
                Compilation: compilation,
                SemanticModel: semanticModel,
                ClassDeclaration: null,
                CommandMethods: new List<MethodDeclarationSyntax>(),
                ParseDiagnostics: parseDiagnostics,
                SyntheticClass: syntheticClass
            );
        }

        // Extract all methods with the [RelayCommand] attribute
        var commandMethods = ExtractCommandMethods(classDeclaration, semanticModel);

        return new CodeBlockAnalysisResult(
            Success: true,
            SyntaxTree: syntaxTree,
            Compilation: compilation,
            SemanticModel: semanticModel,
            ClassDeclaration: classDeclaration,
            CommandMethods: commandMethods,
            ParseDiagnostics: parseDiagnostics,
            SyntheticClass: syntheticClass
        );
    }

    /// <summary>
    /// Extracts all methods that have the [RelayCommand] attribute.
    /// </summary>
    private List<MethodDeclarationSyntax> ExtractCommandMethods(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel
    )
    {
        var commandMethods = new List<MethodDeclarationSyntax>();

        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method)
            {
                if (HasRelayCommandAttribute(method, semanticModel))
                {
                    commandMethods.Add(method);
                }
            }
        }

        return commandMethods;
    }

    /// <summary>
    /// Checks if a method has the [RelayCommand] attribute using semantic analysis.
    /// </summary>
    private bool HasRelayCommandAttribute(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel
    )
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                // Get the symbol for the attribute to resolve its full type name
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);

                if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeType = attributeSymbol.ContainingType;
                    var fullName = attributeType.ToDisplayString();

                    // Check for the RelayCommandAttribute
                    if (fullName == "BlazingSingularity.Commands.RelayCommandAttribute")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the method symbol for a method declaration.
    /// </summary>
    public static IMethodSymbol? GetMethodSymbol(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel
    )
    {
        return semanticModel.GetDeclaredSymbol(method);
    }

    /// <summary>
    /// Gets the class symbol for a class declaration.
    /// </summary>
    public static INamedTypeSymbol? GetClassSymbol(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel
    )
    {
        return semanticModel.GetDeclaredSymbol(classDeclaration);
    }
}

/// <summary>
/// Results from analyzing a @code block with Roslyn.
/// </summary>
/// <param name="Success">Whether the analysis was successful</param>
/// <param name="SyntaxTree">The parsed syntax tree</param>
/// <param name="Compilation">The Roslyn compilation</param>
/// <param name="SemanticModel">The semantic model for type/symbol resolution</param>
/// <param name="ClassDeclaration">The class declaration syntax node</param>
/// <param name="CommandMethods">List of methods with [RelayCommand] attribute</param>
/// <param name="ParseDiagnostics">Any parse errors or warnings</param>
/// <param name="SyntheticClass">The synthetic class information for line mapping</param>
public record CodeBlockAnalysisResult(
    bool Success,
    SyntaxTree SyntaxTree,
    Compilation? Compilation,
    SemanticModel? SemanticModel,
    ClassDeclarationSyntax? ClassDeclaration,
    List<MethodDeclarationSyntax> CommandMethods,
    List<Diagnostic> ParseDiagnostics,
    SyntheticClass SyntheticClass
);
