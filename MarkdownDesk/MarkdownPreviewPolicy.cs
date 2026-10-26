namespace MarkdownDesk;

/// <summary>Bounds Markdown visual-tree creation for unusually large source files.</summary>
public static class MarkdownPreviewPolicy
{
    public const int MaximumCharacters = 250_000;
    public const int MaximumLines = 5_000;
    public const int MaximumRenderedNodes = 6_000;
    public const int MaximumCodeBlocks = 64;

    public static bool ShouldRender(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (markdown.Length > MaximumCharacters) return false;
        var lines = 1;
        foreach (var character in markdown)
        {
            if (character == '\n' && ++lines > MaximumLines) return false;
        }
        return true;
    }
}
