using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class FileAssociationServiceTests
{
    [Fact]
    public void OpenWithCommandQuotesExecutablePathsContainingSpaces()
    {
        var command = FileAssociationService.BuildOpenCommandLine("C:\\Program Files\\markdim\\markdim.exe");

        Assert.Equal("\"C:\\Program Files\\markdim\\markdim.exe\" \"%1\"", command);
    }

    [Fact]
    public void OpenWithCommandRejectsMissingExecutablePath()
    {
        Assert.Throws<ArgumentException>(() => FileAssociationService.BuildOpenCommandLine(" "));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void ManualAssociationRegistrationIsLimitedToUnpackagedWindows(bool isWindows, bool isPackaged, bool expected)
    {
        var executablePath = Path.Combine("Program Files", "markdim.exe");

        Assert.Equal(expected, FileAssociationService.CanRegisterApplication(isWindows, isPackaged, executablePath));
    }

    [Fact]
    public void OtherApplicationsCannotRegisterAsMarkdim()
    {
        var executablePath = Path.Combine("Program Files", "OtherApp.exe");

        Assert.False(FileAssociationService.CanRegisterApplication(true, false, executablePath));
    }
}
