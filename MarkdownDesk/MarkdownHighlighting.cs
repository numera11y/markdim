using System.Reflection;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace MarkdownDesk;

public static class MarkdownHighlighting
{
    public static IHighlightingDefinition LoadSourceLinks(bool dark)
    {
        var resourceName = dark
            ? "MarkdownDesk.Highlighting.Markdown.Source.Dark.xshd"
            : "MarkdownDesk.Highlighting.Markdown.Source.Light.xshd";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing Markdown source link definition: {resourceName}");
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    public static IHighlightingDefinition Load(bool dark)
    {
        var resourceName = dark
            ? "MarkdownDesk.Highlighting.Markdown.Dark.xshd"
            : "MarkdownDesk.Highlighting.Markdown.Light.xshd";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing Markdown syntax definition: {resourceName}");
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
