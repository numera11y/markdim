using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace MarkdownDesk;

public static class FileAssociationService
{
    public const string ProgrammaticId = "markdim.Markdown";
    private const string ClassesPath = @"Software\Classes";
    private const int AppModelErrorNoPackage = 15700;
    private static readonly string[] Extensions = [".md", ".markdown"];

    public static bool IsSupported => OperatingSystem.IsWindows();
    public static bool IsPackagedApplication
    {
        get
        {
            if (!IsSupported) return false;
            uint length = 0;
            try { return GetCurrentPackageFullName(ref length, null) != AppModelErrorNoPackage; }
            catch (EntryPointNotFoundException) { return false; }
        }
    }
    public static bool CanRegisterCurrentApplication =>
        CanRegisterApplication(IsSupported, IsPackagedApplication, Environment.ProcessPath);

    internal static bool CanRegisterApplication(bool isWindows, bool isPackaged, string? processPath) =>
        isWindows
        && !isPackaged
        && string.Equals(Path.GetFileNameWithoutExtension(processPath), "markdim", StringComparison.OrdinalIgnoreCase);

    public static string? RegisteredExecutablePath
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return null;
            if (IsPackagedApplication) return Environment.ProcessPath;
            using var program = Registry.CurrentUser.OpenSubKey($@"{ClassesPath}\{ProgrammaticId}\shell\open\command");
            return ExtractExecutablePath(program?.GetValue(null) as string);
        }
    }

    public static bool IsRegisteredToCurrentApplication =>
        IsPackagedApplication
        || (CanRegisterCurrentApplication
            && string.Equals(RegisteredExecutablePath, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase));

    public static string? ExtractExecutablePath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        var value = command.TrimStart();
        if (value[0] == '"')
        {
            var endQuote = value.IndexOf('"', 1);
            return endQuote > 1 ? value[1..endQuote] : null;
        }

        var firstWhitespace = value.IndexOfAny([' ', '\t']);
        return firstWhitespace < 0 ? value : value[..firstWhitespace];
    }

    public static string BuildOpenCommandLine(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return $"\"{executablePath}\" \"%1\"";
    }

    public static bool IsRegistered
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;
            if (IsPackagedApplication) return true;
            return Extensions.All(IsRegisteredForExtension);
        }
    }

    public static void RegisterCurrentApplication()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(Localization.Translate("La gestion directe du menu Ouvrir avec est disponible sous Windows."));
        if (IsPackagedApplication)
            throw new InvalidOperationException(Localization.Translate("Les associations Markdown sont gérées par le manifeste du paquet Microsoft Store."));
        var executablePath = GetPreferredExecutablePath();

        var programPath = $@"{ClassesPath}\{ProgrammaticId}";
        using (var program = Registry.CurrentUser.CreateSubKey(programPath)
            ?? throw new InvalidOperationException(Localization.Translate("Impossible de créer l’entrée markdim dans le Registre.")))
        {
            program.SetValue(null, Localization.Translate("markdim Markdown document"));
            using var icon = program.CreateSubKey("DefaultIcon");
            icon.SetValue(null, $"\"{executablePath}\",0");
            using var command = program.CreateSubKey(@"shell\open\command");
            command.SetValue(null, BuildOpenCommandLine(executablePath));
        }

        foreach (var extension in Extensions)
        {
            using var openWith = Registry.CurrentUser.CreateSubKey($@"{ClassesPath}\{extension}\OpenWithProgids")
                ?? throw new InvalidOperationException(Localization.Format("Impossible d’enregistrer l’extension {0}.", extension));
            openWith.SetValue(ProgrammaticId, string.Empty, RegistryValueKind.String);
        }

        using (var application = Registry.CurrentUser.CreateSubKey($@"{ClassesPath}\Applications\markdim.exe")
            ?? throw new InvalidOperationException(Localization.Translate("Impossible de créer l’entrée markdim dans le menu Ouvrir avec.")))
        {
            application.SetValue("FriendlyAppName", "markdim");
            using var supportedTypes = application.CreateSubKey("SupportedTypes")
                ?? throw new InvalidOperationException(Localization.Translate("Impossible d’enregistrer les types de fichiers pris en charge."));
            foreach (var extension in Extensions) supportedTypes.SetValue(extension, string.Empty, RegistryValueKind.String);
        }

        NotifyShellAssociationChanged();
    }

    public static void UnregisterCurrentApplication()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(Localization.Translate("La gestion directe du menu Ouvrir avec est disponible sous Windows."));
        if (IsPackagedApplication)
            throw new InvalidOperationException(Localization.Translate("Les associations Markdown sont gérées par le manifeste du paquet Microsoft Store."));
        foreach (var extension in Extensions)
        {
            using var openWith = Registry.CurrentUser.OpenSubKey($@"{ClassesPath}\{extension}\OpenWithProgids", writable: true);
            openWith?.DeleteValue(ProgrammaticId, throwOnMissingValue: false);
        }

        Registry.CurrentUser.DeleteSubKeyTree($@"{ClassesPath}\{ProgrammaticId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"{ClassesPath}\Applications\markdim.exe", throwOnMissingSubKey: false);
        NotifyShellAssociationChanged();
    }

    [SupportedOSPlatform("windows")]
    private static bool IsRegisteredForExtension(string extension)
    {
        using var openWith = Registry.CurrentUser.OpenSubKey($@"{ClassesPath}\{extension}");
        using var programIds = openWith?.OpenSubKey("OpenWithProgids");
        return programIds?.GetValueNames().Contains(ProgrammaticId, StringComparer.OrdinalIgnoreCase) == true;
    }

    private static string GetPreferredExecutablePath()
    {
        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentPath)
            || !Path.GetFileNameWithoutExtension(currentPath).Equals("markdim", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(Localization.Translate("L’association doit être activée depuis l’application publiée markdim.exe."));
        }

        var installedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "markdim");
        if (!Directory.Exists(installedRoot)) return currentPath;

        var currentVersion = ReadVersion(currentPath);
        var newestInstalled = Directory.EnumerateFiles(installedRoot, "markdim.exe", SearchOption.AllDirectories)
            .Select(path => (Path: path, Version: ReadVersion(path)))
            .Where(item => item.Version is not null)
            .OrderByDescending(item => item.Version)
            .FirstOrDefault();

        return newestInstalled.Version is not null
            && (currentVersion is null || newestInstalled.Version > currentVersion)
                ? newestInstalled.Path
                : currentPath;
    }

    private static Version? ReadVersion(string executablePath)
    {
        var productVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(executablePath).ProductVersion;
        var semVer = productVersion?.Split('+', '-')[0];
        return Version.TryParse(semVer, out var version) ? version : null;
    }

    private static void NotifyShellAssociationChanged()
    {
        if (OperatingSystem.IsWindows()) SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, StringBuilder? packageFullName);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
