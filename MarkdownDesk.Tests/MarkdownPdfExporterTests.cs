using System.Text;
using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class MarkdownPdfExporterTests
{
    [Fact]
    public void ExportCreatesReadablePdfForMarkdownBlocks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"marginalia-test-{Guid.NewGuid():N}.pdf");

        try
        {
            MarkdownPdfExporter.Export("# Heading\n\nA paragraph.\n\n- One item", "sample.md", path);

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 500);
            Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
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
