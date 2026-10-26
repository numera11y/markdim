using System.Reflection;
using System.Xml;
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace MarkdownDesk;

public static class MarkdownCodeHighlighting
{
    public static FontFeatureCollection CodeFontFeatures => new()
    {
        FontFeature.Parse("liga=0"),
        FontFeature.Parse("clig=0"),
        FontFeature.Parse("dlig=0"),
        FontFeature.Parse("calt=0")
    };

    public static IHighlightingDefinition Load(bool dark)
    {
        var resourceName = dark
            ? "MarkdownDesk.Highlighting.Code.Dark.xshd"
            : "MarkdownDesk.Highlighting.Code.Light.xshd";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing code syntax definition: {resourceName}");
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
