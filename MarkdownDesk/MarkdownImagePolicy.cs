namespace MarkdownDesk;

/// <summary>Restricts preview image loads to ordinary relative files in a document's folder tree.</summary>
public static class MarkdownImagePolicy
{
    public static bool TryGetRemoteImageUri(string source, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(source, UriKind.Absolute, out var candidate)
            || candidate.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(candidate.UserInfo)
            || candidate.Port != 443
            || candidate.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || candidate.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || candidate.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || candidate.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || System.Net.IPAddress.TryParse(candidate.Host, out var address) && !IsPublicAddress(address)) return false;

        uri = candidate;
        return true;
    }

    internal static bool IsPublicAddress(System.Net.IPAddress address)
    {
        if (System.Net.IPAddress.IsLoopback(address)) return false;
        if (address.IsIPv4MappedToIPv6) return IsPublicAddress(address.MapToIPv4());

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var first = bytes[0];
            var second = bytes[1];
            return first != 0
                && first != 10
                && first != 127
                && !(first == 169 && second == 254)
                && !(first == 172 && second is >= 16 and <= 31)
                && !(first == 192 && second == 168)
                && !(first == 100 && second is >= 64 and <= 127)
                && !(first == 192 && second == 0 && bytes[2] == 0)
                && !(first == 192 && second == 0 && bytes[2] == 2)
                && !(first == 198 && second is 18 or 19)
                && !(first == 198 && second == 51 && bytes[2] == 100)
                && !(first == 203 && second == 0 && bytes[2] == 113)
                && first < 224;
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) return false;
        return (bytes[0] & 0xE0) == 0x20 // Global unicast 2000::/3 only.
            && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8); // Documentation range.
    }

    public static bool TryResolveRelativeImagePath(string source, string baseDirectory, out string path)
    {
        path = "";
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(baseDirectory)) return false;
        if (Uri.TryCreate(source, UriKind.Absolute, out _)) return false;

        try
        {
            var root = Path.GetFullPath(baseDirectory);
            var candidate = Path.GetFullPath(Path.Combine(root, Uri.UnescapeDataString(source)));
            var relativePath = Path.GetRelativePath(root, candidate);
            if (!IsWithinRoot(relativePath) || ContainsReparsePoint(root, candidate)) return false;
            path = candidate;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsWithinRoot(string relativePath) =>
        !Path.IsPathRooted(relativePath)
        && !relativePath.Equals("..", StringComparison.Ordinal)
        && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

    private static bool ContainsReparsePoint(string root, string path)
    {
        FileSystemInfo? current = new FileInfo(path);
        var pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        while (current is not null)
        {
            if (string.Equals(Path.GetFullPath(current.FullName), root, pathComparison)) break;
            if ((File.Exists(current.FullName) || Directory.Exists(current.FullName))
                && (current.Attributes & FileAttributes.ReparsePoint) != 0) return true;
            current = current switch
            {
                FileInfo file => file.Directory,
                DirectoryInfo directory => directory.Parent,
                _ => null
            };
        }
        return false;
    }
}
