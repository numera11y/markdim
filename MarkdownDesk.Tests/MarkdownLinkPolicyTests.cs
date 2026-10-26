using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class MarkdownLinkPolicyTests
{
    [Theory]
    [InlineData("https://example.org/docs")]
    [InlineData("http://example.org")]
    [InlineData("mailto:reader@example.org")]
    public void CommonExternalLinkSchemesAreAllowed(string value)
    {
        Assert.True(MarkdownLinkPolicy.TryGetSafeUri(value, out _));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/private.txt")]
    [InlineData("data:text/html,hello")]
    [InlineData("https://user:password@example.org/")]
    [InlineData("relative/page.md")]
    public void DangerousOrRelativeLinksAreRejected(string value)
    {
        Assert.False(MarkdownLinkPolicy.TryGetSafeUri(value, out _));
    }

    [Theory]
    [InlineData("HTTPS://example.org/docs")]
    [InlineData("MAILTO:reader@example.org")]
    public void AllowedSchemesAreComparedWithoutCaseSensitivity(string value)
    {
        Assert.True(MarkdownLinkPolicy.TryGetSafeUri(value, out _));
    }
}
