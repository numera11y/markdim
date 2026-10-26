using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using HtmlAgilityPack;
using Markdig;
using SkiaSharp;
using System.Net;
using System.Net.Sockets;

namespace MarkdownDesk;

public static class MarkdownPreviewRenderer
{
    private const int MaximumPreviewImages = 24;
    private const long MaximumSingleImageBytes = 8_000_000;
    private const long MaximumTotalImageBytes = 24_000_000;
    private const long MaximumImagePixels = 4_000_000;
    private const long MaximumTotalImagePixels = 12_000_000;
    private static readonly SemaphoreSlim RemoteImageConcurrency = new(4, 4);
    private static readonly HttpClient RemoteImageClient = CreateRemoteImageClient();
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    private static readonly AsyncLocal<PreviewImageBudget?> CurrentImageBudget = new();

    private sealed class PreviewImageBudget
    {
        public int ImageCount { get; private set; }
        private readonly object _sync = new();
        private long _encodedBytes;
        private long _decodedPixels;
        public long EncodedBytes { get { lock (_sync) return _encodedBytes; } }
        private bool _limitMessageSent;
        public Action<string>? StatusMessage { get; }

        public PreviewImageBudget(Action<string>? statusMessage) => StatusMessage = statusMessage;

        public bool TryReserve(long bytes, long pixels)
        {
            lock (_sync)
            {
                if (ImageCount >= MaximumPreviewImages
                    || bytes > MaximumSingleImageBytes
                    || _encodedBytes + bytes > MaximumTotalImageBytes
                    || pixels > MaximumImagePixels
                    || _decodedPixels + pixels > MaximumTotalImagePixels)
                    return false;

                ImageCount++;
                _encodedBytes += bytes;
                _decodedPixels += pixels;
                return true;
            }
        }

        public bool CanReserveBytes(long bytes)
        {
            lock (_sync) return ImageCount < MaximumPreviewImages && bytes <= MaximumSingleImageBytes && _encodedBytes + bytes <= MaximumTotalImageBytes;
        }

        public bool TryMarkLimitMessage()
        {
            lock (_sync)
            {
                if (_limitMessageSent) return false;
                _limitMessageSent = true;
                return true;
            }
        }
    }

    public static string ToHtml(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return Markdown.ToHtml(markdown, Pipeline);
    }

    public static Control Render(string markdown, string? baseDirectory = null, Action<string>? statusMessage = null)
    {
        var previousBudget = CurrentImageBudget.Value;
        var budget = new PreviewImageBudget(statusMessage);
        CurrentImageBudget.Value = budget;
        try
        {
            var document = new HtmlDocument();
            document.LoadHtml(ToHtml(markdown));
            var body = document.DocumentNode.SelectSingleNode("//body") ?? document.DocumentNode;
            if (ExceedsStructureBudget(body))
            {
                ReportRenderLimit(budget);
                return PreviewLimitMessage();
            }
            var content = new StackPanel { Spacing = 0, MaxWidth = 980, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
            foreach (var node in body.ChildNodes)
            {
                var control = RenderBlock(node, baseDirectory, 0, statusMessage);
                if (control is not null) content.Children.Add(control);
            }
            return content;
        }
        finally { CurrentImageBudget.Value = previousBudget; }
    }

    private static bool ExceedsStructureBudget(HtmlNode root)
    {
        var pending = new Stack<HtmlNode>(root.ChildNodes.Reverse());
        var nodeCount = 0;
        var codeBlockCount = 0;
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (++nodeCount > MarkdownPreviewPolicy.MaximumRenderedNodes) return true;
            if (node.NodeType == HtmlNodeType.Element && node.Name.Equals("pre", StringComparison.OrdinalIgnoreCase)
                && ++codeBlockCount > MarkdownPreviewPolicy.MaximumCodeBlocks)
            {
                return true;
            }
            foreach (var child in node.ChildNodes) pending.Push(child);
        }
        return false;
    }

    private static Control PreviewLimitMessage() => new TextBlock
    {
        Text = "L’aperçu de ce document est suspendu pour préserver la réactivité. Le texte source reste entièrement accessible dans l’éditeur.",
        FontSize = 16,
        Foreground = AppTheme.SubtleTextBrush,
        TextWrapping = TextWrapping.Wrap,
        MaxWidth = 720
    };

    private static void ReportRenderLimit(PreviewImageBudget budget)
    {
        if (!budget.TryMarkLimitMessage()) return;
        budget.StatusMessage?.Invoke("Aperçu limité : le document contient trop d’éléments à afficher en même temps.");
    }

    private static Control? RenderBlock(HtmlNode node, string? baseDirectory, int depth, Action<string>? statusMessage)
    {
        if (node.NodeType == HtmlNodeType.Text)
            return string.IsNullOrWhiteSpace(node.InnerText) ? null : Paragraph([node], baseDirectory);

        if (node.NodeType != HtmlNodeType.Element) return null;

        var name = node.Name.ToLowerInvariant();
        if (name is "script" or "style" or "iframe" or "object") return null;
        if (name == "p") return RenderParagraph(node, baseDirectory);
        if (name.Length == 2 && name[0] == 'h' && char.IsDigit(name[1])) return Heading(node, baseDirectory);
        if (name is "ul" or "ol") return RenderList(node, baseDirectory, depth, statusMessage);
        if (name == "pre") return RenderCodeBlock(node, statusMessage);
        if (name == "blockquote")
        {
            return new Border
            {
                BorderBrush = AppTheme.BorderBrush,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Background = AppTheme.SidebarBrush,
                Padding = new Thickness(16, 10),
                Margin = new Thickness(0, 6, 0, 14),
                Child = RenderBlocks(node.ChildNodes, baseDirectory, depth, statusMessage)
            };
        }
        if (name == "table") return RenderTable(node, baseDirectory);
        if (name == "hr") return new Border { Height = 1, Background = AppTheme.DividerBrush, Margin = new Thickness(0, 14) };
        if (name == "img") return RenderImageBlock(node, baseDirectory);

        return RenderBlocks(node.ChildNodes, baseDirectory, depth, statusMessage);
    }

    private static Control RenderParagraph(HtmlNode paragraph, string? baseDirectory)
    {
        var imageNodes = paragraph.ChildNodes.Where(node => node.NodeType == HtmlNodeType.Element
            && node.Name.Equals("img", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (imageNodes.Length == 0) return Paragraph(paragraph.ChildNodes, baseDirectory);

        var blocks = new StackPanel { Spacing = 0 };
        var inlineNodes = new List<HtmlNode>();
        void FlushInline()
        {
            if (inlineNodes.Count == 0) return;
            blocks.Children.Add(Paragraph(inlineNodes, baseDirectory));
            inlineNodes.Clear();
        }

        foreach (var child in paragraph.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Element && child.Name.Equals("img", StringComparison.OrdinalIgnoreCase))
            {
                FlushInline();
                var image = RenderImageBlock(child, baseDirectory);
                if (image is not null) blocks.Children.Add(image);
            }
            else inlineNodes.Add(child);
        }
        FlushInline();
        return CollapseSingleBlock(blocks);
    }

    internal static Control CollapseSingleBlock(StackPanel blocks)
    {
        if (blocks.Children.Count != 1) return blocks;
        var child = blocks.Children[0];
        blocks.Children.Clear();
        return child;
    }

    private static StackPanel RenderBlocks(IEnumerable<HtmlNode> nodes, string? baseDirectory, int depth, Action<string>? statusMessage)
    {
        var panel = new StackPanel { Spacing = 0 };
        foreach (var child in nodes)
        {
            var rendered = RenderBlock(child, baseDirectory, depth, statusMessage);
            if (rendered is not null) panel.Children.Add(rendered);
        }
        return panel;
    }

    private static Control Heading(HtmlNode node, string? baseDirectory)
    {
        var level = int.Parse(node.Name.AsSpan(1), System.Globalization.CultureInfo.InvariantCulture);
        var text = CreateInlineTextBlock(node.ChildNodes, baseDirectory);
        text.FontSize = level switch { 1 => 32, 2 => 25, 3 => 20, 4 => 18, 5 => 16, _ => 14 };
        text.FontWeight = FontWeight.SemiBold;
        text.Foreground = AppTheme.TextBrush;
        text.Margin = new Thickness(0, level == 1 ? 12 : 16, 0, 8);
        return text;
    }

    private static Control Paragraph(IEnumerable<HtmlNode> nodes, string? baseDirectory)
    {
        var text = CreateInlineTextBlock(nodes, baseDirectory);
        text.FontSize = 16;
        text.FontWeight = FontWeight.SemiBold;
        text.Foreground = AppTheme.TextBrush;
        text.TextWrapping = TextWrapping.Wrap;
        text.LineHeight = 23;
        text.Margin = new Thickness(0, 2, 0, 12);
        return text;
    }

    private static TextBlock CreateInlineTextBlock(IEnumerable<HtmlNode> nodes, string? baseDirectory, IBrush? foreground = null)
    {
        var inlines = new InlineCollection();
        var effectiveForeground = foreground ?? AppTheme.TextBrush;
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Inlines = inlines, Foreground = effectiveForeground, FontWeight = FontWeight.Medium };
        foreach (var node in nodes) AppendInline(node, inlines, baseDirectory, effectiveForeground);
        return text;
    }

    private static void AppendInline(HtmlNode node, InlineCollection inlines, string? baseDirectory, IBrush foreground)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            var value = HtmlEntity.DeEntitize(node.InnerText);
            if (value.Length > 0) inlines.Add(new Run(value) { Foreground = foreground });
            return;
        }

        if (node.NodeType != HtmlNodeType.Element) return;
        var name = node.Name.ToLowerInvariant();
        if (name == "a")
        {
            var href = HtmlEntity.DeEntitize(node.GetAttributeValue("href", ""));
            if (MarkdownLinkPolicy.TryGetSafeUri(href, out var uri))
            {
                var linkText = CreateInlineTextBlock(node.ChildNodes, baseDirectory, AppTheme.LinkBrush);
                linkText.Foreground = AppTheme.LinkBrush;
                linkText.TextDecorations = TextDecorations.Underline;
                var link = new HyperlinkButton
                {
                    NavigateUri = uri,
                    Content = linkText,
                    Background = Avalonia.Media.Brushes.Transparent,
                    Foreground = AppTheme.LinkBrush,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0),
                    MinWidth = 0,
                    MinHeight = 0,
                    FontSize = 15
                };
                ToolTip.SetTip(link, uri.AbsoluteUri);
                Avalonia.Automation.AutomationProperties.SetName(link, $"Ouvrir le lien {uri.Host}");
                inlines.Add(new InlineUIContainer { Child = link, BaselineAlignment = BaselineAlignment.Center });
                return;
            }

            foreach (var child in node.ChildNodes) AppendInline(child, inlines, baseDirectory, foreground);
            return;
        }
        if (name == "br") { inlines.Add(new Run("\n")); return; }
        if (name == "img")
        {
            var image = TryLoadImage(node, baseDirectory);
            if (image is not null)
            {
                inlines.Add(new InlineUIContainer
                {
                    Child = image,
                    BaselineAlignment = BaselineAlignment.Center
                });
            }
            else
            {
                var alt = node.GetAttributeValue("alt", "image");
                inlines.Add(new Run($"[Image: {alt}]") { Foreground = AppTheme.SubtleTextBrush, FontStyle = FontStyle.Italic });
            }
            return;
        }
        if (name == "input" && node.GetAttributeValue("type", "") == "checkbox")
        {
            inlines.Add(new Run(node.Attributes["checked"] is null ? "☐ " : "☑ ") { Foreground = AppTheme.AccentBrush });
            return;
        }
        if (name is "script" or "style") return;

        var span = new Span();
        switch (name)
        {
            case "strong":
            case "b": span.FontWeight = FontWeight.Bold; break;
            case "em":
            case "i": span.FontStyle = FontStyle.Italic; break;
            case "del":
            case "s": span.TextDecorations = TextDecorations.Strikethrough; break;
            case "code":
            case "kbd":
                span.Foreground = AppTheme.AccentBrush;
                span.FontFamily = new FontFamily("avares://Marginalia/Assets/Fonts#JetBrains Mono");
                span.FontSize = 13;
                break;
            case "sup": span.FontSize = 11; span.BaselineAlignment = BaselineAlignment.Superscript; break;
            case "sub": span.FontSize = 11; span.BaselineAlignment = BaselineAlignment.Subscript; break;
        }
        var childForeground = name is "code" or "kbd" ? AppTheme.AccentBrush : foreground;
        foreach (var child in node.ChildNodes) AppendInline(child, span.Inlines, baseDirectory, childForeground);
        inlines.Add(span);
    }

    private static Control RenderList(HtmlNode list, string? baseDirectory, int depth, Action<string>? statusMessage)
    {
        var ordered = list.Name.Equals("ol", StringComparison.OrdinalIgnoreCase);
        var start = int.TryParse(list.GetAttributeValue("start", "1"), out var parsedStart) ? parsedStart : 1;
        var panel = new StackPanel { Spacing = 3, Margin = new Thickness(depth * 14, 3, 0, 10) };
        foreach (var item in list.ChildNodes.Where(child => child.Name.Equals("li", StringComparison.OrdinalIgnoreCase)))
        {
            var checkbox = item.SelectSingleNode("./input[@type='checkbox']");
            var markerText = checkbox is not null
                ? (checkbox.Attributes["checked"] is null ? "☐" : "☑")
                : ordered ? $"{start++}." : "•";
            var row = new Grid { Margin = new Thickness(0, 1) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var marker = new TextBlock
            {
                Text = markerText,
                Foreground = checkbox is null ? AppTheme.AccentBrush : AppTheme.MutedTextBrush,
                FontWeight = ordered ? FontWeight.Medium : FontWeight.Normal,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, 1, 4, 0)
            };
            var content = RenderListItem(item, checkbox, baseDirectory, depth + 1, statusMessage);
            Grid.SetColumn(marker, 0);
            Grid.SetColumn(content, 1);
            row.Children.Add(marker);
            row.Children.Add(content);
            panel.Children.Add(row);
        }
        return panel;
    }

    private static StackPanel RenderListItem(HtmlNode item, HtmlNode? checkbox, string? baseDirectory, int depth, Action<string>? statusMessage)
    {
        var content = new StackPanel { Spacing = 1 };
        var inlineNodes = new List<HtmlNode>();

        void FlushInlineNodes()
        {
            if (inlineNodes.Count == 0) return;
            content.Children.Add(Paragraph(inlineNodes, baseDirectory));
            inlineNodes.Clear();
        }

        foreach (var child in item.ChildNodes)
        {
            if (ReferenceEquals(child, checkbox)) continue;
            if (child.NodeType == HtmlNodeType.Text && string.IsNullOrWhiteSpace(child.InnerText)) continue;
            var isBlock = child.NodeType == HtmlNodeType.Element
                && child.Name.ToLowerInvariant() is "p" or "ol" or "ul" or "pre" or "blockquote" or "table" or "hr" or "div" or "section";
            if (!isBlock)
            {
                inlineNodes.Add(child);
                continue;
            }

            FlushInlineNodes();
            if (child.Name.Equals("p", StringComparison.OrdinalIgnoreCase))
            {
                var paragraph = Paragraph(child.ChildNodes, baseDirectory);
                paragraph.Margin = new Thickness(0, 0, 0, 3);
                content.Children.Add(paragraph);
                continue;
            }
            var renderedBlock = RenderBlock(child, baseDirectory, depth, statusMessage);
            if (renderedBlock is Border codeBlock) codeBlock.Margin = new Thickness(0, 4, 0, 6);
            if (renderedBlock is not null) content.Children.Add(renderedBlock);
        }

        FlushInlineNodes();
        return content;
    }

    private static Control RenderCodeBlock(HtmlNode pre, Action<string>? statusMessage)
    {
        var code = pre.SelectSingleNode("./code");
        var languageClass = code?.GetAttributeValue("class", "") ?? "";
        var language = languageClass.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith("language-", StringComparison.OrdinalIgnoreCase))?[9..];
        var source = HtmlEntity.DeEntitize(code?.InnerText ?? pre.InnerText).TrimEnd('\r', '\n');
        var desiredHeight = Math.Max(56, (source.Count(character => character == '\n') + 1) * 22 + 30);
        var editor = new TextEditor
        {
            Text = source,
            Tag = language,
            ShowLineNumbers = false,
            IsReadOnly = true,
            WordWrap = false,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            FontFamily = new FontFamily("avares://Marginalia/Assets/Fonts#JetBrains Mono"),
            FontFeatures = MarkdownCodeHighlighting.CodeFontFeatures,
            FontSize = 13,
            LineNumbersForeground = AppTheme.SubtleTextBrush,
            Foreground = AppTheme.TextBrush,
            Background = AppTheme.CanvasBrush,
            TextArea = { Margin = new Thickness(8, 5) },
            Height = desiredHeight
        };
        Avalonia.Controls.Documents.TextElement.SetFontFeatures(editor.TextArea.TextView, MarkdownCodeHighlighting.CodeFontFeatures);

        // Use the theme-aware Marginalia palette for every language. AvaloniaEdit's
        // registered language definitions can contain low-contrast fixed colors.
        editor.SyntaxHighlighting = MarkdownCodeHighlighting.Load(AppTheme.IsDark);

        var lineNumberGutter = new TextBlock
        {
            Text = string.Join(Environment.NewLine, Enumerable.Range(1, source.Count(character => character == '\n') + 1)),
            FontFamily = editor.FontFamily,
            FontSize = editor.FontSize,
            Foreground = AppTheme.SubtleTextBrush,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(8, 10, 10, 0),
            MinWidth = 20
        };
        var editorGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Height = desiredHeight
        };
        Grid.SetColumn(lineNumberGutter, 0);
        Grid.SetColumn(editor, 1);
        editorGrid.Children.Add(lineNumberGutter);
        editorGrid.Children.Add(editor);

        var codeHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Height = 30 };
        var languageLabel = new Border
        {
            Background = AppTheme.SidebarBrush,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(language) ? "CODE" : language.ToUpperInvariant(),
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = AppTheme.SubtleTextBrush
            }
        };
        var copyButton = new Button
        {
            Width = 30,
            Height = 26,
            MinWidth = 30,
            Padding = new Thickness(5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Content = new Avalonia.Controls.Shapes.Path
            {
                Width = 16,
                Height = 16,
                Stretch = Avalonia.Media.Stretch.Uniform,
                Stroke = AppTheme.SubtleTextBrush,
                StrokeThickness = 1.4,
                Data = Geometry.Parse("M8,8 V3 H19 V17 H14 M5,7 H15 V21 H5 Z")
            }
        };
        copyButton.SetValue(ToolTip.TipProperty, "Copier le code");
        Avalonia.Automation.AutomationProperties.SetName(copyButton, "Copier le bloc de code");
        copyButton.Click += async (_, _) =>
        {
            try
            {
                var clipboard = TopLevel.GetTopLevel(copyButton)?.Clipboard;
                if (clipboard is null) throw new InvalidOperationException("Le presse-papiers est indisponible.");
                await clipboard.SetTextAsync(source);
                statusMessage?.Invoke("Bloc de code copié.");
            }
            catch (Exception ex)
            {
                AppDiagnostics.Error("copy-code-block", ex);
                statusMessage?.Invoke("Impossible de copier le bloc de code.");
            }
        };
        Grid.SetColumn(copyButton, 1);
        codeHeader.Children.Add(languageLabel);
        codeHeader.Children.Add(copyButton);

        var codeContent = new StackPanel { Spacing = 8, Children = { codeHeader } };
        Control codeView = editorGrid;
        if (desiredHeight > 280)
        {
            codeView = new ScrollViewer
            {
                Content = editorGrid,
                Height = 280,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };
        }
        codeContent.Children.Add(codeView);

        return new Border
        {
            Background = AppTheme.CanvasBrush,
            BorderBrush = AppTheme.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 8, 0, 14),
            Child = codeContent
        };
    }

    private static Control RenderTable(HtmlNode table, string? baseDirectory)
    {
        var rows = table.SelectNodes(".//tr")?.ToArray() ?? [];
        if (rows.Length == 0) return new TextBlock();
        var columnCount = rows.Max(row => row.SelectNodes("./th|./td")?.Count ?? 0);
        if (columnCount == 0) return new TextBlock();

        var grid = new Grid { Margin = new Thickness(0, 6, 0, 14) };
        for (var column = 0; column < columnCount; column++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var cells = rows[rowIndex].SelectNodes("./th|./td")?.ToArray() ?? [];
            for (var column = 0; column < cells.Length; column++)
            {
                var header = cells[column].Name.Equals("th", StringComparison.OrdinalIgnoreCase);
                var cellText = CreateInlineTextBlock(cells[column].ChildNodes, baseDirectory);
                cellText.FontSize = 14;
                cellText.TextWrapping = TextWrapping.Wrap;
                var border = new Border
                {
                    Background = header ? AppTheme.SidebarBrush : AppTheme.SurfaceBrush,
                    BorderBrush = AppTheme.BorderBrush,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(10, 8),
                    Child = cellText
                };
                if (header) cellText.FontWeight = FontWeight.SemiBold;
                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, column);
                grid.Children.Add(border);
            }
        }
        return grid;
    }

    private static Control RenderImageBlock(HtmlNode imageNode, string? baseDirectory)
    {
        var image = TryLoadImage(imageNode, baseDirectory);
        if (image is not null) return image;
        return new TextBlock
        {
            Text = $"[Image: {imageNode.GetAttributeValue("alt", "image")}]",
            Foreground = AppTheme.SubtleTextBrush,
            FontStyle = FontStyle.Italic,
            Margin = new Thickness(0, 4, 0, 12)
        };
    }

    private static Control? TryLoadImage(HtmlNode imageNode, string? baseDirectory)
    {
        var source = HtmlEntity.DeEntitize(imageNode.GetAttributeValue("src", ""));
        var alt = HtmlEntity.DeEntitize(imageNode.GetAttributeValue("alt", "image"));
        var budget = CurrentImageBudget.Value;
        if (MarkdownImagePolicy.TryGetRemoteImageUri(source, out var remoteUri))
        {
            if (budget is null || !budget.CanReserveBytes(1)) return null;
            AppDiagnostics.Debug("preview-image-remote", ("phase", "request"), ("scheme", "https"));
            return StartRemoteImageLoad(remoteUri, alt, budget);
        }
        if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteImageUri)
            && absoluteImageUri.Scheme is "http" or "https")
        {
            AppDiagnostics.Debug("preview-image-remote", ("phase", "rejected"), ("scheme", absoluteImageUri.Scheme));
            return null;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(baseDirectory)
                || !MarkdownImagePolicy.TryResolveRelativeImagePath(source, baseDirectory, out var path)) return null;

            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists || budget is null) return null;
            if (!budget.CanReserveBytes(fileInfo.Length))
            {
                ReportImageLimit(budget);
                return null;
            }

            using (var codec = SKCodec.Create(path))
            {
                if (codec is null) return null;
                var decodedPixels = (long)codec.Info.Width * codec.Info.Height;
                if (!budget.TryReserve(fileInfo.Length, decodedPixels))
                {
                    ReportImageLimit(budget);
                    return null;
                }
            }

            using var stream = File.OpenRead(path);
            var bitmap = new Bitmap(stream);
            if ((long)bitmap.PixelSize.Width * bitmap.PixelSize.Height > MaximumImagePixels)
            {
                bitmap.Dispose();
                ReportImageLimit(budget);
                return null;
            }

            var image = new Image
            {
                Source = bitmap,
                Stretch = Avalonia.Media.Stretch.Uniform,
                MaxWidth = 720,
                MaxHeight = 480,
                Margin = new Thickness(0, 4, 0, 12)
            };
            AppDiagnostics.Debug("preview-image-local", ("phase", "decoded"), ("bytes", fileInfo.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("width", bitmap.PixelSize.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("height", bitmap.PixelSize.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            image.DetachedFromVisualTree += (_, _) => Dispatcher.UIThread.Post(bitmap.Dispose, DispatcherPriority.Background);
            return image;
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("load-preview-image", ex);
            return null;
        }
    }

    private static Control StartRemoteImageLoad(Uri uri, string alt, PreviewImageBudget budget)
    {
        var image = new Image
        {
            Stretch = Avalonia.Media.Stretch.Uniform,
            MaxWidth = 720,
            MaxHeight = 480,
            IsVisible = false
        };
        Avalonia.Automation.AutomationProperties.SetName(image, alt);
        var placeholder = new TextBlock
        {
            Text = $"Chargement de l’image : {alt}",
            Foreground = AppTheme.SubtleTextBrush,
            FontStyle = FontStyle.Italic,
            TextWrapping = TextWrapping.Wrap
        };
        var container = new Border
        {
            Child = new Grid { Children = { image, placeholder } },
            Margin = new Thickness(0, 4, 0, 12),
            MaxWidth = 720,
            MaxHeight = 500
        };
        var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var hasBeenAttached = false;
        image.AttachedToVisualTree += (_, _) => hasBeenAttached = true;
        image.DetachedFromVisualTree += (_, _) =>
        {
            if (hasBeenAttached) _ = CancelRemoteImageAfterDetachAsync(image, cancellation);
        };
        ToolTip.SetTip(container, $"Image distante : {uri.Host}");
        _ = LoadRemoteImageAsync(uri, alt, image, placeholder, container, budget, cancellation);
        return container;
    }

    private static async Task LoadRemoteImageAsync(Uri uri, string alt, Image image, TextBlock placeholder, Border container, PreviewImageBudget budget, CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;
        var semaphoreHeld = false;
        try
        {
            AppDiagnostics.Debug("preview-image-remote", ("phase", "waiting-for-slot"));
            await RemoteImageConcurrency.WaitAsync(token);
            semaphoreHeld = true;
            AppDiagnostics.Debug("preview-image-remote", ("phase", "sending-request"));
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await RemoteImageClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            AppDiagnostics.Debug("preview-image-remote", ("phase", "response"), ("status", ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (!response.IsSuccessStatusCode)
            {
                AppDiagnostics.Debug("preview-image-remote", ("phase", "http-failed"), ("status", ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                throw new HttpRequestException("Remote image request was not successful.");
            }
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is not ("image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp"))
            {
                AppDiagnostics.Debug("preview-image-remote", ("phase", "mime-rejected"), ("mime", mediaType ?? "missing"));
                throw new InvalidDataException("Remote resource is not an allowed image type.");
            }
            if (response.Content.Headers.ContentLength is > MaximumSingleImageBytes) { ReportImageLimit(budget); return; }

            await using var responseStream = await response.Content.ReadAsStreamAsync(token);
            using var buffer = new MemoryStream();
            var chunk = new byte[64 * 1024];
            while (true)
            {
                var read = await responseStream.ReadAsync(chunk.AsMemory(), token);
                if (read == 0) break;
                if (buffer.Length + read > MaximumSingleImageBytes) { ReportImageLimit(budget); return; }
                await buffer.WriteAsync(chunk.AsMemory(0, read), token);
            }

            var encoded = buffer.ToArray();
            AppDiagnostics.Debug("preview-image-remote", ("phase", "body-read"), ("bytes", encoded.LongLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            using (var codecStream = new SKMemoryStream(encoded))
            using (var codec = SKCodec.Create(codecStream))
            {
                if (codec is null) throw new InvalidDataException("Remote image data could not be decoded.");
                var pixels = (long)codec.Info.Width * codec.Info.Height;
                if (!budget.TryReserve(encoded.LongLength, pixels)) { ReportImageLimit(budget); return; }
            }

            using var bitmapStream = new MemoryStream(encoded);
            var bitmap = new Bitmap(bitmapStream);
            if ((long)bitmap.PixelSize.Width * bitmap.PixelSize.Height > MaximumImagePixels)
            {
                bitmap.Dispose();
                ReportImageLimit(budget);
                return;
            }
            AppDiagnostics.Debug("preview-image-remote", ("phase", "decoded"), ("bytes", encoded.LongLength.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("width", bitmap.PixelSize.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("height", bitmap.PixelSize.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                {
                    bitmap.Dispose();
                    return;
                }
                var fittedSize = FitImageSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
                image.Width = fittedSize.Width;
                image.Height = fittedSize.Height;
                container.Width = fittedSize.Width;
                container.Height = fittedSize.Height;
                image.Source = bitmap;
                image.IsVisible = true;
                placeholder.IsVisible = false;
                placeholder.Text = "";
                container.InvalidateMeasure();
                container.InvalidateArrange();
            }, DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
            AppDiagnostics.Debug("preview-image-remote", ("phase", cancellation.IsCancellationRequested ? "cancelled" : "timeout"));
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!cancellation.IsCancellationRequested) placeholder.Text = $"Délai dépassé pour l’image : {alt}";
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            AppDiagnostics.Debug("preview-image-remote", ("phase", "failed"), ("error", ex.GetType().Name));
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!token.IsCancellationRequested) placeholder.Text = $"Image indisponible : {alt}";
            }, DispatcherPriority.Background);
        }
        finally
        {
            if (semaphoreHeld) RemoteImageConcurrency.Release();
            cancellation.Dispose();
        }
    }

    private static async Task CancelRemoteImageAfterDetachAsync(Image image, CancellationTokenSource cancellation)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(750));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (image.GetVisualRoot() is not null) return;
            try { cancellation.Cancel(); }
            catch (ObjectDisposedException) { }
            if (image.Source is IDisposable disposable)
            {
                image.Source = null;
                disposable.Dispose();
            }
        }, DispatcherPriority.Background);
    }

    internal static Size FitImageSize(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0) return new Size(1, 1);
        var scale = Math.Min(1, Math.Min(720d / pixelWidth, 480d / pixelHeight));
        return new Size(Math.Max(1, Math.Round(pixelWidth * scale)), Math.Max(1, Math.Round(pixelHeight * scale)));
    }

    private static HttpClient CreateRemoteImageClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectCallback = ConnectToPublicHostAsync
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(12) };
    }

    private static async ValueTask<Stream> ConnectToPublicHostAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        AppDiagnostics.Debug("preview-image-connect", ("phase", "dns-start"));
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        AppDiagnostics.Debug("preview-image-connect", ("phase", "dns-finished"), ("addresses", addresses.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("all-public", (!addresses.Any(address => !MarkdownImagePolicy.IsPublicAddress(address))).ToString()));
        if (addresses.Length == 0 || addresses.Any(address => !MarkdownImagePolicy.IsPublicAddress(address)))
            throw new HttpRequestException("Remote image host did not resolve exclusively to public addresses.");

        Exception? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                AppDiagnostics.Debug("preview-image-connect", ("phase", "connected"));
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                socket.Dispose();
                lastError = ex;
                AppDiagnostics.Debug("preview-image-connect", ("phase", "address-failed"), ("error", ex.GetType().Name));
                if (ex is OperationCanceledException) throw;
            }
        }
        throw new HttpRequestException("Could not connect to the remote image host.", lastError);
    }

    private static void ReportImageLimit(PreviewImageBudget budget)
    {
        if (!budget.TryMarkLimitMessage() || budget.StatusMessage is not { } statusMessage) return;
        const string message = "Certaines images de l’aperçu ont été ignorées pour limiter la consommation mémoire.";
        if (Dispatcher.UIThread.CheckAccess()) statusMessage(message);
        else Dispatcher.UIThread.Post(() => statusMessage(message), DispatcherPriority.Background);
    }
}
