using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MarkdownDesk;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            var captureOnOpen = args.Contains("--debug-capture", StringComparer.OrdinalIgnoreCase);
            var debugMenuOpen = args.Contains("--debug-menu", StringComparer.OrdinalIgnoreCase);
            var debugTheme = args.FirstOrDefault(argument => argument.StartsWith("--debug-theme=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2).Last();
            if (debugTheme is not ("light" or "dark")) debugTheme = null;
            var debugModal = args.FirstOrDefault(argument => argument.StartsWith("--debug-modal=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2).Last();
            if (debugModal is not ("about" or "shortcuts" or "settings")) debugModal = null;
            var debugModalHover = args.Contains("--debug-modal-hover", StringComparer.OrdinalIgnoreCase);
            var debugEnabled = captureOnOpen || debugMenuOpen || debugTheme is not null || debugModal is not null || debugModalHover || args.Contains("--debug", StringComparer.OrdinalIgnoreCase);
            AppDiagnostics.Start(debugEnabled, debugTheme, debugMenuOpen, debugModal, debugModalHover);
            var startupPaths = args
                .Where(path => !path.StartsWith("--", StringComparison.Ordinal) && (File.Exists(path) || Directory.Exists(path)))
                .ToArray();
            desktop.MainWindow = new MainWindow(startupPaths, captureOnOpen);
        }
        else AppDiagnostics.Start();
        base.OnFrameworkInitializationCompleted();
    }
}
