namespace MarkdownDesk;

public static class CodeFenceExtensions
{
    private static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["bash"] = ".sh",
        ["sh"] = ".sh",
        ["shell"] = ".sh",
        ["zsh"] = ".sh",
        ["c"] = ".c",
        ["c++"] = ".cpp",
        ["cpp"] = ".cpp",
        ["c#"] = ".cs",
        ["csharp"] = ".cs",
        ["cs"] = ".cs",
        ["css"] = ".css",
        ["diff"] = ".diff",
        ["dockerfile"] = ".dockerfile",
        ["go"] = ".go",
        ["html"] = ".html",
        ["java"] = ".java",
        ["javascript"] = ".js",
        ["js"] = ".js",
        ["json"] = ".json",
        ["jsx"] = ".jsx",
        ["powershell"] = ".ps1",
        ["ps1"] = ".ps1",
        ["py"] = ".py",
        ["python"] = ".py",
        ["ruby"] = ".rb",
        ["rs"] = ".rs",
        ["rust"] = ".rs",
        ["sql"] = ".sql",
        ["ts"] = ".ts",
        ["tsx"] = ".tsx",
        ["typescript"] = ".ts",
        ["xml"] = ".xml",
        ["yaml"] = ".yaml",
        ["yml"] = ".yaml"
    };

    public static string? Get(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return null;
        var identifier = language.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        return Extensions.TryGetValue(identifier, out var extension) ? extension : null;
    }
}
