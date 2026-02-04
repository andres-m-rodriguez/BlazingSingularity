using BlazingSingularity.SourceGenerators.RazorAnalysis;
using Xunit;

namespace BlazingSingularity.SourceGenerators.Core.Tests;

public class CodeBlockExtractorTests
{
    private readonly CodeBlockExtractor _extractor = new();

    [Fact]
    public void ExtractCodeBlocks_SingleBlock_ReturnsOne()
    {
        var razor = """
            <h1>Hello</h1>

            @code {
                private int _count;
            }
            """;

        var blocks = _extractor.ExtractCodeBlocks(razor);

        var block = Assert.Single(blocks);
        Assert.Contains("private int _count;", block.Content);
    }

    [Fact]
    public void ExtractCodeBlocks_NoCodeBlock_ReturnsEmpty()
    {
        var razor = """
            <h1>Hello</h1>
            <p>No code here</p>
            """;

        var blocks = _extractor.ExtractCodeBlocks(razor);

        Assert.Empty(blocks);
    }

    [Fact]
    public void ExtractCodeBlocks_NestedBraces_HandledCorrectly()
    {
        var razor = """
            @code {
                private void DoWork()
                {
                    if (true)
                    {
                        var x = 1;
                    }
                }
            }
            """;

        var blocks = _extractor.ExtractCodeBlocks(razor);

        var block = Assert.Single(blocks);
        Assert.Contains("DoWork()", block.Content);
        Assert.Contains("var x = 1;", block.Content);
    }
}
