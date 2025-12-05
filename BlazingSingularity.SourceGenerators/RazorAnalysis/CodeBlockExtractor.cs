#nullable enable
using System;
using System.Collections.Generic;

namespace BlazingSingularity.SourceGenerators.RazorAnalysis;

/// <summary>
/// Extracts @code blocks from Razor files without parsing the C# syntax.
/// Simply finds the @code{} boundaries and extracts the raw C# content.
/// </summary>
public class CodeBlockExtractor
{
    /// <summary>
    /// Extracts all @code blocks from the given Razor file content.
    /// </summary>
    /// <param name="razorContent">The complete Razor file content</param>
    /// <returns>List of extracted code blocks with their content and position information</returns>
    public List<CodeBlockInfo> ExtractCodeBlocks(string razorContent)
    {
        var blocks = new List<CodeBlockInfo>();
        int position = 0;

        while (position < razorContent.Length)
        {
            // Find next @code directive
            int codeDirectiveIndex = FindCodeDirective(razorContent, position);
            if (codeDirectiveIndex == -1)
                break;

            // Calculate line and column for error reporting
            var (line, column) = CalculateLineAndColumn(razorContent, codeDirectiveIndex);

            // Find the opening brace
            int openBraceIndex = FindOpeningBrace(razorContent, codeDirectiveIndex);
            if (openBraceIndex == -1)
            {
                // No opening brace found - skip this @code and continue
                position = codeDirectiveIndex + 5; // Skip "@code"
                continue;
            }

            // Find the matching closing brace
            int closeBraceIndex = FindMatchingCloseBrace(razorContent, openBraceIndex);
            if (closeBraceIndex == -1)
            {
                // No matching closing brace - malformed @code block
                position = openBraceIndex + 1;
                continue;
            }

            // Extract the content between the braces (excluding the braces themselves)
            int contentStart = openBraceIndex + 1;
            int contentLength = closeBraceIndex - contentStart;
            string content = razorContent.Substring(contentStart, contentLength);

            // Calculate the starting line of the actual code content (after the opening brace)
            var (contentStartLine, contentStartColumn) = CalculateLineAndColumn(
                razorContent,
                contentStart
            );

            blocks.Add(
                new CodeBlockInfo(
                    Content: content,
                    StartLine: line,
                    StartColumn: column,
                    ContentStartLine: contentStartLine,
                    ContentStartColumn: contentStartColumn,
                    StartPosition: codeDirectiveIndex,
                    EndPosition: closeBraceIndex + 1
                )
            );

            // Move past this @code block
            position = closeBraceIndex + 1;
        }

        return blocks;
    }

    /// <summary>
    /// Finds the next @code directive starting from the given position.
    /// </summary>
    private int FindCodeDirective(string content, int startPosition)
    {
        int position = startPosition;

        while (position < content.Length)
        {
            int atIndex = content.IndexOf('@', position);
            if (atIndex == -1)
                return -1;

            // Check if this is "@code"
            if (atIndex + 5 <= content.Length)
            {
                string directive = content.Substring(atIndex, 5);
                if (directive == "@code")
                {
                    // Verify it's followed by whitespace or '{'
                    if (
                        atIndex + 5 >= content.Length
                        || char.IsWhiteSpace(content[atIndex + 5])
                        || content[atIndex + 5] == '{'
                    )
                    {
                        return atIndex;
                    }
                }
            }

            position = atIndex + 1;
        }

        return -1;
    }

    /// <summary>
    /// Finds the opening brace after the @code directive.
    /// </summary>
    private int FindOpeningBrace(string content, int codeDirectiveIndex)
    {
        int position = codeDirectiveIndex + 5; // Skip "@code"

        // Skip whitespace
        while (position < content.Length && char.IsWhiteSpace(content[position]))
        {
            position++;
        }

        // Should now be at '{'
        if (position < content.Length && content[position] == '{')
        {
            return position;
        }

        return -1;
    }

    /// <summary>
    /// Finds the closing brace that matches the opening brace at the given position.
    /// Handles nested braces correctly.
    /// </summary>
    private int FindMatchingCloseBrace(string content, int openBraceIndex)
    {
        int depth = 1;
        int position = openBraceIndex + 1;
        bool inString = false;
        bool inChar = false;
        bool inSingleLineComment = false;
        bool inMultiLineComment = false;
        bool escapeNext = false;

        while (position < content.Length && depth > 0)
        {
            char current = content[position];
            char next = position + 1 < content.Length ? content[position + 1] : '\0';

            // Handle escape sequences in strings/chars
            if (escapeNext)
            {
                escapeNext = false;
                position++;
                continue;
            }

            if (current == '\\' && (inString || inChar))
            {
                escapeNext = true;
                position++;
                continue;
            }

            // Handle comments
            if (!inString && !inChar)
            {
                // Check for single-line comment start
                if (current == '/' && next == '/' && !inMultiLineComment)
                {
                    inSingleLineComment = true;
                    position += 2;
                    continue;
                }

                // Check for multi-line comment start
                if (current == '/' && next == '*' && !inSingleLineComment)
                {
                    inMultiLineComment = true;
                    position += 2;
                    continue;
                }

                // Check for multi-line comment end
                if (current == '*' && next == '/' && inMultiLineComment)
                {
                    inMultiLineComment = false;
                    position += 2;
                    continue;
                }

                // Check for single-line comment end
                if (current == '\n' && inSingleLineComment)
                {
                    inSingleLineComment = false;
                    position++;
                    continue;
                }
            }

            // Skip everything inside comments
            if (inSingleLineComment || inMultiLineComment)
            {
                position++;
                continue;
            }

            // Handle string literals
            if (current == '"' && !inChar)
            {
                inString = !inString;
                position++;
                continue;
            }

            // Handle char literals
            if (current == '\'' && !inString)
            {
                inChar = !inChar;
                position++;
                continue;
            }

            // Handle braces (only when not in string, char, or comment)
            if (!inString && !inChar)
            {
                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return position;
                    }
                }
            }

            position++;
        }

        return -1; // No matching closing brace found
    }

    /// <summary>
    /// Calculates the line number and column for a given position in the content.
    /// Line and column numbers are 1-based.
    /// </summary>
    private (int line, int column) CalculateLineAndColumn(string content, int position)
    {
        int line = 1;
        int column = 1;

        for (int i = 0; i < position && i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}

/// <summary>
/// Information about an extracted @code block.
/// </summary>
/// <param name="Content">The raw C# code content (without @code{} wrapper)</param>
/// <param name="StartLine">Line number where @code directive starts (1-based)</param>
/// <param name="StartColumn">Column number where @code directive starts (1-based)</param>
/// <param name="ContentStartLine">Line number where the actual code content starts (after opening brace)</param>
/// <param name="ContentStartColumn">Column number where the actual code content starts</param>
/// <param name="StartPosition">Character position where @code directive starts</param>
/// <param name="EndPosition">Character position where the closing brace ends</param>
public record CodeBlockInfo(
    string Content,
    int StartLine,
    int StartColumn,
    int ContentStartLine,
    int ContentStartColumn,
    int StartPosition,
    int EndPosition
);
