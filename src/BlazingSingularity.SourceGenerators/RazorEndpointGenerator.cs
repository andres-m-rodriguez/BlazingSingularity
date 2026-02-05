#nullable enable
using System;
using System.IO;
using System.Linq;
using BlazingSingularity.SourceGenerators.RazorAnalysis;
using BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;
using Microsoft.CodeAnalysis;

namespace BlazingSingularity.SourceGenerators;

/// <summary>
/// Generator for [Endpoint] methods in Razor @code blocks.
/// Generates in-component endpoint proxies.
/// </summary>
[Generator]
public sealed class RazorEndpointGenerator : IIncrementalGenerator
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
                string hintName = $"{fileNameWithoutExt}.Endpoints.g.cs";

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
                    var sourceBuilder = new EndpointSourceBuilder(
                        razorContext.Namespace,
                        razorContext.ClassName
                    );

                    // Pass usings from the Razor file so generated code can use the same types
                    sourceBuilder.AddUsings(razorContext.UsingDirectives);

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
                                            "RAZOR005",
                                            "C# Syntax Error in @code block",
                                            "Syntax error in @code block: {0}",
                                            "RazorEndpointAnalyzer",
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

                        // Generate endpoint proxies from this code block
                        if (
                            analysisResult.EndpointMethods.Any()
                            && analysisResult.SemanticModel != null
                        )
                        {
                            foreach (var method in analysisResult.EndpointMethods)
                            {
                                var endpointInfo = EndpointInfoExtractor.ExtractEndpointInfo(
                                    method,
                                    analysisResult.SemanticModel
                                );

                                if (endpointInfo != null)
                                {
                                    sourceBuilder.AddEndpoint(endpointInfo);
                                }
                            }
                        }
                    }

                    // Step 4: Generate source if we have any endpoints
                    if (sourceBuilder.EndpointCount == 0)
                        return;

                    var source = sourceBuilder.BuildComponentProxies();
                    ctx.AddSource(hintName, source);
                }
                catch (Exception ex)
                {
                    // Report diagnostic if anything goes wrong
                    ctx.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "RAZOR006",
                                "Razor Endpoint Analysis Error",
                                "Failed to analyze Razor file for endpoints: {0}",
                                "RazorEndpointAnalyzer",
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
