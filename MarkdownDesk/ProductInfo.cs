using System.Reflection;

namespace MarkdownDesk;

public static class ProductInfo
{
    public static string Version
    {
        get
        {
            var fullVersion = typeof(ProductInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return string.IsNullOrWhiteSpace(fullVersion)
                ? typeof(ProductInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"
                : fullVersion.Split('+', 2)[0];
        }
    }
}
