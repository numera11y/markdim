using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace MarkdownDesk;

internal static class AppTheme
{
    internal readonly record struct ContrastSample(string Name, string Foreground, string Background);

    private sealed record Palette(
        string Window,
        string Surface,
        string Sidebar,
        string Canvas,
        string Border,
        string Text,
        string Muted,
        string Subtle,
        string Accent,
        string OnAccent,
        string AccentHover,
        string Danger,
        string OnDanger,
        string DangerHover,
        string Hover,
        string Divider);

    private static readonly Palette Light = new(
        "#F5F6F8", "#FFFFFF", "#EEF1F5", "#FFFFFF", "#C7CDD6", "#171A21",
        "#344054", "#475467", "#275DAD", "#FFFFFF", "#1F4F9D", "#B42318", "#FFFFFF", "#912018", "#DCE8F8", "#D0D5DD");

    private static readonly Palette Dark = new(
        "#141820", "#1C222C", "#191F29", "#151A22", "#343C49", "#E6EAF0",
        "#D0D6E0", "#B1BBC9", "#A3B8FF", "#141820", "#B6C7FF", "#C5342B", "#FFFFFF", "#B82E26", "#34445E", "#303846");

    public static bool IsDark { get; private set; }
    public static IBrush SurfaceBrush { get; private set; } = Brushes.White;
    public static IBrush SidebarBrush { get; private set; } = Brush.Parse("#EEF1F5");
    public static IBrush CanvasBrush { get; private set; } = Brushes.White;
    public static IBrush BorderBrush { get; private set; } = Brush.Parse("#C7CDD6");
    public static IBrush TextBrush { get; private set; } = Brush.Parse("#171A21");
    public static IBrush MutedTextBrush { get; private set; } = Brush.Parse("#344054");
    public static IBrush SubtleTextBrush { get; private set; } = Brush.Parse("#475467");
    public static IBrush AccentBrush { get; private set; } = Brush.Parse("#275DAD");
    public static IBrush LinkBrush { get; private set; } = Brush.Parse("#005A9C");
    public static IBrush OnAccentBrush { get; private set; } = Brushes.White;
    public static IBrush AccentHoverBrush { get; private set; } = Brush.Parse("#1F4F9D");
    public static IBrush DangerBrush { get; private set; } = Brush.Parse("#B42318");
    public static IBrush OnDangerBrush { get; private set; } = Brushes.White;
    public static IBrush DangerHoverBrush { get; private set; } = Brush.Parse("#912018");
    public static IBrush HoverBrush { get; private set; } = Brush.Parse("#DCE8F8");
    public static IBrush DividerBrush { get; private set; } = Brush.Parse("#D0D5DD");

    public static void Apply(Application application, string mode)
    {
        if (AppDiagnostics.DebugThemeOverride is { } debugTheme) mode = debugTheme;
        IsDark = string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase);
        var palette = IsDark ? Dark : Light;
        application.RequestedThemeVariant = IsDark ? ThemeVariant.Dark : ThemeVariant.Light;

        SurfaceBrush = SetBrush(application, "AppSurfaceBrush", palette.Surface);
        SidebarBrush = SetBrush(application, "AppSidebarBrush", palette.Sidebar);
        CanvasBrush = SetBrush(application, "AppCanvasBrush", palette.Canvas);
        BorderBrush = SetBrush(application, "AppBorderBrush", palette.Border);
        TextBrush = SetBrush(application, "AppTextBrush", palette.Text);
        SetBrush(application, "AppWindowBackgroundBrush", palette.Window);
        MutedTextBrush = SetBrush(application, "AppMutedTextBrush", palette.Muted);
        SubtleTextBrush = SetBrush(application, "AppSubtleTextBrush", palette.Subtle);
        AccentBrush = SetBrush(application, "AppAccentBrush", palette.Accent);
        LinkBrush = SetBrush(application, "AppLinkBrush", IsDark ? "#F0C987" : "#005A9C");
        OnAccentBrush = SetBrush(application, "AppOnAccentBrush", palette.OnAccent);
        AccentHoverBrush = SetBrush(application, "AppAccentHoverBrush", palette.AccentHover);
        DangerBrush = SetBrush(application, "AppDangerBrush", palette.Danger);
        OnDangerBrush = SetBrush(application, "AppOnDangerBrush", palette.OnDanger);
        DangerHoverBrush = SetBrush(application, "AppDangerHoverBrush", palette.DangerHover);
        HoverBrush = SetBrush(application, "AppHoverBrush", palette.Hover);
        DividerBrush = SetBrush(application, "AppDividerBrush", palette.Divider);
        var fluentTheme = application.Styles.OfType<FluentTheme>().FirstOrDefault()
            ?? throw new InvalidOperationException("markdim requires the Fluent theme to be present in Application.Styles.");
        ConfigureFluentTheme(fluentTheme);
        SetBrush(application, "SystemControlHyperlinkTextBrush", IsDark ? "#F0C987" : "#005A9C");
        SetBrush(application, "HyperlinkButtonForeground", IsDark ? "#F0C987" : "#005A9C");
        SetBrush(application, "HyperlinkButtonForegroundPointerOver", IsDark ? "#F0C987" : "#005A9C");
        SetBrush(application, "HyperlinkButtonForegroundPressed", IsDark ? "#F0C987" : "#005A9C");
        // Fluent menu items use these named aliases for normal, hovered, pressed, and disabled states.
        SetBrush(application, "MenuFlyoutPresenterBackground", palette.Surface);
        SetBrush(application, "MenuFlyoutPresenterBorderBrush", palette.Border);
        SetBrush(application, "MenuFlyoutItemBackground", palette.Surface);
        SetBrush(application, "MenuFlyoutItemBackgroundPointerOver", palette.Hover);
        SetBrush(application, "MenuFlyoutItemBackgroundPressed", palette.Hover);
        SetBrush(application, "MenuFlyoutItemBackgroundDisabled", palette.Surface);
        SetBrush(application, "MenuFlyoutItemForeground", palette.Text);
        SetBrush(application, "MenuFlyoutItemForegroundPointerOver", palette.Text);
        SetBrush(application, "MenuFlyoutItemForegroundPressed", palette.Text);
        SetBrush(application, "MenuFlyoutItemForegroundDisabled", palette.Subtle);
        SetBrush(application, "MenuFlyoutItemKeyboardAcceleratorTextForeground", palette.Subtle);
        SetBrush(application, "MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver", palette.Text);
        SetBrush(application, "MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed", palette.Text);
        SetBrush(application, "ToolTipForeground", palette.Text);
        SetBrush(application, "ToolTipBackground", palette.Surface);
        SetBrush(application, "ToolTipBorderBrush", palette.Border);
        SetBrush(application, "MenuFlyoutSubItemChevron", palette.Subtle);
        SetBrush(application, "MenuFlyoutSubItemChevronPointerOver", palette.Text);
        SetBrush(application, "MenuFlyoutSubItemChevronPressed", palette.Text);
        // Fluent ListBox selection and ComboBox dropdown items use their own resource keys.
        SetBrush(application, "SystemControlHighlightListLowBrush", palette.Hover);
        SetBrush(application, "SystemControlHighlightListMediumBrush", palette.Hover);
        SetBrush(application, "SystemControlHighlightListAccentLowBrush", palette.Accent);
        SetBrush(application, "SystemControlHighlightListAccentMediumBrush", palette.AccentHover);
        SetBrush(application, "SystemControlHighlightListAccentHighBrush", palette.AccentHover);
        SetBrush(application, "SystemControlHighlightAltBaseHighBrush", palette.OnAccent);
        SetBrush(application, "ComboBoxItemBackground", palette.Surface);
        SetBrush(application, "ComboBoxItemForeground", palette.Text);
        SetBrush(application, "ComboBoxItemBackgroundPointerOver", palette.Hover);
        SetBrush(application, "ComboBoxItemForegroundPointerOver", palette.Text);
        SetBrush(application, "ComboBoxItemBorderBrushPointerOver", palette.Border);
        SetBrush(application, "ComboBoxItemBackgroundPressed", palette.Hover);
        SetBrush(application, "ComboBoxItemForegroundPressed", palette.Text);
        SetBrush(application, "ComboBoxItemBorderBrushPressed", palette.Border);
        SetBrush(application, "ComboBoxItemBackgroundSelected", palette.Accent);
        SetBrush(application, "ComboBoxItemForegroundSelected", palette.OnAccent);
        SetBrush(application, "ComboBoxItemBorderBrushSelected", palette.Accent);
        SetBrush(application, "ComboBoxItemBackgroundSelectedPointerOver", palette.AccentHover);
        SetBrush(application, "ComboBoxItemForegroundSelectedPointerOver", palette.OnAccent);
        SetBrush(application, "ComboBoxItemBorderBrushSelectedPointerOver", palette.AccentHover);
        SetBrush(application, "ComboBoxItemBackgroundSelectedPressed", palette.AccentHover);
        SetBrush(application, "ComboBoxItemForegroundSelectedPressed", palette.OnAccent);
        SetBrush(application, "ComboBoxItemBorderBrushSelectedPressed", palette.AccentHover);
        SetBrush(application, "ComboBoxDropDownBackground", palette.Surface);
        SetBrush(application, "ComboBoxDropDownBorderBrush", palette.Border);
        SetBrush(application, "SystemControlTransientBackgroundBrush", palette.Surface);
        SetBrush(application, "SystemControlTransientBorderBrush", palette.Border);
        AppDiagnostics.Debug("theme-applied",
            ("variant", IsDark ? "dark" : "light"),
            ("text", palette.Text),
            ("muted", palette.Muted),
            ("subtle", palette.Subtle),
            ("window", palette.Window),
            ("surface", palette.Surface),
            ("sidebar", palette.Sidebar),
            ("canvas", palette.Canvas),
            ("accent", palette.Accent),
            ("link", IsDark ? "#F0C987" : "#005A9C"));
    }

    internal static void ConfigureFluentTheme(FluentTheme fluentTheme)
    {
        ArgumentNullException.ThrowIfNull(fluentTheme);
        fluentTheme.Palettes[ThemeVariant.Light] = CreateFluentPalette(Light);
        fluentTheme.Palettes[ThemeVariant.Dark] = CreateFluentPalette(Dark);
    }

    internal static ColorPaletteResources CreateFluentPalette(bool dark) => CreateFluentPalette(dark ? Dark : Light);

    private static ColorPaletteResources CreateFluentPalette(Palette palette) => new()
    {
        Accent = Color.Parse(palette.Accent),
        BaseHigh = Color.Parse(palette.Text),
        BaseMediumHigh = Color.Parse(palette.Muted),
        BaseMedium = Color.Parse(palette.Muted),
        BaseMediumLow = Color.Parse(palette.Subtle),
        BaseLow = Color.Parse(palette.Subtle),
        AltHigh = Color.Parse(palette.Surface),
        AltMediumHigh = Color.Parse(palette.Sidebar),
        AltMedium = Color.Parse(palette.Window),
        AltMediumLow = Color.Parse(palette.Hover),
        AltLow = Color.Parse(palette.Canvas),
        ChromeWhite = Color.Parse("#FFFFFF"),
        ChromeBlackHigh = Color.Parse(palette.Text),
        ChromeBlackMedium = Color.Parse(palette.Muted),
        ChromeBlackMediumLow = Color.Parse(palette.Subtle),
        ChromeBlackLow = Color.Parse(palette.Window),
        ChromeHigh = Color.Parse(palette.Text),
        ChromeMedium = Color.Parse(palette.Muted),
        ChromeMediumLow = Color.Parse(palette.Subtle),
        ChromeLow = Color.Parse(palette.Window),
        ChromeAltLow = Color.Parse(palette.Hover),
        ChromeGray = Color.Parse(palette.Muted),
        ChromeDisabledHigh = Color.Parse(palette.Subtle),
        ChromeDisabledLow = Color.Parse(palette.Subtle),
        ErrorText = Color.Parse(palette.Danger),
        ListLow = Color.Parse(palette.Sidebar),
        ListMedium = Color.Parse(palette.Hover),
        RegionColor = Color.Parse(palette.Window)
    };

    internal static IReadOnlyList<ContrastSample> GetTextContrastSamples(bool dark)
    {
        var palette = dark ? Dark : Light;
        var link = dark ? "#F0C987" : "#005A9C";
        var backgrounds = new[] { palette.Window, palette.Surface, palette.Sidebar, palette.Canvas, palette.Hover };
        var samples = new List<ContrastSample>();
        foreach (var background in backgrounds)
        {
            samples.Add(new("AppTextBrush", palette.Text, background));
            samples.Add(new("AppMutedTextBrush", palette.Muted, background));
            samples.Add(new("AppSubtleTextBrush", palette.Subtle, background));
            samples.Add(new("AppAccentBrush (inline code/checks)", palette.Accent, background));
        }

        samples.Add(new("AppLinkBrush", link, palette.Surface));
        samples.Add(new("AppLinkBrush", link, palette.Canvas));
        samples.Add(new("AppOnAccentBrush", palette.OnAccent, palette.Accent));
        samples.Add(new("AppOnAccentBrush:hover", palette.OnAccent, palette.AccentHover));
        samples.Add(new("AppOnDangerBrush", palette.OnDanger, palette.Danger));
        samples.Add(new("AppOnDangerBrush:hover", palette.OnDanger, palette.DangerHover));
        return samples;
    }

    private static IBrush SetBrush(Application application, string key, string color)
    {
        var brush = Brush.Parse(color);
        application.Resources[key] = brush;
        return brush;
    }
}
