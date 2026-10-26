using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class UserSettingsTests
{
    [Fact]
    public void ProductVersionComesFromTheCentralBuildVersion()
    {
        Assert.Equal("0.3.0-beta.3", ProductInfo.Version);
    }

    [Fact]
    public void NormalizeClampsInvalidLayoutValues()
    {
        var settings = new UserSettings
        {
            EditorFontSize = 90,
            SidebarWidth = 20,
            EditorPaneRatio = double.NaN,
            ThemeMode = "sepia"
        };

        settings.Normalize();

        Assert.Equal(32, settings.EditorFontSize);
        Assert.Equal(190, settings.SidebarWidth);
        Assert.Equal(0.5, settings.EditorPaneRatio);
        Assert.Equal("Light", settings.ThemeMode);

        var minimumFontSize = new UserSettings { EditorFontSize = 1 };
        minimumFontSize.Normalize();
        Assert.Equal(8, minimumFontSize.EditorFontSize);
    }

    [Fact]
    public void SettingsAlwaysKeepAnEditingOrReadingPaneVisible()
    {
        var settings = new UserSettings { ShowEditor = false, ShowPreview = false };

        settings.Normalize();

        Assert.True(settings.ShowEditor);
    }

    [Fact]
    public void WhitespaceAndLineEndingMarkersAreEnabledByDefault()
    {
        var settings = new UserSettings();

        Assert.True(settings.ShowWhitespaceCharacters);
        Assert.True(settings.ShowLineEndings);
    }

    [Fact]
    public void NormalizeKeepsOnlyFiveUniqueRecentFiles()
    {
        var settings = new UserSettings
        {
            RecentFiles = ["one.md", "two.md", "one.md", "", "three.md", "four.md", "five.md", "six.md"]
        };

        settings.Normalize();

        Assert.Equal(new[] { "one.md", "two.md", "three.md", "four.md", "five.md" }, settings.RecentFiles);
    }

    [Fact]
    public void SettingsRoundTripThroughJsonStore()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"marginalia-settings-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        var expected = new UserSettings
        {
            EditorFontSize = 18,
            SidebarWidth = 310,
            EditorPaneRatio = 0.4,
            ShowLibrary = false,
            ShowEditor = true,
            ShowPreview = false,
            ShowWhitespaceCharacters = false,
            ShowLineEndings = false,
            ThemeMode = "Dark",
            ResumeLastDocument = true,
            LastDocumentPath = "C:\\Notes\\daily.md",
            RecentFiles = ["C:\\Notes\\daily.md", "C:\\Notes\\ideas.md"]
        };

        try
        {
            UserSettingsStore.Save(expected, path);
            var actual = UserSettingsStore.Load(path);

            Assert.Equal(expected.EditorFontSize, actual.EditorFontSize);
            Assert.Equal(expected.SidebarWidth, actual.SidebarWidth);
            Assert.Equal(expected.EditorPaneRatio, actual.EditorPaneRatio);
            Assert.Equal(expected.ShowLibrary, actual.ShowLibrary);
            Assert.Equal(expected.ShowEditor, actual.ShowEditor);
            Assert.Equal(expected.ShowPreview, actual.ShowPreview);
            Assert.Equal(expected.ShowWhitespaceCharacters, actual.ShowWhitespaceCharacters);
            Assert.Equal(expected.ShowLineEndings, actual.ShowLineEndings);
            Assert.Equal(expected.ThemeMode, actual.ThemeMode);
            Assert.Equal(expected.ResumeLastDocument, actual.ResumeLastDocument);
            Assert.Equal(expected.LastDocumentPath, actual.LastDocumentPath);
            Assert.Equal(expected.RecentFiles, actual.RecentFiles);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
