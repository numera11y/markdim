using Xunit;
using MarkdownDesk;
using HtmlAgilityPack;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace MarkdownDesk.Tests;

public sealed class MarkdownPreviewRendererTests
{
    private const string NestedCodeFence = "1. item 1\r\n2. item 2\r\n\r\n    ```sh\r\n    Code block\r\n    ```\r\n3. item 3";

    [Fact]
    public void OrderedListWithIndentedCodeFenceRendersWithoutLosingListStructure()
    {
        var html = MarkdownPreviewRenderer.ToHtml(NestedCodeFence);

        Assert.Contains("<ol>", html);
        Assert.Contains("<li>", html);
        Assert.Contains("language-sh", html);
        Assert.Contains("Code block", html);
        Assert.Contains("item 3", html);
    }

    [Fact]
    public void UserReportedNestedOrderedCodeBlockSyntaxParsesCorrectly()
    {
        const string markdown = "1. item 1\r\n2. item 2\r\n\r\n    ```sh\r\n    Code block\r\n    ```\r\n3. item 3";

        var html = MarkdownPreviewRenderer.ToHtml(markdown);

        Assert.Contains("<ol>", html);
        Assert.Contains("language-sh", html);
        Assert.Contains("Code block", html);
        Assert.Contains("item 3", html);
    }

    [Fact]
    public void InlineEmphasisAndTextInListItemsStayInOneParagraph()
    {
        var html = MarkdownPreviewRenderer.ToHtml("- **Éditez** avec la source et l’aperçu côte à côte");

        Assert.Contains("<li><strong>Éditez</strong> avec la source et l’aperçu côte à côte</li>", html);
    }

    [Fact]
    public void AutolinksReferenceLinksTablesAndTasksAreRendered()
    {
        const string markdown = """
            See https://example.org and <person@example.org>.

            [Guide][ref]

            [ref]: https://example.org/guide

            | Name | Value |
            | --- | --- |
            | Item | 42 |

            - [x] Done
            - [ ] Next

            ~~removed~~
            """;

        var html = MarkdownPreviewRenderer.ToHtml(markdown);

        Assert.Contains("href=\"https://example.org\"", html);
        Assert.Contains("mailto:person@example.org", html);
        Assert.Contains("/guide", html);
        Assert.Contains("<table>", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("<del>removed</del>", html);
    }

    [Fact]
    public void MarkdownGuideCoverageFixtureProducesCompletePreviewStructure()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MarkdownFeatureCoverage.md");
        var html = MarkdownPreviewRenderer.ToHtml(File.ReadAllText(sourcePath));
        var document = new HtmlDocument();
        document.LoadHtml(html);

        Assert.NotNull(document.DocumentNode.SelectSingleNode("//h1"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//h4"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//blockquote//ul"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//ol//pre/code[@class='language-sh']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//a[starts-with(@href,'mailto:')]"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//table//th"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//input[@type='checkbox']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//del"));
        Assert.Contains("Raw HTML first item", document.DocumentNode.InnerText);
    }

    [Fact]
    public void MarkdownItFixtureCoversCoreCommonMarkAndGitHubExtensions()
    {
        var html = RenderMarkdownItFixture();
        var document = new HtmlDocument();
        document.LoadHtml(html);

        for (var level = 1; level <= 6; level++)
            Assert.NotNull(document.DocumentNode.SelectSingleNode($"//h{level}"));
        Assert.True(document.DocumentNode.SelectNodes("//hr")?.Count >= 3);
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//blockquote//blockquote"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//ul//ul//ul"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//ol[@start='57']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//code[@class='language-js']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//pre/code[contains(., 'line 1 of code')]"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//table//th"));
        Assert.Contains(document.DocumentNode.SelectNodes("//table")!, tables => tables.SelectSingleNode(".//th[contains(@style, 'right')]") is not null);
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//a[@href='http://dev.nodeca.com']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//a[@href='https://github.com/nodeca/pica']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//a[@title='title text!']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//img[@src='https://octodex.github.com/images/dojocat.jpg']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//img[@alt='Stormtroopocat'][@title='The Stormtroopocat']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//del[.='Strikethrough']"));
    }

    [Fact]
    public void MarkdownItPluginSyntaxIsExplicitlyCovered()
    {
        var html = RenderMarkdownItFixture();
        var document = new HtmlDocument();
        document.LoadHtml(html);

        // Emoji shortcodes are a markdown-it-specific plugin; Markdig leaves them as literal text.
        Assert.Contains(":wink:", document.DocumentNode.InnerText);
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//sup[.='th']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//sub[.='2']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//ins[.='Inserted text']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//mark[.='Marked text']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//div[contains(@class, 'footnotes')]"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//dl/dt"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//abbr[@title='Hyper Text Markup Language']"));
        Assert.NotNull(document.DocumentNode.SelectSingleNode("//div[contains(@class, 'warning')]"));
    }

    [Fact]
    public void MarkdownItFixtureDocumentsTypographerIsNotEnabledByDefault()
    {
        var html = RenderMarkdownItFixture();
        var document = new HtmlDocument();
        document.LoadHtml(html);
        var text = document.DocumentNode.InnerText;

        Assert.Contains("(c) (C) (r) (R) (tm) +-", text);
        Assert.Contains("test.. test...", text);
        Assert.Contains("Smartypants, double quotes", text);
    }

    [Fact]
    public void PreviewTextAndLinksUseExplicitThemeForegrounds()
    {
        var preview = Assert.IsType<StackPanel>(MarkdownPreviewRenderer.Render("Plain text [a link](https://example.org)"));
        var paragraph = Assert.IsType<TextBlock>(Assert.Single(preview.Children));
        var inlines = Assert.IsAssignableFrom<System.Collections.IEnumerable>(paragraph.Inlines);
        var inlineItems = inlines.Cast<Inline>().ToArray();
        var runs = inlineItems.OfType<Run>().ToArray();
        var link = Assert.IsType<HyperlinkButton>(inlineItems.OfType<InlineUIContainer>().Single().Child);
        var linkContent = Assert.IsType<TextBlock>(link.Content);

        Assert.Equal(Color.Parse("#171A21"), Assert.IsAssignableFrom<ISolidColorBrush>(paragraph.Foreground).Color);
        Assert.All(runs, run => Assert.Equal(Color.Parse("#171A21"), Assert.IsAssignableFrom<ISolidColorBrush>(run.Foreground).Color));
        Assert.Equal(Color.Parse("#005A9C"), Assert.IsAssignableFrom<ISolidColorBrush>(link.Foreground).Color);
        Assert.Equal(Color.Parse("#005A9C"), Assert.IsAssignableFrom<ISolidColorBrush>(linkContent.Foreground).Color);
    }

    [Theory]
    [InlineData(896, 896, 480, 480)]
    [InlineData(1440, 900, 720, 450)]
    [InlineData(320, 200, 320, 200)]
    public void RemoteImageLayoutUsesDecodedDimensions(int pixelWidth, int pixelHeight, double expectedWidth, double expectedHeight)
    {
        Assert.Equal(new Avalonia.Size(expectedWidth, expectedHeight), MarkdownPreviewRenderer.FitImageSize(pixelWidth, pixelHeight));
    }

    [Fact]
    public void ParagraphWithASingleBlockImageReturnsAnUnparentedControl()
    {
        var imagePlaceholder = new Border();
        var temporaryPanel = new StackPanel { Children = { imagePlaceholder } };

        var result = MarkdownPreviewRenderer.CollapseSingleBlock(temporaryPanel);
        var destination = new StackPanel { Children = { result } };

        Assert.Same(imagePlaceholder, Assert.Single(destination.Children));
        Assert.Empty(temporaryPanel.Children);
    }

    private static string RenderMarkdownItFixture()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MarkdownItFeatureCoverage.md");
        return MarkdownPreviewRenderer.ToHtml(File.ReadAllText(sourcePath));
    }

}
