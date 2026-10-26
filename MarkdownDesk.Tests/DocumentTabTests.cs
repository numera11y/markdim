using System.Text;
using MarkdownDesk;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class DocumentTabTests
{
    [Fact]
    public void NewDocumentStartsCleanAndUsesItsFileName()
    {
        var document = new DocumentTab(Path.Combine("notes", "daily.md"), "# Today");

        Assert.Equal("daily.md", document.Title);
        Assert.Equal("daily.md", document.TabLabel);
        Assert.False(document.Dirty);
    }

    [Fact]
    public void UntitledDocumentStartsAsAnEmptyCleanMarkdownBuffer()
    {
        var document = DocumentTab.CreateUntitled("Sans titre");

        Assert.True(document.IsUntitled);
        Assert.Equal("Sans titre", document.Title);
        Assert.Equal("", document.Text);
        Assert.False(document.Dirty);
    }

    [Fact]
    public void EditingMarksDocumentDirtyAndUpdatesTabLabel()
    {
        var document = new DocumentTab("daily.md", "# Today");
        var changedProperties = new List<string?>();
        document.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        document.Text = "# Updated";

        Assert.True(document.Dirty);
        Assert.Equal("● daily.md", document.TabLabel);
        Assert.Contains(nameof(DocumentTab.Text), changedProperties);
        Assert.Contains(nameof(DocumentTab.Dirty), changedProperties);
        Assert.Contains(nameof(DocumentTab.TabLabel), changedProperties);
    }

    [Fact]
    public void SavingClearsDirtyStateWithoutChangingText()
    {
        var document = new DocumentTab("daily.md", "first");
        document.Text = "saved content";

        document.MarkSaved();

        Assert.False(document.Dirty);
        Assert.Equal("daily.md", document.TabLabel);
        Assert.Equal("saved content", document.Text);
    }

    [Fact]
    public void SavingAsUpdatesTheDocumentPathAndTitle()
    {
        var document = new DocumentTab(Path.Combine(Path.GetTempPath(), "old-name.md"), "content");
        var targetPath = Path.Combine(Path.GetTempPath(), "new-name.md");
        var changedProperties = new List<string?>();
        document.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        document.MoveToPath(targetPath);

        Assert.Equal(Path.GetFullPath(targetPath), document.Path);
        Assert.Equal("new-name.md", document.Title);
        Assert.False(document.IsUntitled);
        Assert.Contains(nameof(DocumentTab.Path), changedProperties);
        Assert.Contains(nameof(DocumentTab.Title), changedProperties);
        Assert.Contains(nameof(DocumentTab.TabLabel), changedProperties);
    }

    [Fact]
    public async Task FileContentIsLoadedOnlyWhenTheTabIsActivated()
    {
        var path = Path.Combine(Path.GetTempPath(), $"markdim-test-{Guid.NewGuid():N}.md");

        try
        {
            File.WriteAllText(path, "first version");
            var document = new DocumentTab(path);
            File.WriteAllText(path, "latest version");

            Assert.False(document.IsLoaded);
            Assert.Equal("latest version", await document.LoadTextAsync());
            Assert.True(document.IsLoaded);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadingPreservesUtf16BomAndCrLfWhenSavingAtomically()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"markdim-source-{Guid.NewGuid():N}.md");
        var destinationPath = Path.Combine(Path.GetTempPath(), $"markdim-destination-{Guid.NewGuid():N}.md");
        const string source = "# Header\r\nText\r\n";

        try
        {
            File.WriteAllText(sourcePath, source, new UnicodeEncoding(false, true));
            var document = new DocumentTab(sourcePath);

            Assert.Equal("# Header\nText\n", await document.LoadTextAsync());
            await document.WriteToFileAtomicallyAsync(destinationPath);

            Assert.Equal(source, File.ReadAllText(destinationPath, new UnicodeEncoding(false, true)));
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (File.Exists(destinationPath)) File.Delete(destinationPath);
        }
    }

    [Fact]
    public async Task OversizedDocumentsAreRejectedBeforeReadingTheirContents()
    {
        var path = Path.Combine(Path.GetTempPath(), $"markdim-large-{Guid.NewGuid():N}.md");

        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                stream.SetLength(DocumentTab.MaximumFileBytes + 1);

            var document = new DocumentTab(path);

            await Assert.ThrowsAsync<InvalidDataException>(() => document.LoadTextAsync());
            Assert.False(document.IsLoaded);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
