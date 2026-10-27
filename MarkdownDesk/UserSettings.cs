using System.Text.Json;

namespace MarkdownDesk;

public sealed class UserSettings
{
    public double EditorFontSize { get; set; } = 14;
    public double SidebarWidth { get; set; } = 264;
    public double EditorPaneRatio { get; set; } = 0.5;
    public bool ShowLibrary { get; set; } = true;
    public bool ShowEditor { get; set; } = true;
    public bool ShowPreview { get; set; } = true;
    public bool ShowWhitespaceCharacters { get; set; } = true;
    public bool ShowLineEndings { get; set; } = true;
    public string ThemeMode { get; set; } = "Light";
    public string Language { get; set; } = "System";
    public bool ResumeLastDocument { get; set; }
    public string? LastDocumentPath { get; set; }
    public List<string> RecentFiles { get; set; } = [];

    public void Normalize()
    {
        EditorFontSize = ClampOrDefault(EditorFontSize, 8, 32, 14);
        SidebarWidth = ClampOrDefault(SidebarWidth, 190, 420, 264);
        EditorPaneRatio = ClampOrDefault(EditorPaneRatio, 0.25, 0.75, 0.5);
        ThemeMode = string.Equals(ThemeMode, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";
        Language = Localization.NormalizeLanguage(Language);
        if (string.IsNullOrWhiteSpace(LastDocumentPath)) LastDocumentPath = null;
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        RecentFiles = (RecentFiles ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(pathComparer)
            .Take(5)
            .ToList();
        if (!ShowEditor && !ShowPreview) ShowEditor = true;
    }

    private static double ClampOrDefault(double value, double minimum, double maximum, double defaultValue) =>
        double.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : defaultValue;
}

public static class UserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "markdim", "settings.json");

    public static UserSettings Load(string? path = null)
    {
        var settings = new UserSettings();
        var settingsPath = path ?? DefaultPath;
        if (path is null && !File.Exists(settingsPath))
        {
            var previousSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Marginalia", "settings.json");
            if (File.Exists(previousSettingsPath)) settingsPath = previousSettingsPath;
        }
        if (!File.Exists(settingsPath)) return settings;

        try
        {
            settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(settingsPath), JsonOptions) ?? settings;
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("load-settings", ex);
        }

        settings.Normalize();
        return settings;
    }

    public static void Save(UserSettings settings, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();
        var settingsPath = path ?? DefaultPath;
        var directory = Path.GetDirectoryName(settingsPath)
            ?? throw new ArgumentException("The settings path must include a directory.", nameof(path));
        Directory.CreateDirectory(directory);

        var temporaryPath = settingsPath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, settingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
