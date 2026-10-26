using System.Text;
using System.Diagnostics;

namespace MarkdownDesk;

/// <summary>Small local diagnostic log. It deliberately excludes document contents and paths.</summary>
internal static class AppDiagnostics
{
    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Marginalia", "logs", "application.log");
    private const long MaxLogBytes = 1_000_000;
    public static bool DebugEnabled { get; private set; }
    public static string? DebugThemeOverride { get; private set; }
    public static bool DebugMenuOpen { get; private set; }
    public static string? DebugModal { get; private set; }
    public static bool DebugModalHover { get; private set; }

    public static void Start(bool debugEnabled = false, string? debugThemeOverride = null, bool debugMenuOpen = false, string? debugModal = null, bool debugModalHover = false)
    {
        DebugEnabled = debugEnabled;
        DebugThemeOverride = debugThemeOverride;
        DebugMenuOpen = debugMenuOpen;
        DebugModal = debugModal;
        DebugModalHover = debugModalHover;
        Write("INFO", "application-start", debugEnabled ? $"debug=true;theme={debugThemeOverride ?? "settings"};menu={debugMenuOpen};modal={debugModal ?? "none"};modal-hover={debugModalHover}" : null);
    }

    public static void Debug(string operation, params (string Key, string Value)[] values)
    {
        if (!DebugEnabled) return;
        var detail = string.Join(";", values.Select(value => $"{value.Key}={value.Value}"));
        Write("DEBUG", operation, detail);
    }

    public static string CreateScreenshotPath(string prefix = "capture")
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Marginalia", "diagnostics");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"{prefix}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.png");
    }

    public static void Error(string operation, Exception exception)
    {
        var target = exception.TargetSite is null
            ? "unknown"
            : $"{exception.TargetSite.DeclaringType?.FullName}.{exception.TargetSite.Name}";
        var frames = new StackTrace(exception, false).GetFrames()?
            .Take(8)
            .Select(frame => $"{frame.GetMethod()?.DeclaringType?.FullName}.{frame.GetMethod()?.Name}");
        var stack = frames is null ? "" : string.Join(" <- ", frames);
        Write("ERROR", operation, $"{exception.GetType().FullName} at {target} [{stack}]");
    }

    private static void Write(string level, string operation, string? detail)
    {
        try
        {
            lock (Gate)
            {
                var directory = Path.GetDirectoryName(LogPath)!;
                Directory.CreateDirectory(directory);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length >= MaxLogBytes)
                {
                    var previous = LogPath + ".1";
                    if (File.Exists(previous)) File.Delete(previous);
                    File.Move(LogPath, previous);
                }

                var line = $"{DateTimeOffset.UtcNow:O}\t{level}\t{operation}\t{detail ?? "-"}{Environment.NewLine}";
                File.AppendAllText(LogPath, line, new UTF8Encoding(false));
            }
        }
        catch
        {
            // Diagnostics must never prevent the editor from opening or saving a document.
        }
    }
}
