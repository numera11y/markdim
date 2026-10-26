namespace MarkdownDesk;

/// <summary>Restricts preview links to schemes that are safe to hand to the operating system.</summary>
public static class MarkdownLinkPolicy
{
    public static bool TryGetSafeUri(string? value, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate)) return false;
        if (candidate.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || candidate.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(candidate.Host) || !string.IsNullOrEmpty(candidate.UserInfo)) return false;
            uri = candidate;
            return true;
        }
        if (candidate.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
        {
            uri = candidate;
            return true;
        }
        return false;
    }
}
