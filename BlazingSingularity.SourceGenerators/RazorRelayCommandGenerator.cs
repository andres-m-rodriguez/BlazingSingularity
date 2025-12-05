#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using BlazingSingularity.SourceGenerators.RazorAnalysis;
using BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;
using Microsoft.CodeAnalysis;

namespace BlazingSingularity.SourceGenerators;

[Generator]
public sealed class RazorRelayCommandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get all .razor files
        var razorFiles = context.AdditionalTextsProvider.Where(file =>
            file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
        );

        // Combine with compilation (not used yet, but required for future semantic work)
        var razorFilesWithCompilation = razorFiles.Combine(context.CompilationProvider);

        // Output for each file
        context.RegisterSourceOutput(
            razorFilesWithCompilation,
            (ctx, pair) =>
            {
                var (file, compilation) = pair;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Path);
                string hintName = $"{fileNameWithoutExt}.Commands.g.cs";

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
                    var sourceBuilder = new CommandSourceBuilder(
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
                                // Map the diagnostic location back to the original Razor file
                                var razorLine = SyntheticClassBuilder.MapToRazorLine(
                                    syntheticClass,
                                    diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                                    codeBlock.ContentStartLine
                                );

                                ctx.ReportDiagnostic(
                                    Diagnostic.Create(
                                        new DiagnosticDescriptor(
                                            "RAZOR002",
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

                        // Generate commands from this code block
                        // IMPORTANT: Each method must use its own SemanticModel!
                        if (
                            analysisResult.CommandMethods.Any()
                            && analysisResult.SemanticModel != null
                        )
                        {
                            foreach (var method in analysisResult.CommandMethods)
                            {
                                sourceBuilder.TryAddCommandFromMethod(
                                    method,
                                    analysisResult.SemanticModel
                                );
                            }
                        }
                    }

                    // Step 4: Generate source if we have any commands
                    if (sourceBuilder.CommandCount == 0)
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
                                "RAZOR001",
                                "Razor Analysis Error",
                                "Failed to analyze Razor file: {0}",
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
