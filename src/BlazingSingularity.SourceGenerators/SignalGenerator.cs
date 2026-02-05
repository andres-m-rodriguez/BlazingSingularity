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

[Generator]
public class SignalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fieldDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static f => f is not null);

        var compilationAndFields = context.CompilationProvider.Combine(
            fieldDeclarations.Collect()
        );

        context.RegisterSourceOutput(
            compilationAndFields,
            static (spc, source) => Execute(source.Left, source.Right!, spc)
        );
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is FieldDeclarationSyntax f && f.AttributeLists.Count > 0;
    }

    private static FieldDeclarationSyntax? GetSemanticTargetForGeneration(
        GeneratorSyntaxContext context
    )
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        foreach (var attributeList in fieldDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == "BlazingSingularity.Signals.SignalAttribute")
                    {
                        return fieldDeclaration;
                    }
                }
            }
        }

        return null;
    }

    private static void Execute(
        Compilation compilation,
        IEnumerable<FieldDeclarationSyntax> fields,
        SourceProductionContext context
    )
    {
        if (!fields.Any())
            return;

        var fieldsByClass = fields.GroupBy(f => f.FirstAncestorOrSelf<ClassDeclarationSyntax>());

        foreach (var group in fieldsByClass)
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
                    DiagnosticDescriptors.BLAZING003,
                    classDeclaration.GetLocation(),
                    className
                );
                context.ReportDiagnostic(diagnostic);
                continue;
            }

            var source = GenerateSignalsForClass(
                namespaceName,
                className,
                group,
                semanticModel
            );
            context.AddSource($"{className}.Signals.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string GenerateSignalsForClass(
        string namespaceName,
        string className,
        IEnumerable<FieldDeclarationSyntax> fields,
        SemanticModel semanticModel
    )
    {
        var builder = new SignalSourceBuilder(namespaceName, className);

        foreach (var field in fields)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                if (fieldSymbol == null)
                    continue;

                var fieldName = fieldSymbol.Name;
                var fieldType = fieldSymbol.Type.ToDisplayString();
                var propertyName = SignalHelpers.GetPropertyName(fieldName);
                var signalPropertyName = SignalHelpers.GetSignalPropertyName(propertyName);
                var signalFieldName = SignalHelpers.GetSignalFieldName(signalPropertyName);

                builder.AddSignal(fieldName, fieldType, propertyName, signalFieldName, signalPropertyName);
            }
        }

        return builder.Build();
    }
}
