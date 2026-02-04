#nullable enable
using System.Linq;
using System.Text;

namespace BlazingSingularity.SourceGenerators.RazorAnalysis;

/// <summary>
/// Builds a synthetic C# class by wrapping extracted @code block content
/// in a proper class structure that Roslyn can parse and analyze.
/// </summary>
public class SyntheticClassBuilder
{
    /// <summary>
    /// Builds a complete C# class file from the Razor context and code block content.
    /// </summary>
    /// <param name="context">The Razor context containing namespace, usings, etc.</param>
    /// <param name="codeBlockContent">The raw C# code from the @code block</param>
    /// <returns>A complete, valid C# class file as a string, along with line mapping information</returns>
    public SyntheticClass BuildClass(RazorContext context, string codeBlockContent)
    {
        var sb = new StringBuilder();
        int headerLineCount = 0;

        // Add #nullable enable
        sb.AppendLine("#nullable enable");
        headerLineCount++;

        // Add using directives
        foreach (var usingDirective in context.UsingDirectives)
        {
            sb.AppendLine($"using {usingDirective};");
            headerLineCount++;
        }

        // Add blank line after usings
        sb.AppendLine();
        headerLineCount++;

        // Add namespace declaration
        sb.AppendLine($"namespace {context.Namespace}");
        sb.AppendLine("{");
        headerLineCount += 2;

        // Add class declaration
        // Make it partial so it can be combined with the actual Razor component
        sb.AppendLine($"    public partial class {context.ClassName} : {context.InheritsFrom}");
        sb.AppendLine("    {");
        headerLineCount += 2;

        // Now we're about to insert the code block content
        // Track where the actual code starts for line mapping
        int codeStartLine = headerLineCount + 1; // +1 because we're 1-based

        // Insert the code block content with proper indentation
        // The code block content should be indented by 8 spaces (2 levels: namespace + class)
        string indentedContent = IndentContent(codeBlockContent, 8);
        sb.Append(indentedContent);

        // Count lines in the code block content
        int codeLineCount = CountLines(codeBlockContent);

        // Close class
        sb.AppendLine();
        sb.AppendLine("    }");

        // Close namespace
        sb.AppendLine("}");

        return new SyntheticClass(
            SourceCode: sb.ToString(),
            HeaderLineCount: headerLineCount,
            CodeStartLine: codeStartLine,
            TotalLines: headerLineCount + codeLineCount + 3 // +3 for closing braces and blank line
        );
    }

    /// <summary>
    /// Indents each line of the content by the specified number of spaces.
    /// Preserves existing indentation and empty lines.
    /// </summary>
    private string IndentContent(string content, int spaces)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var indent = new string(' ', spaces);
        var lines = content.Split('\n');
        var sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Remove trailing \r if present (Windows line endings)
            if (line.EndsWith("\r"))
                line = line.Substring(0, line.Length - 1);

            // Only add indent to non-empty lines
            if (!string.IsNullOrWhiteSpace(line))
            {
                sb.Append(indent);
                sb.Append(line);
            }

            // Add newline except for the last line if it's empty
            if (i < lines.Length - 1)
            {
                sb.AppendLine();
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Counts the number of lines in the content.
    /// </summary>
    private int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        return content.Split('\n').Length;
    }

    /// <summary>
    /// Converts a line number in the synthetic class back to the original Razor file line number.
    /// </summary>
    /// <param name="syntheticClass">The synthetic class information</param>
    /// <param name="syntheticLine">Line number in the synthetic class (1-based)</param>
    /// <param name="razorCodeBlockStartLine">Line number where the @code block content starts in the Razor file</param>
    /// <returns>The corresponding line number in the original Razor file</returns>
    public static int MapToRazorLine(
        SyntheticClass syntheticClass,
        int syntheticLine,
        int razorCodeBlockStartLine
    )
    {
        // If the line is in the header (before the actual code content), we can't map it accurately
        if (syntheticLine < syntheticClass.CodeStartLine)
        {
            // Return the start of the @code block as a fallback
            return razorCodeBlockStartLine;
        }

        // Calculate the offset within the code content
        int offsetInCode = syntheticLine - syntheticClass.CodeStartLine;

        // Map to the Razor file
        return razorCodeBlockStartLine + offsetInCode;
    }
}

/// <summary>
/// Represents a synthetic C# class built from a Razor @code block.
/// </summary>
/// <param name="SourceCode">The complete C# source code</param>
/// <param name="HeaderLineCount">Number of lines in the header (usings, namespace, class declaration)</param>
/// <param name="CodeStartLine">Line number where the actual @code block content starts (1-based)</param>
/// <param name="TotalLines">Total number of lines in the synthetic class</param>
public record SyntheticClass(
    string SourceCode,
    int HeaderLineCount,
    int CodeStartLine,
    int TotalLines
);
