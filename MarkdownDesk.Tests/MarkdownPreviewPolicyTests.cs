using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class MarkdownPreviewPolicyTests
{
    [Fact]
    public void PreviewRemainsEnabledAtTheConfiguredLimit()
    {
        Assert.True(MarkdownPreviewPolicy.ShouldRender(new string('a', MarkdownPreviewPolicy.MaximumCharacters)));
    }

    [Fact]
    public void PreviewIsSuppressedForOversizedDocuments()
    {
        Assert.False(MarkdownPreviewPolicy.ShouldRender(new string('a', MarkdownPreviewPolicy.MaximumCharacters + 1)));
    }

    [Fact]
    public void PreviewIsSuppressedForPathologicalLineCounts()
    {
        var markdown = string.Join('\n', Enumerable.Repeat("x", MarkdownPreviewPolicy.MaximumLines + 1));

        Assert.False(MarkdownPreviewPolicy.ShouldRender(markdown));
    }
}
