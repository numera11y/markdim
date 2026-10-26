using System.Text;
using MarkdownDesk;
using PdfSharp.Pdf.IO;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class MarkdownPdfExporterTests
{
    [Fact]
    public void ExportCreatesReadablePdfForMarkdownBlocks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"markdim-test-{Guid.NewGuid():N}.pdf");

        try
        {
            MarkdownPdfExporter.Export("# Édition Markdown\n\nUn paragraphe avec les accents : café, où, à, œ.\n\n- Première option\n- Deuxième option\n\n> Une citation\n\n```csharp\nConsole.WriteLine(\"été\");\n```", "sample.md", path);

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 500);
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            var pdfObjects = Encoding.Latin1.GetString(bytes);
            Assert.Contains("IBM#20Plex#20Sans#20Regular", pdfObjects);
            Assert.Contains("IBM#20Plex#20Sans#20SmBld", pdfObjects);
            Assert.Contains("IBM#20Plex#20Sans,Italic", pdfObjects);
            Assert.Contains("JetBrains#20Mono", pdfObjects);
            using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
            Assert.True(document.PageCount > 0);
            Assert.Equal("sample.md", document.Info.Title);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(null, "sample.md", "output.pdf")]
    [InlineData("content", "", "output.pdf")]
    [InlineData("content", "sample.md", " ")]
    public void ExportRejectsMissingRequiredArguments(string? markdown, string documentName, string destinationPath)
    {
        Assert.ThrowsAny<ArgumentException>(() => MarkdownPdfExporter.Export(markdown!, documentName, destinationPath));
    }
}
