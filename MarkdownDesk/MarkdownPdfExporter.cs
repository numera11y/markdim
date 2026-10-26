using System.Reflection;
using System.Text.RegularExpressions;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace MarkdownDesk;

/// <summary>Renders common Markdown block conventions to a local PDF.</summary>
public static class MarkdownPdfExporter
{
    private const string SansRegular = "Marginalia-IBM-Plex-Sans";
    private const string MonoRegular = "Marginalia-JetBrains-Mono-Regular";
    private const string MonoBold = "Marginalia-JetBrains-Mono-Bold";
    private const string MonoItalic = "Marginalia-JetBrains-Mono-Italic";
    private const string MonoBoldItalic = "Marginalia-JetBrains-Mono-BoldItalic";

    static MarkdownPdfExporter() => GlobalFontSettings.FontResolver = new EmbeddedPdfFontResolver();

    public static void Export(string markdown, string documentName, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var document = new Document();
        document.Info.Title = documentName;
        var normalStyle = document.Styles[StyleNames.Normal]
            ?? throw new InvalidOperationException("MigraDoc did not provide its Normal text style.");
        normalStyle.Font.Name = "IBM Plex Sans";
        normalStyle.Font.Size = Unit.FromPoint(11);
        normalStyle.Font.Color = Colors.Black;

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.LeftMargin = Unit.FromPoint(52);
        section.PageSetup.RightMargin = Unit.FromPoint(52);
        section.PageSetup.TopMargin = Unit.FromPoint(52);
        section.PageSetup.BottomMargin = Unit.FromPoint(52);

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Size = Unit.FromPoint(9);
        footer.AddText(documentName + "  ·  ");
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();

        var lines = markdown.Replace("\r", "").Split('\n');
        var inCodeBlock = false;
        var codeLines = new List<string>();
        foreach (var line in lines)
        {
            if (line.StartsWith("```") || line.StartsWith("~~~"))
            {
                if (inCodeBlock)
                {
                    AddCodeBlock(section, codeLines);
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
                var paragraph = section.AddParagraph(CleanInlineMarkdown(heading.Groups[2].Value));
                paragraph.Format.SpaceBefore = Unit.FromPoint(9);
                paragraph.Format.SpaceAfter = Unit.FromPoint(7);
                paragraph.Format.Font.Name = "JetBrains Mono";
                paragraph.Format.Font.Size = Unit.FromPoint(size);
                paragraph.Format.Font.Bold = true;
                paragraph.Format.KeepWithNext = true;
            }
            else if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                var paragraph = AddParagraph(section, CleanInlineMarkdown(line[2..]));
                paragraph.Format.LeftIndent = Unit.FromPoint(14);
                paragraph.Format.Font.Italic = true;
                paragraph.Format.Font.Color = Colors.DarkGray;
            }
            else if (Regex.IsMatch(line, "^\\s*[-*+]\\s+"))
            {
                var item = Regex.Replace(line, "^\\s*[-*+]\\s+", "");
                var paragraph = AddParagraph(section, "•  " + CleanInlineMarkdown(item));
                paragraph.Format.LeftIndent = Unit.FromPoint(12);
                paragraph.Format.FirstLineIndent = Unit.FromPoint(-8);
            }
            else if (Regex.IsMatch(line, "^\\s*\\d+\\.\\s+"))
            {
                var item = Regex.Replace(line, "^\\s*(\\d+\\.)\\s+", "$1  ");
                var paragraph = AddParagraph(section, CleanInlineMarkdown(item));
                paragraph.Format.LeftIndent = Unit.FromPoint(12);
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                AddParagraph(section, CleanInlineMarkdown(line));
            }
            else
            {
                section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(3);
            }
        }
        if (inCodeBlock) AddCodeBlock(section, codeLines);

        var temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var renderer = new PdfDocumentRenderer { Document = document };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(temporaryPath);
            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static Paragraph AddParagraph(Section section, string text)
    {
        var paragraph = section.AddParagraph(text);
        paragraph.Format.SpaceAfter = Unit.FromPoint(7);
        return paragraph;
    }

    private static void AddCodeBlock(Section section, IReadOnlyList<string> lines)
    {
        var paragraph = section.AddParagraph();
        paragraph.Format.Shading.Color = Colors.LightGray;
        paragraph.Format.LeftIndent = Unit.FromPoint(9);
        paragraph.Format.RightIndent = Unit.FromPoint(9);
        paragraph.Format.SpaceBefore = Unit.FromPoint(3);
        paragraph.Format.SpaceAfter = Unit.FromPoint(10);
        paragraph.Format.Font.Name = "JetBrains Mono";
        paragraph.Format.Font.Size = Unit.FromPoint(9);
        paragraph.Format.Font.Color = Colors.DarkSlateGray;
        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0) paragraph.AddLineBreak();
            paragraph.AddText(lines[index]);
        }
    }

    private static string CleanInlineMarkdown(string text) => Regex.Replace(text, "(!?)\\[([^\\]]+)\\]\\([^)]*\\)", "$2").Replace("**", "").Replace("__", "").Replace("*", "").Replace("_", "").Replace("`", "");

    private sealed class EmbeddedPdfFontResolver : IFontResolver
    {
        private static readonly Assembly AppAssembly = typeof(MarkdownPdfExporter).Assembly;
        private static readonly IReadOnlyDictionary<string, string> FontResources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SansRegular] = "Marginalia.PdfFonts.IBMPlexSans.ttf",
            [MonoRegular] = "Marginalia.PdfFonts.JetBrainsMono-Regular.ttf",
            [MonoBold] = "Marginalia.PdfFonts.JetBrainsMono-Bold.ttf",
            [MonoItalic] = "Marginalia.PdfFonts.JetBrainsMono-Italic.ttf",
            [MonoBoldItalic] = "Marginalia.PdfFonts.JetBrainsMono-BoldItalic.ttf"
        };

        public string DefaultFontName => "IBM Plex Sans";

        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("JetBrains Mono", StringComparison.OrdinalIgnoreCase)
                || familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase))
            {
                var monoFace = (isBold, isItalic) switch
                {
                    (true, true) => MonoBoldItalic,
                    (true, false) => MonoBold,
                    (false, true) => MonoItalic,
                    _ => MonoRegular
                };
                return new FontResolverInfo(monoFace, false, false);
            }

            return new FontResolverInfo(SansRegular, mustSimulateBold: isBold, mustSimulateItalic: isItalic);
        }

        public byte[]? GetFont(string faceName)
        {
            if (!FontResources.TryGetValue(faceName, out var resourceName)) return null;
            using var stream = AppAssembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"The embedded PDF font resource '{resourceName}' was not found.");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
