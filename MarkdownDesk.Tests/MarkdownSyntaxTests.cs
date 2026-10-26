using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Document;
using Avalonia.Media;
using System.Xml.Linq;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class MarkdownSyntaxTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryApplicationTextTokenMeetsAaAcrossItsThemeSurfaces(bool dark)
    {
        var failingPairs = AppTheme.GetTextContrastSamples(dark)
            .Select(sample => (sample.Name, Ratio: ContrastRatio(Color.Parse(sample.Foreground), Color.Parse(sample.Background))))
            .Where(sample => sample.Ratio < 4.5)
            .ToArray();

        Assert.True(failingPairs.Length == 0,
            $"{(dark ? "Dark" : "Light")} theme has text contrast below 4.5:1: {string.Join(", ", failingPairs.Select(pair => $"{pair.Name}={pair.Ratio:F2}:1"))}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FluentPalettesUseTheContrastAuditedThemeColors(bool dark)
    {
        var palette = AppTheme.CreateFluentPalette(dark);
        Assert.Equal(Color.Parse(dark ? "#E6EAF0" : "#171A21"), palette.BaseHigh);
        Assert.Equal(Color.Parse(dark ? "#D0D6E0" : "#344054"), palette.BaseMedium);
        Assert.Equal(Color.Parse(dark ? "#B1BBC9" : "#475467"), palette.BaseMediumLow);
        Assert.Equal(Color.Parse(dark ? "#A3B8FF" : "#275DAD"), palette.Accent);
        Assert.Equal(Color.Parse(dark ? "#1C222C" : "#FFFFFF"), palette.AltHigh);
        Assert.Equal(Color.Parse(dark ? "#191F29" : "#EEF1F5"), palette.AltMediumHigh);
    }

    [Fact]
    public void ThemeSwitchConfiguresBothOfficialFluentPalettes()
    {
        var fluentTheme = new Avalonia.Themes.Fluent.FluentTheme();
        AppTheme.ConfigureFluentTheme(fluentTheme);

        Assert.Equal(Color.Parse("#171A21"), fluentTheme.Palettes[Avalonia.Styling.ThemeVariant.Light].BaseHigh);
        Assert.Equal(Color.Parse("#E6EAF0"), fluentTheme.Palettes[Avalonia.Styling.ThemeVariant.Dark].BaseHigh);
        Assert.Equal(Color.Parse("#275DAD"), fluentTheme.Palettes[Avalonia.Styling.ThemeVariant.Light].Accent);
        Assert.Equal(Color.Parse("#A3B8FF"), fluentTheme.Palettes[Avalonia.Styling.ThemeVariant.Dark].Accent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllMarkdownAndCodeHighlightingColorsMeetAa(bool dark)
    {
        var theme = dark ? "Dark" : "Light";
        var canvas = Color.Parse(dark ? "#151A22" : "#FFFFFF");
        var resources = new[]
        {
            $"MarkdownDesk.Highlighting.Code.{theme}.xshd",
            $"MarkdownDesk.Highlighting.Markdown.{theme}.xshd",
            $"MarkdownDesk.Highlighting.Markdown.Source.{theme}.xshd"
        };
        var failures = new List<string>();

        foreach (var resourceName in resources)
        {
            using var stream = typeof(MarkdownHighlighting).Assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);
            var document = XDocument.Load(stream);
            foreach (var colorElement in document.Descendants().Where(element => element.Name.LocalName == "Color"))
            {
                var foreground = (string?)colorElement.Attribute("foreground");
                if (foreground is null) continue;
                var background = (string?)colorElement.Attribute("background") ?? $"#{canvas.R:X2}{canvas.G:X2}{canvas.B:X2}";
                var ratio = ContrastRatio(Color.Parse(foreground), Color.Parse(background));
                if (ratio < 4.5)
                    failures.Add($"{resourceName}/{(string?)colorElement.Attribute("name")}: {ratio:F2}:1");
            }
        }

        Assert.True(failures.Count == 0, string.Join("; ", failures));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkdownSyntaxDefinitionLoadsForBothThemes(bool dark)
    {
        var definition = MarkdownHighlighting.Load(dark);

        Assert.NotNull(definition);
        Assert.Equal("Markdown", definition.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceMarkdownLinksHaveAccessibleContrastWithoutOtherSyntaxColors(bool dark)
    {
        var definition = MarkdownHighlighting.LoadSourceLinks(dark);
        var document = new TextDocument("# Heading\n[link](https://example.com)");
        var engine = new HighlightingEngine(definition.MainRuleSet);
        var sections = document.Lines
            .SelectMany(line => engine.HighlightLine(document, line).Sections)
            .ToArray();
        var linkSection = Assert.Single(sections, section => section.Color.Name == "Link");
        Assert.NotNull(linkSection.Color.Foreground);
        var resourceName = dark
            ? "MarkdownDesk.Highlighting.Markdown.Source.Dark.xshd"
            : "MarkdownDesk.Highlighting.Markdown.Source.Light.xshd";
        using var stream = typeof(MarkdownHighlighting).Assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        var linkHex = XDocument.Load(stream).Descendants().Single(element => element.Name.LocalName == "Color" && (string?)element.Attribute("name") == "Link").Attribute("foreground")?.Value;
        Assert.NotNull(linkHex);
        var linkColor = Color.Parse(linkHex);
        var canvas = Color.Parse(dark ? "#151A22" : "#FFFFFF");

        Assert.True(ContrastRatio(linkColor, canvas) >= 4.5);
        Assert.DoesNotContain(sections, section => section.Color.Name == "Heading");
    }

    private static double ContrastRatio(Color first, Color second)
    {
        var firstLuminance = RelativeLuminance(first);
        var secondLuminance = RelativeLuminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05) / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Linearize(byte value)
        {
            var channel = value / 255d;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linearize(color.R) + 0.7152 * Linearize(color.G) + 0.0722 * Linearize(color.B);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenericCodeSyntaxDefinitionLoadsForBothThemes(bool dark)
    {
        Assert.Equal("Code", MarkdownCodeHighlighting.Load(dark).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenericCodeDefinitionColorsKeywordsStringsAndComments(bool dark)
    {
        var document = new TextDocument("// comment\npublic class Example { string value = \"hello\"; }");
        var engine = new HighlightingEngine(MarkdownCodeHighlighting.Load(dark).MainRuleSet);
        var colors = document.Lines
            .SelectMany(line => engine.HighlightLine(document, line).Sections)
            .Select(section => section.Color.Name)
            .ToArray();

        Assert.Contains("Comment", colors);
        Assert.Contains("Keyword", colors);
        Assert.Contains("String", colors);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryPreviewCodeSyntaxColorMeetsAaContrast(bool dark)
    {
        var resourceName = dark
            ? "MarkdownDesk.Highlighting.Code.Dark.xshd"
            : "MarkdownDesk.Highlighting.Code.Light.xshd";
        using var stream = typeof(MarkdownCodeHighlighting).Assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        var document = XDocument.Load(stream);
        var background = Color.Parse(dark ? "#151A22" : "#FFFFFF");
        var foregrounds = document.Descendants()
            .Where(element => element.Name.LocalName == "Color")
            .Select(element => (string?)element.Attribute("foreground"))
            .Where(value => value is not null)
            .Select(value => Color.Parse(value!))
            .ToArray();

        Assert.NotEmpty(foregrounds);
        Assert.All(foregrounds, foreground => Assert.True(ContrastRatio(foreground, background) >= 4.5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkdownSyntaxHighlightsCommonMarkdownElements(bool dark)
    {
        var definition = MarkdownHighlighting.Load(dark);
        var document = new TextDocument("# Heading\n\nA **bold** and *italic* [link](https://example.com) with `inline code`.\n\n- item\n\n```sh\necho hello\n```");
        var engine = new HighlightingEngine(definition.MainRuleSet);

        var highlighted = document.Lines.Select(line => engine.HighlightLine(document, line)).ToArray();
        var codeLineNumber = document.Lines.First(line => document.GetText(line).Equals("echo hello", StringComparison.Ordinal)).LineNumber;
        var codeSection = highlighted[codeLineNumber - 1].Sections.FirstOrDefault(section => section.Color.Name == "Fence");

        Assert.Contains(highlighted, line => line.Sections.Count > 0);
        Assert.NotNull(codeSection);
        Assert.NotNull(codeSection.Color.Foreground);
    }

    [Theory]
    [InlineData("sh", ".sh")]
    [InlineData("csharp", ".cs")]
    [InlineData("TypeScript", ".ts")]
    [InlineData("json title=sample", ".json")]
    public void CodeFenceLanguageMapsToHighlightingExtension(string language, string expectedExtension)
    {
        Assert.Equal(expectedExtension, CodeFenceExtensions.Get(language));
    }

    [Theory]
    [InlineData(".cs")]
    [InlineData(".js")]
    [InlineData(".json")]
    public void PreviewCodeFenceHighlightingDefinitionsAreAvailable(string extension)
    {
        Assert.NotNull(HighlightingManager.Instance.GetDefinitionByExtension(extension));
    }
}
