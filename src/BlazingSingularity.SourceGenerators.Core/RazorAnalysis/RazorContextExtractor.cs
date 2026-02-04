#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace BlazingSingularity.SourceGenerators.RazorAnalysis;

/// <summary>
/// Extracts context information from Razor files (@using, @namespace, @inherits, etc.)
/// that is needed to build a synthetic C# class.
/// </summary>
public class RazorContextExtractor
{
    private static readonly string[] CommonBlazorUsings = 
    [
        "Microsoft.AspNetCore.Components",
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Threading.Tasks",
    ];

    /// <summary>
    /// Extracts all necessary context from a Razor file to build a synthetic C# class.
    /// </summary>
    /// <param name="razorContent">The complete Razor file content</param>
    /// <param name="filePath">The file path of the Razor file</param>
    /// <param name="compilation">The compilation context for namespace resolution</param>
    /// <returns>A RazorContext containing all extracted information</returns>
    public RazorContext ExtractContext(
        string razorContent,
        string filePath,
        Compilation compilation
    )
    {
        var usingDirectives = ExtractUsingDirectives(razorContent);
        var namespaceName = ExtractNamespace(razorContent, filePath, compilation);
        var className = Path.GetFileNameWithoutExtension(filePath);
        var inheritsFrom = ExtractInheritsDirective(razorContent);

        return new RazorContext(
            UsingDirectives: usingDirectives,
            Namespace: namespaceName,
            ClassName: className,
            InheritsFrom: inheritsFrom
        );
    }

    /// <summary>
    /// Extracts all @using directives from the Razor file.
    /// </summary>
    private List<string> ExtractUsingDirectives(string razorContent)
    {
        var usings = new HashSet<string>();

        // Add common Blazor usings first
        foreach (var commonUsing in CommonBlazorUsings)
        {
            usings.Add(commonUsing);
        }

        // Extract @using directives from the file
        // Pattern: @using <namespace>
        var usingPattern = @"@using\s+([^\r\n;]+)";
        var matches = Regex.Matches(razorContent, usingPattern);

        foreach (Match match in matches)
        {
            var usingNamespace = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(usingNamespace))
            {
                usings.Add(usingNamespace);
            }
        }

        return usings.OrderBy(u => u).ToList();
    }

    /// <summary>
    /// Extracts the namespace from @namespace directive or derives it from file path.
    /// </summary>
    private string ExtractNamespace(string razorContent, string filePath, Compilation compilation)
    {
        // First, check for @namespace directive in the file
        var namespaceMatch = Regex.Match(razorContent, @"@namespace\s+([^\s\r\n]+)");
        if (namespaceMatch.Success)
        {
            return namespaceMatch.Groups[1].Value;
        }

        // If no @namespace directive, derive from file path and root namespace
        return DeriveNamespaceFromPath(filePath, compilation);
    }

    /// <summary>
    /// Derives the namespace from the file path by analyzing the path structure.
    /// Looks for project name in path and builds namespace from subdirectories.
    /// This is project-agnostic and doesn't rely on hardcoded folder names.
    /// </summary>
    private string DeriveNamespaceFromPath(string filePath, Compilation compilation)
    {
        // Get the root namespace from the compilation assembly name
        string rootNamespace = compilation.AssemblyName ?? "App";

        // Normalize path separators
        string normalizedPath = filePath.Replace('\\', '/');

        // Split the path into segments
        var pathSegments = normalizedPath.Split(
            new[] { '/' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (pathSegments.Length == 0)
            return rootNamespace;

        // Try to find the project directory by looking for the assembly name in the path
        // The assembly name usually matches a folder in the path
        int projectIndex = FindProjectIndexInPath(pathSegments, compilation.AssemblyName);

        if (projectIndex == -1)
        {
            // Can't determine project location in path, just use root namespace
            return rootNamespace;
        }

        // Build namespace from folders after the project directory (excluding the filename)
        var namespaceParts = new List<string> { rootNamespace };

        // Add all subdirectories after the project folder
        for (int i = projectIndex + 1; i < pathSegments.Length - 1; i++) // -1 to exclude filename
        {
            var cleanSegment = CleanNamespaceSegment(pathSegments[i]);
            if (!string.IsNullOrEmpty(cleanSegment))
            {
                namespaceParts.Add(cleanSegment);
            }
        }

        return string.Join(".", namespaceParts);
    }

    /// <summary>
    /// Finds the index of the project directory in the path segments.
    /// Looks for a segment that matches or contains the assembly name.
    /// </summary>
    private int FindProjectIndexInPath(string[] pathSegments, string? assemblyName)
    {
        if (string.IsNullOrEmpty(assemblyName))
            return -1;

        // Look for exact match first
        for (int i = 0; i < pathSegments.Length; i++)
        {
            if (pathSegments[i].Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        // Look for partial match (assembly name might be "MyApp" and folder "MyApp.Client")
        for (int i = 0; i < pathSegments.Length; i++)
        {
            if (pathSegments[i].StartsWith(assemblyName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        // If we can't find the project folder, assume it's the last meaningful directory
        // before the file (heuristic: look for bin/obj and return the parent)
        for (int i = pathSegments.Length - 1; i >= 0; i--)
        {
            string segment = pathSegments[i];
            if (
                segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
            )
            {
                // Project root is likely the parent of bin/obj
                return i > 0 ? i - 1 : 0;
            }
        }

        // Last resort: assume project is 2 levels up from the file
        // (e.g., ProjectName/Folder/File.razor)
        return Math.Max(0, pathSegments.Length - 3);
    }

    /// <summary>
    /// Cleans a path segment to be a valid C# namespace identifier.
    /// Removes invalid characters and ensures it's a valid identifier.
    /// </summary>
    private string CleanNamespaceSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return string.Empty;

        // Remove any characters that aren't valid in a C# identifier
        // Valid: letters, digits, underscore
        // First character cannot be a digit
        var cleaned = new System.Text.StringBuilder();

        for (int i = 0; i < segment.Length; i++)
        {
            char c = segment[i];

            if (char.IsLetterOrDigit(c) || c == '_')
            {
                // First character can't be a digit
                if (i == 0 && char.IsDigit(c))
                {
                    cleaned.Append('_');
                }
                cleaned.Append(c);
            }
            else if (c == ' ' || c == '-' || c == '.')
            {
                // Convert common separators to underscore
                cleaned.Append('_');
            }
            // Skip all other invalid characters
        }

        var result = cleaned.ToString();

        // Ensure the result is not empty and not a C# keyword
        if (string.IsNullOrEmpty(result))
            return string.Empty;

        // If it's a C# keyword, prefix with @
        if (IsCSharpKeyword(result))
            return "@" + result;

        return result;
    }

    /// <summary>
    /// Checks if a string is a C# keyword.
    /// </summary>
    private bool IsCSharpKeyword(string identifier)
    {
        // Common C# keywords that might appear in paths
        string[] keywords =
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            "while",
        };

        return Array.IndexOf(keywords, identifier.ToLowerInvariant()) >= 0;
    }

    /// <summary>
    /// Extracts the @inherits directive if present.
    /// Returns "ComponentBase" if no @inherits is specified (Blazor default).
    /// </summary>
    private string ExtractInheritsDirective(string razorContent)
    {
        // Pattern: @inherits <BaseClass>
        var inheritsMatch = Regex.Match(razorContent, @"@inherits\s+([^\s\r\n<]+(?:<[^>]+>)?)");
        if (inheritsMatch.Success)
        {
            return inheritsMatch.Groups[1].Value.Trim();
        }

        // Default to ComponentBase for Blazor components
        return "ComponentBase";
    }
}

/// <summary>
/// Context information extracted from a Razor file.
/// </summary>
/// <param name="UsingDirectives">List of namespaces to include (from @using directives + common usings)</param>
/// <param name="Namespace">The namespace for the generated class (from @namespace or derived from path)</param>
/// <param name="ClassName">The class name (derived from filename)</param>
/// <param name="InheritsFrom">The base class (from @inherits or default to ComponentBase)</param>
public record RazorContext(
    List<string> UsingDirectives,
    string Namespace,
    string ClassName,
    string InheritsFrom
);
