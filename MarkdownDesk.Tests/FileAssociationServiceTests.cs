using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class FileAssociationServiceTests
{
    [Fact]
    public void OpenWithCommandQuotesExecutablePathsContainingSpaces()
    {
        var command = FileAssociationService.BuildOpenCommandLine("C:\\Program Files\\Marginalia\\Marginalia.exe");

        Assert.Equal("\"C:\\Program Files\\Marginalia\\Marginalia.exe\" \"%1\"", command);
    }

    [Fact]
    public void OpenWithCommandRejectsMissingExecutablePath()
    {
        Assert.Throws<ArgumentException>(() => FileAssociationService.BuildOpenCommandLine(" "));
    }
}
