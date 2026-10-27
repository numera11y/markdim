using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace MarkdownDesk;

/// <summary>Loads contributor-maintained JSON catalogs embedded in the application.</summary>
public static class Localization
{
    private static string _languagePreference = "System";
    private static readonly IReadOnlyDictionary<string, string> French = LoadCatalog("fr");
    private static readonly IReadOnlyDictionary<string, string> English = LoadCatalog("en");

    public static string NormalizeLanguage(string? language) => language is "fr" or "en" ? language : "System";

    public static string ResolveLanguage(string? preference = null) => (preference ?? _languagePreference) switch
    {
        "fr" => "fr",
        "en" => "en",
        _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? "fr" : "en"
    };

    public static string Translate(string value, string? language = null)
    {
        var catalog = ResolveLanguage(language) == "en" ? English : French;
        if (catalog.TryGetValue(value, out var translation)) return translation;

        // Permit switching back to French after controls have already been translated.
        return ResolveLanguage(language) == "fr"
            ? English.FirstOrDefault(pair => pair.Value == value).Key ?? value
            : French.FirstOrDefault(pair => pair.Value == value).Key ?? value;
    }

    public static string Format(string value, params object?[] arguments)
    {
        var culture = CultureInfo.GetCultureInfo(ResolveLanguage() == "fr" ? "fr-FR" : "en-US");
        return string.Format(culture, Translate(value), arguments);
    }

    public static void Apply(Control root, string? language = null)
    {
        if (language is not null) _languagePreference = NormalizeLanguage(language);
        foreach (var control in root.GetLogicalDescendants().OfType<Control>().Prepend(root))
        {
            switch (control)
            {
                case TextBlock text when text.Text is not null: text.Text = Translate(text.Text, language); break;
                case TextBox text when text.Watermark is not null: text.Watermark = Translate(text.Watermark, language); break;
                case MenuItem menu when menu.Header is string header: menu.Header = Translate(header, language); break;
                case MenuItem menu when menu.Header is AccessText accessText: accessText.Text = Translate(accessText.Text ?? string.Empty, language); break;
                case Button button when button.Content is string content: button.Content = Translate(content, language); break;
                case CheckBox check when check.Content is string content: check.Content = Translate(content, language); break;
                case Window window when window.Title is not null: window.Title = Translate(window.Title, language); break;
            }
            var tip = ToolTip.GetTip(control);
            if (tip is string tipText) ToolTip.SetTip(control, Translate(tipText, language));
            var accessibleName = Avalonia.Automation.AutomationProperties.GetName(control);
            if (accessibleName is not null) Avalonia.Automation.AutomationProperties.SetName(control, Translate(accessibleName, language));
        }
    }

    internal static IReadOnlyDictionary<string, string> LoadCatalog(string language)
    {
        var resourceName = $"markdim.Localization.{language}.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Localization catalog '{resourceName}' was not embedded.");
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Localization catalog '{resourceName}' must contain a JSON object.");

        var catalog = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String || !catalog.TryAdd(property.Name, property.Value.GetString()!))
                throw new InvalidDataException($"Localization catalog '{resourceName}' contains a duplicate key or a non-string value: '{property.Name}'.");
        }
        return catalog;
    }
}
