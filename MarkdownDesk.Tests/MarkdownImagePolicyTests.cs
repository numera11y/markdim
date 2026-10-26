using MarkdownDesk;
using System.Net;
using Xunit;

namespace MarkdownDesk.Tests;

public sealed class MarkdownImagePolicyTests
{
    [Fact]
    public void RelativeImagePathsStayInsideTheMarkdownFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"markdim-images-{Guid.NewGuid():N}");
        var imageDirectory = Path.Combine(root, "images");
        Directory.CreateDirectory(imageDirectory);
        var source = Path.Combine(imageDirectory, "cover.png");

        try
        {
            Assert.True(MarkdownImagePolicy.TryResolveRelativeImagePath("images/cover.png", root, out var resolved));
            Assert.Equal(Path.GetFullPath(source), resolved);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../secret.png")]
    [InlineData("%2e%2e/secret.png")]
    [InlineData("file:///tmp/secret.png")]
    [InlineData("https://example.org/image.png")]
    [InlineData("")]
    public void AbsoluteAndEscapingImageReferencesAreRejected(string source)
    {
        Assert.False(MarkdownImagePolicy.TryResolveRelativeImagePath(source, Path.GetTempPath(), out _));
    }

    [Theory]
    [InlineData("https://octodex.github.com/images/minion.png")]
    [InlineData("https://images.example.org/photo.webp")]
    public void PublicHttpsImageUrisAreAllowed(string source)
    {
        Assert.True(MarkdownImagePolicy.TryGetRemoteImageUri(source, out var uri));
        Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
    }

    [Theory]
    [InlineData("http://images.example.org/photo.png")]
    [InlineData("file:///C:/private/photo.png")]
    [InlineData("https://user:password@example.org/photo.png")]
    [InlineData("https://localhost/photo.png")]
    [InlineData("https://127.0.0.1/photo.png")]
    [InlineData("https://192.168.1.1/photo.png")]
    [InlineData("https://example.org:8443/photo.png")]
    public void RemoteImageUrisRejectInsecureOrLocalTargets(string source)
    {
        Assert.False(MarkdownImagePolicy.TryGetRemoteImageUri(source, out _));
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.10.20", false)]
    [InlineData("::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("2001:4860:4860::8888", true)]
    public void RemoteImageHostsMustResolveToPublicAddresses(string value, bool expected)
    {
        Assert.Equal(expected, MarkdownImagePolicy.IsPublicAddress(IPAddress.Parse(value)));
    }
}
