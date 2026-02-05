#nullable enable
using System;
using System.IO;
using System.Linq;
using BlazingSingularity.SourceGenerators.RazorAnalysis;
using BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BlazingSingularity.SourceGenerators;

[Generator]
public sealed class RazorSignalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get all .razor files
        var razorFiles = context.AdditionalTextsProvider.Where(file =>
            file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
        );

        // Combine with compilation
        var razorFilesWithCompilation = razorFiles.Combine(context.CompilationProvider);

        // Output for each file
        context.RegisterSourceOutput(
            razorFilesWithCompilation,
            (ctx, pair) =>
            {
                var (file, compilation) = pair;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Path);
                string hintName = $"{fileNameWithoutExt}.Signals.g.cs";

                var content = file.GetText(ctx.CancellationToken);
                if (content is null)
                    return;

                string razorContent = content.ToString();

                try
                {
                    // Step 1: Extract Razor context (@using, @namespace, etc.)
                    var contextExtractor = new RazorContextExtractor();
                    var razorContext = contextExtractor.ExtractContext(
                        razorContent,
                        file.Path,
                        compilation
                    );

                    // Step 2: Extract @code blocks
                    var codeBlockExtractor = new CodeBlockExtractor();
                    var codeBlocks = codeBlockExtractor.ExtractCodeBlocks(razorContent);

                    if (codeBlocks.Count == 0)
                        return; // No @code blocks found

                    // Step 3: Process each @code block with Roslyn
                    var sourceBuilder = new SignalSourceBuilder(
                        razorContext.Namespace,
                        razorContext.ClassName
                    );

                    foreach (var codeBlock in codeBlocks)
                    {
                        // Build synthetic C# class
                        var classBuilder = new SyntheticClassBuilder();
                        var syntheticClass = classBuilder.BuildClass(
                            razorContext,
                            codeBlock.Content
                        );

                        // Analyze with Roslyn
                        var analyzer = new RoslynCodeBlockAnalyzer();
                        var analysisResult = analyzer.Analyze(syntheticClass, compilation);

                        // Handle parse errors
                        if (!analysisResult.Success)
                        {
                            foreach (var diagnostic in analysisResult.ParseDiagnostics)
                            {
                                ctx.ReportDiagnostic(
                                    Diagnostic.Create(
                                        new DiagnosticDescriptor(
                                            "RAZOR003",
                                            "C# Syntax Error in @code block",
                                            "Syntax error in @code block: {0}",
                                            "RazorRoslynAnalyzer",
                                            DiagnosticSeverity.Error,
                                            isEnabledByDefault: true
                                        ),
                                        Location.None,
                                        diagnostic.GetMessage()
                                    )
                                );
                            }
                            continue;
                        }

                        // Generate signals from this code block
                        if (
                            analysisResult.SignalFields.Any()
                            && analysisResult.SemanticModel != null
                        )
                        {
                            foreach (var field in analysisResult.SignalFields)
                            {
                                foreach (var variable in field.Declaration.Variables)
                                {
                                    var fieldSymbol = analysisResult.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                                    if (fieldSymbol == null)
                                        continue;

                                    var fieldName = fieldSymbol.Name;
                                    var fieldType = fieldSymbol.Type.ToDisplayString();
                                    var propertyName = SignalHelpers.GetPropertyName(fieldName);
                                    var signalPropertyName = SignalHelpers.GetSignalPropertyName(propertyName);
                                    var signalFieldName = SignalHelpers.GetSignalFieldName(signalPropertyName);

                                    sourceBuilder.AddSignal(fieldName, fieldType, propertyName, signalFieldName, signalPropertyName);
                                }
                            }
                        }
                    }

                    // Step 4: Generate source if we have any signals
                    if (sourceBuilder.SignalCount == 0)
                        return;

                    var source = sourceBuilder.Build();
                    ctx.AddSource(hintName, source);
                }
                catch (Exception ex)
                {
                    // Report diagnostic if anything goes wrong
                    ctx.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "RAZOR004",
                                "Razor Signal Analysis Error",
                                "Failed to analyze Razor file for signals: {0}",
                                "RazorRoslynAnalyzer",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true
                            ),
                            Location.None,
                            ex.Message
                        )
                    );
                }
            }
        );
    }
}
