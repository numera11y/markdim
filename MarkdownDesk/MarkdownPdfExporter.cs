using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace MarkdownDesk;

/// <summary>Renders common Markdown block conventions to a local PDF.</summary>
public static class MarkdownPdfExporter
{
    static MarkdownPdfExporter() => QuestPDF.Settings.License = LicenseType.Community;

    public static void Export(string markdown, string documentName, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var lines = markdown.Replace("\r", "").Split('\n');
        var temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Document.Create(container => container.Page(page =>
            {
                page.Margin(52);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#252B35"));
                page.Content().Column(column =>
                {
                    column.Spacing(7);
                    var inCodeBlock = false;
                    var codeLines = new List<string>();
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("```") || line.StartsWith("~~~"))
                        {
                            if (inCodeBlock)
                            {
                                AddCodeBlock(column, codeLines);
                                codeLines.Clear();
                            }
                            inCodeBlock = !inCodeBlock;
                            continue;
                        }

                        if (inCodeBlock)
                        {
                            codeLines.Add(line);
                            continue;
                        }

                        var heading = Regex.Match(line, "^(#{1,6})\\s+(.*)$");
                        if (heading.Success)
                        {
                            var size = heading.Groups[1].Length switch { 1 => 25, 2 => 20, 3 => 16, _ => 13 };
                            column.Item().PaddingTop(9).Text(CleanInlineMarkdown(heading.Groups[2].Value)).FontSize(size).Bold().FontColor("#202938");
                        }
                        else if (line.StartsWith("> ", StringComparison.Ordinal)) column.Item().PaddingLeft(14).Text(CleanInlineMarkdown(line[2..])).Italic().FontColor("#596579");
                        else if (Regex.IsMatch(line, "^\\s*[-*+]\\s+")) column.Item().PaddingLeft(12).Text("•  " + CleanInlineMarkdown(Regex.Replace(line, "^\\s*[-*+]\\s+", "")));
                        else if (Regex.IsMatch(line, "^\\s*\\d+\\.\\s+")) column.Item().PaddingLeft(12).Text(CleanInlineMarkdown(Regex.Replace(line, "^\\s*(\\d+\\.)\\s+", "$1  ")));
                        else if (!string.IsNullOrWhiteSpace(line)) column.Item().Text(CleanInlineMarkdown(line));
                        else column.Item().Height(5);
                    }
                    if (inCodeBlock) AddCodeBlock(column, codeLines);
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(documentName + "  ·  ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            })).GeneratePdf(temporaryPath);
            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static void AddCodeBlock(ColumnDescriptor column, IReadOnlyCollection<string> lines)
    {
        column.Item().Background("#F2F4F7").Padding(9).Text(string.Join("\n", lines))
            .FontFamily("Courier New").FontSize(9).FontColor("#263247");
    }

    private static string CleanInlineMarkdown(string text) => Regex.Replace(text, "(!?)\\[([^\\]]+)\\]\\([^)]*\\)", "$2").Replace("**", "").Replace("__", "").Replace("*", "").Replace("_", "").Replace("`", "");
}
