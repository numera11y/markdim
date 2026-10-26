using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;

namespace MarkdownDesk;

public partial class MainWindow : Window
{
    private const int MaximumOpenDocuments = 250;
    private static StringComparer FilePathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private ColumnDefinition LibraryColumn => WorkspaceGrid.ColumnDefinitions[1];
    private ColumnDefinition LibrarySplitColumn => WorkspaceGrid.ColumnDefinitions[2];
    private ColumnDefinition EditorColumn => EditorPreviewGrid.ColumnDefinitions[0];
    private ColumnDefinition PreviewSplitColumn => EditorPreviewGrid.ColumnDefinitions[1];
    private ColumnDefinition PreviewColumn => EditorPreviewGrid.ColumnDefinitions[2];
    private readonly ObservableCollection<DocumentTab> _documents = new();
    private readonly ObservableCollection<HeadingOutlineItem> _headingOutline = new();
    private readonly string[] _startupPaths;
    private readonly bool _captureAfterStartup;
    private DocumentTab? _active;
    private CancellationTokenSource? _activationCancellation;
    private long _activationRequest;
    private bool _loading;
    private bool _tabRebuild;
    private bool _suppressFileSelection;
    private bool _headingOutlineRebuild;
    private bool _closePromptOpen;
    private bool _allowClose;
    private UserSettings _settings = UserSettingsStore.Load();
    private TextBlock? _associationStatus;
    private Button? _associationButton;
    private Window? _settingsWindow;
    private Window? _shortcutsWindow;
    private Avalonia.Controls.WindowState _windowStateBeforeFullScreen = Avalonia.Controls.WindowState.Maximized;
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };
    private readonly Dictionary<DocumentTab, Button> _tabButtons = new();
    private readonly HashSet<MenuItem> _contrastStyledMenuItems = new();
    public MainWindow() : this(null, false) { }

    public MainWindow(IEnumerable<string>? startupPaths, bool captureAfterStartup = false)
    {
        InitializeComponent();
        _startupPaths = startupPaths?.ToArray() ?? [];
        _captureAfterStartup = captureAfterStartup;
        using (var iconStream = AssetLoader.Open(new Uri("avares://Marginalia/Assets/marginalia.ico")))
            Icon = new WindowIcon(iconStream);
        RefreshRecentFilesMenu();
        Editor.TextArea.Margin = new Avalonia.Thickness(24, 18);
        Editor.FontFeatures = MarkdownCodeHighlighting.CodeFontFeatures;
        Avalonia.Controls.Documents.TextElement.SetFontFeatures(Editor.TextArea.TextView, MarkdownCodeHighlighting.CodeFontFeatures);
        ApplyWhitespaceSettings();
        Editor.AddHandler(InputElement.PointerWheelChangedEvent, EditorPointerWheelChanged, RoutingStrategies.Tunnel);
        _previewTimer.Tick += (_, _) =>
        {
            _previewTimer.Stop();
            var text = Editor.Text ?? "";
            UpdatePreview(text);
            UpdateHeadingOutline(text);
        };
        FileList.ItemsSource = _documents;
        HeadingOutlineList.ItemsSource = _headingOutline;
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateStatusBar();
        WorkspaceGrid.AddHandler(DragDrop.DragOverEvent, WorkspaceDragOver, RoutingStrategies.Bubble);
        WorkspaceGrid.AddHandler(DragDrop.DropEvent, WorkspaceDrop, RoutingStrategies.Bubble);
        ApplySettings();
        LibrarySplitter.PointerReleased += (_, _) => PersistLayoutSettings();
        PreviewSplitter.PointerReleased += (_, _) => PersistLayoutSettings();
        Opened += async (_, _) =>
        {
            ApplyWatermarkContrast();
            if (AppDiagnostics.DebugMenuOpen) FileMenuItem.IsSubMenuOpen = true;
            if (AppDiagnostics.DebugModal is { } debugModal)
            {
                Dispatcher.UIThread.Post(() => OpenDebugModal(debugModal), DispatcherPriority.Background);
            }
            if (_documents.Count > 0) return;
            if (_startupPaths.Length > 0)
            {
                foreach (var path in _startupPaths) await OpenPathAsync(path);
                if (_documents.Count == 0) ShowWelcome();
                if (_captureAfterStartup) await CaptureAfterStartupAsync();
                return;
            }
            if (_settings.ResumeLastDocument
                && !string.IsNullOrWhiteSpace(_settings.LastDocumentPath)
                && File.Exists(_settings.LastDocumentPath))
            {
                await OpenPathAsync(_settings.LastDocumentPath);
                if (_documents.Count == 0) ShowWelcome();
                return;
            }
            ShowWelcome();
            if (_captureAfterStartup) await CaptureAfterStartupAsync();
        };
        Closing += HandleClosing;
    }

    private void ShowWelcome()
    {
        const string welcomeMarkdown = "# Marginalia\n\nUn espace de travail pour lire, écrire et partager vos documents Markdown.\n\n## Écrire et prévisualiser\n\nModifiez le texte Markdown dans l’éditeur et consultez son rendu en direct dans le panneau d’aperçu.\n\n## Organiser vos documents\n\nOuvrez un fichier Markdown ou un dossier pour travailler avec plusieurs documents. Retrouvez-les dans la bibliothèque, recherchez par nom ou chemin, puis passez d’un onglet à l’autre.\n\n## Exporter et personnaliser\n\nExportez vos documents en PDF et adaptez l’apparence de l’application dans les paramètres. Vos fichiers restent sur votre appareil.\n\n### Pour commencer\n\nChoisissez **Fichier → Ouvrir un fichier** ou **Ouvrir un dossier**. Vous pouvez aussi déposer un fichier Markdown sur l’application.";
        _activationRequest++;
        _activationCancellation?.Cancel();
        _previewTimer.Stop();
        _loading = true;
        try { Editor.Text = welcomeMarkdown; }
        finally { _loading = false; }
        Title = "Marginalia";
        UpdatePreview(welcomeMarkdown);
        UpdateHeadingOutline(welcomeMarkdown);
        StatusText.Text = "Ouvrez un fichier Markdown ou choisissez un dossier pour commencer.";
        UpdateStatusBar();
    }

    private void NewDocumentClick(object? sender, RoutedEventArgs e)
    {
        var number = 1;
        string title;
        do
        {
            title = number == 1 ? "Sans titre" : $"Sans titre {number}";
            number++;
        }
        while (_documents.Any(document => document.IsUntitled && string.Equals(document.Title, title, StringComparison.CurrentCultureIgnoreCase)));

        var document = DocumentTab.CreateUntitled(title);
        _suppressFileSelection = true;
        try { _documents.Add(document); }
        finally { _suppressFileSelection = false; }
        _ = ActivateAsync(document);
    }

    public void OpenPath(string path) => _ = OpenPathAsync(path);

    public async Task OpenPathAsync(string path)
    {
        if (Directory.Exists(path)) { await OpenFolderAsync(path); return; }
        if (!File.Exists(path) || !new[] { ".md", ".markdown" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) return;
        var fullPath = Path.GetFullPath(path);
        var existing = _documents.FirstOrDefault(document => !document.IsUntitled && FilePathComparer.Equals(Path.GetFullPath(document.Path), fullPath));
        if (existing is null)
        {
            if (_documents.Count >= MaximumOpenDocuments)
            {
                StatusText.Text = $"Limite de {MaximumOpenDocuments} documents ouverts atteinte.";
                return;
            }
            existing = new DocumentTab(fullPath);
            _suppressFileSelection = true;
            try { _documents.Add(existing); }
            finally { _suppressFileSelection = false; }
        }
        if (!await ActivateAsync(existing))
        {
            if (existing.IsUntitled == false && !existing.IsLoaded && _documents.Remove(existing))
            {
                RebuildTabs();
                RefreshFileList();
                if (_documents.Count == 0) ShowWelcome();
            }
            return;
        }
        RecordRecentFiles([existing.Path]);
    }

    private void RecordRecentFiles(IEnumerable<string> paths)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var openedPaths = paths
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(comparer)
            .ToArray();
        if (openedPaths.Length == 0) return;

        _settings.RecentFiles = openedPaths
            .Concat(_settings.RecentFiles)
            .Distinct(comparer)
            .Take(5)
            .ToList();
        PersistSettingsSilently();
        RefreshRecentFilesMenu();
    }

    private void RefreshRecentFilesMenu()
    {
        OpenRecentMenuItem.Items.Clear();
        if (_settings.RecentFiles.Count == 0)
        {
            OpenRecentMenuItem.Items.Add(new MenuItem { Header = "Aucun fichier récent", IsEnabled = false });
            return;
        }

        foreach (var path in _settings.RecentFiles.Take(5))
        {
            var item = new MenuItem { Header = Path.GetFileName(path), Tag = path };
            ToolTip.SetTip(item, path);
            Avalonia.Automation.AutomationProperties.SetName(item, $"Ouvrir le fichier récent {Path.GetFileName(path)}");
            item.Click += (_, _) => OpenRecentFile(path);
            OpenRecentMenuItem.Items.Add(item);
        }

        OpenRecentMenuItem.Items.Add(new Separator());
        var clearItem = new MenuItem { Header = "Effacer les fichiers récents" };
        clearItem.Click += ClearRecentFilesClick;
        OpenRecentMenuItem.Items.Add(clearItem);
    }

    private void OpenRecentFile(string path)
    {
        if (!File.Exists(path))
        {
            var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _settings.RecentFiles.RemoveAll(recent => comparer.Equals(recent, path));
            PersistSettingsSilently();
            RefreshRecentFilesMenu();
            StatusText.Text = $"Fichier récent introuvable : {path}";
            return;
        }

        OpenPath(path);
    }

    private void ClearRecentFilesClick(object? sender, RoutedEventArgs e)
    {
        _settings.RecentFiles.Clear();
        PersistSettingsSilently();
        RefreshRecentFilesMenu();
        StatusText.Text = "Liste des fichiers récents effacée.";
    }

    private async void OpenFileClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Ouvrir un fichier Markdown", AllowMultiple = true, FileTypeFilter = new[] { new FilePickerFileType("Markdown") { Patterns = new[] { "*.md", "*.markdown" } } } });
            foreach (var file in files)
            {
                using (file) await OpenPathAsync(file.Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("open-file-picker", ex);
            StatusText.Text = $"Impossible d’ouvrir le sélecteur de fichiers : {ex.Message}";
        }
    }
    private async void OpenFolderClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Ouvrir un dossier", AllowMultiple = false });
            if (folders.Count > 0)
            {
                using (folders[0]) await OpenFolderAsync(folders[0].Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("open-folder-picker", ex);
            StatusText.Text = $"Impossible d’ouvrir le sélecteur de dossiers : {ex.Message}";
        }
    }
    private async Task OpenFolderAsync(string folder)
    {
        try
        {
            var paths = await Task.Run(() => Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                .Where(p => new[] { ".md", ".markdown" }.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
                .Take(MaximumOpenDocuments + 1)
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray());
            var truncated = paths.Length > MaximumOpenDocuments;
            if (truncated) paths = paths.Take(MaximumOpenDocuments).ToArray();
            var folderDocuments = new List<DocumentTab>(paths.Length);
            _suppressFileSelection = true;
            try
            {
                foreach (var path in paths)
                {
                    var document = _documents.FirstOrDefault(openDocument => !openDocument.IsUntitled && FilePathComparer.Equals(Path.GetFullPath(openDocument.Path), Path.GetFullPath(path)));
                    if (document is null)
                    {
                        if (_documents.Count >= MaximumOpenDocuments)
                        {
                            truncated = true;
                            continue;
                        }
                        document = new DocumentTab(path);
                        _documents.Add(document);
                    }
                    folderDocuments.Add(document);
                }
            }
            finally { _suppressFileSelection = false; }
            if (folderDocuments.Count > 0)
            {
                if (await ActivateAsync(folderDocuments[0])) RecordRecentFiles(folderDocuments.Select(document => document.Path));
            }
            StatusText.Text = truncated
                ? $"Limite de {MaximumOpenDocuments} documents atteinte dans {Path.GetFileName(folder)}."
                : $"{folderDocuments.Count} document{(folderDocuments.Count == 1 ? "" : "s")} Markdown ouverts depuis {Path.GetFileName(folder)}.";
        }
        catch (Exception ex) { AppDiagnostics.Error("open-folder", ex); StatusText.Text = $"Could not read folder: {ex.Message}"; }
    }
    private async Task<bool> ActivateAsync(DocumentTab? document)
    {
        var request = ++_activationRequest;
        var cancellation = new CancellationTokenSource();
        var previousCancellation = _activationCancellation;
        _activationCancellation = cancellation;
        previousCancellation?.Cancel();

        try
        {
            string content;
            try { content = document is null ? "" : await document.LoadTextAsync(cancellation.Token); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return true; }
            catch (Exception ex)
            {
                AppDiagnostics.Error("load-document", ex);
                if (request == _activationRequest) StatusText.Text = $"Impossible d’ouvrir {document?.Title ?? "le document"} : {ex.Message}";
                return false;
            }

            if (request != _activationRequest || cancellation.IsCancellationRequested) return true;
            if (document is not null && !_documents.Contains(document)) return true;

            _previewTimer.Stop();
            var previousActive = _active;
            _active = document;
            _loading = true;
            try
            {
                Editor.Text = content;
                UpdatePreview(content);
                UpdateHeadingOutline(content);
            }
            catch (Exception ex)
            {
                AppDiagnostics.Error("activate-document", ex);
                _active = previousActive;
                StatusText.Text = $"Impossible d’afficher le document : {ex.Message}";
                return false;
            }
            finally { _loading = false; }

            Title = document is null ? "Marginalia" : $"Marginalia — {document.Title}{(document.Dirty ? " •" : "")}";
            if (document is not null && !document.IsUntitled)
            {
                _settings.LastDocumentPath = Path.GetFullPath(document.Path);
                PersistSettingsSilently();
            }
            RebuildTabs();
            UpdateStatusBar();
            if (document is not null)
            {
                _tabRebuild = true;
                try { FileList.SelectedItem = document; }
                finally { _tabRebuild = false; }
                StatusText.Text = "Prêt";
            }
            return true;
        }
        finally
        {
            if (ReferenceEquals(_activationCancellation, cancellation)) _activationCancellation = null;
            cancellation.Dispose();
        }
    }
    private void RebuildTabs()
    {
        Tabs.Children.Clear();
        _tabButtons.Clear();
        foreach (var document in _documents)
        {
            var active = document == _active;
            var button = new Button { Content = CreateTabLabel(document), Tag = document, Padding = new Avalonia.Thickness(12, 7), Background = Avalonia.Media.Brushes.Transparent, Foreground = AppTheme.TextBrush, BorderThickness = new Avalonia.Thickness(0), FontSize = 13, FontWeight = Avalonia.Media.FontWeight.Medium };
            ToolTip.SetTip(button, $"{document.Title} — glisser pour réorganiser les onglets");
            button.Click += (_, _) => _ = ActivateAsync((DocumentTab)button.Tag!);
            var dragStart = default(Avalonia.Point);
            var dragMoved = false;
            button.PointerPressed += (_, e) =>
            {
                if (!e.GetCurrentPoint(button).Properties.IsLeftButtonPressed || _documents.Count < 2) return;
                dragStart = e.GetPosition(Tabs);
                dragMoved = false;
                e.Pointer.Capture(button);
            };
            button.PointerMoved += (_, e) =>
            {
                if (!e.GetCurrentPoint(button).Properties.IsLeftButtonPressed) return;
                var position = e.GetPosition(Tabs);
                if (Math.Abs(position.X - dragStart.X) >= 6 || Math.Abs(position.Y - dragStart.Y) >= 6)
                    dragMoved = true;
            };
            button.PointerReleased += (_, e) =>
            {
                e.Pointer.Capture(null);
                if (!dragMoved) return;
                var sourceIndex = _documents.IndexOf(document);
                var insertionIndex = GetTabInsertionIndex(e.GetPosition(Tabs).X);
                var destinationIndex = insertionIndex > sourceIndex ? insertionIndex - 1 : insertionIndex;
                destinationIndex = Math.Clamp(destinationIndex, 0, _documents.Count - 1);
                if (destinationIndex == sourceIndex) return;
                _documents.Move(sourceIndex, destinationIndex);
                RebuildTabs();
            };
            var closeButton = new Button
            {
                Tag = document,
                Width = 24,
                Height = 24,
                MinWidth = 24,
                MinHeight = 24,
                Padding = new Avalonia.Thickness(0),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = Avalonia.Media.Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Content = new Avalonia.Controls.Shapes.Path
                {
                    Width = 12,
                    Height = 12,
                    Stretch = Avalonia.Media.Stretch.Uniform,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Data = Avalonia.Media.Geometry.Parse("M2,2 L10,10 M10,2 L2,10"),
                    Stroke = AppTheme.SubtleTextBrush,
                    StrokeThickness = 1.5,
                    StrokeLineCap = Avalonia.Media.PenLineCap.Round
                }
            };
            ToolTip.SetTip(closeButton, $"Fermer {document.Title}");
            Avalonia.Automation.AutomationProperties.SetName(closeButton, $"Fermer l’onglet {document.Title}");
            closeButton.Click += CloseTabClick;
            var tabContents = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 2, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            tabContents.Children.Add(button);
            tabContents.Children.Add(closeButton);
            var tab = new Border
            {
                Child = tabContents,
                Background = active ? AppTheme.SurfaceBrush : Avalonia.Media.Brushes.Transparent,
                BorderBrush = active ? AppTheme.BorderBrush : Avalonia.Media.Brushes.Transparent,
                BorderThickness = active ? new Avalonia.Thickness(1) : new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(6),
                Margin = new Avalonia.Thickness(0, 5, 6, 5)
            };
            Tabs.Children.Add(tab);
            _tabButtons[document] = button;
        }
    }

    private int GetTabInsertionIndex(double pointerX)
    {
        for (var index = 0; index < Tabs.Children.Count; index++)
        {
            var tab = Tabs.Children[index];
            if (pointerX < tab.Bounds.X + tab.Bounds.Width / 2) return index;
        }
        return Tabs.Children.Count;
    }

    private void EditorChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_active is not null)
        {
            _active.Text = Editor.Text ?? "";
            if (_tabButtons.TryGetValue(_active, out var tabButton)) tabButton.Content = CreateTabLabel(_active);
            Title = $"Marginalia — {_active.Title} •";
        }
        _previewTimer.Stop();
        _previewTimer.Start();
        UpdateStatusBar();
    }

    private static TextBlock CreateTabLabel(DocumentTab document) => new()
    {
        Text = document.TabLabel,
        Foreground = AppTheme.TextBrush,
        FontSize = 13,
        FontWeight = Avalonia.Media.FontWeight.Medium,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
    };

    private void UpdateStatusBar()
    {
        StatusFilePath.Text = _active is null
            ? "Aucun document ouvert"
            : _active.IsUntitled ? _active.Title : $"{_active.Title} — {_active.Path}";
        StatusCursor.Text = $"Ln {Editor.TextArea.Caret.Line}, Col {Editor.TextArea.Caret.Column}";
        StatusDocumentDetails.Text = $"{Editor.LineCount:N0} lignes · {(Editor.Text ?? "").Length:N0} caractères";
        StatusEncoding.Text = "Markdown";
    }

    private void UpdateHeadingOutline(string markdown)
    {
        _headingOutlineRebuild = true;
        try
        {
            _headingOutline.Clear();
            var scanLength = Math.Min(markdown.Length, MarkdownPreviewPolicy.MaximumCharacters);
            var lineStart = 0;
            var lineNumber = 1;
            while (lineStart <= scanLength && lineNumber <= 5_000 && _headingOutline.Count < 300)
            {
                var lineEnd = markdown.IndexOf('\n', lineStart, scanLength - lineStart);
                if (lineEnd < 0) lineEnd = scanLength;
                var line = markdown.AsSpan(lineStart, lineEnd - lineStart);
                var cursor = 0;
                while (cursor < line.Length && cursor < 4 && line[cursor] == ' ') cursor++;
                var indentation = cursor;
                var hashStart = cursor;
                while (cursor < line.Length && line[cursor] == '#') cursor++;
                var level = cursor - hashStart;
                if (indentation <= 3 && level is >= 1 and <= 6 && cursor < line.Length && (line[cursor] == ' ' || line[cursor] == '\t'))
                {
                    while (cursor < line.Length && (line[cursor] == ' ' || line[cursor] == '\t')) cursor++;
                    var titleEnd = line.Length;
                    while (titleEnd > cursor && (line[titleEnd - 1] == ' ' || line[titleEnd - 1] == '\t')) titleEnd--;
                    var closingHashStart = titleEnd;
                    while (closingHashStart > cursor && line[closingHashStart - 1] == '#') closingHashStart--;
                    if (closingHashStart < titleEnd && closingHashStart > cursor && (line[closingHashStart - 1] == ' ' || line[closingHashStart - 1] == '\t'))
                    {
                        titleEnd = closingHashStart - 1;
                        while (titleEnd > cursor && (line[titleEnd - 1] == ' ' || line[titleEnd - 1] == '\t')) titleEnd--;
                    }
                    var title = line[cursor..titleEnd].ToString();
                    if (!string.IsNullOrWhiteSpace(title)) _headingOutline.Add(new HeadingOutlineItem(title, level, lineNumber));
                }

                if (lineEnd == scanLength) break;
                lineStart = lineEnd + 1;
                lineNumber++;
            }
            HeadingOutlineList.SelectedItem = null;
        }
        finally { _headingOutlineRebuild = false; }
    }

    private void HeadingOutlineSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_headingOutlineRebuild || HeadingOutlineList.SelectedItem is not HeadingOutlineItem heading) return;
        var line = Editor.Document.GetLineByNumber(Math.Clamp(heading.LineNumber, 1, Editor.Document.LineCount));
        Editor.TextArea.Caret.Offset = line.Offset;
        Editor.Focus();
        Editor.ScrollToLine(line.LineNumber);
        Editor.TextArea.Caret.BringCaretToView();
        Dispatcher.UIThread.Post(() =>
        {
            if (Editor.Document.LineCount < line.LineNumber) return;
            Editor.ScrollToLine(line.LineNumber);
            Editor.TextArea.Caret.BringCaretToView();
        }, DispatcherPriority.Loaded);
    }

    private sealed record HeadingOutlineItem(string Title, int Level, int LineNumber)
    {
        public string DisplayText => new string(' ', (Level - 1) * 2) + Title;
    }

    private void UpdatePreview(string markdown)
    {
        if (!_settings.ShowPreview) return;
        if (!MarkdownPreviewPolicy.ShouldRender(markdown))
        {
            PreviewContent.Content = new TextBlock
            {
                Text = "Ce document est volumineux. L’aperçu est suspendu pour garder l’application réactive; le texte source reste entièrement accessible dans l’éditeur.",
                FontSize = 16,
                Foreground = AppTheme.SubtleTextBrush,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 720
            };
            return;
        }

        try
        {
            var baseDirectory = _active is null ? null : Path.GetDirectoryName(_active.Path);
            PreviewContent.Content = MarkdownPreviewRenderer.Render(markdown, baseDirectory, message => StatusText.Text = message);
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("render-preview", ex);
            PreviewContent.Content = new TextBlock
            {
                Text = "L’aperçu n’a pas pu afficher ce document. Le texte source reste ouvert et modifiable.",
                FontSize = 16,
                Foreground = AppTheme.SubtleTextBrush,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 720
            };
        }
    }

    private void ApplySettings()
    {
        _settings.Normalize();
        AppTheme.Apply(Avalonia.Application.Current!, _settings.ThemeMode);
        ApplyWatermarkContrast();
        ApplyEditorHighlighting();
        ApplyWhitespaceSettings();
        Editor.FontSize = _settings.EditorFontSize;
        LibraryPane.IsVisible = _settings.ShowLibrary;
        LibrarySplitter.IsVisible = _settings.ShowLibrary;
        LibraryColumn.Width = _settings.ShowLibrary ? new GridLength(_settings.SidebarWidth) : new GridLength(0);
        LibrarySplitColumn.Width = _settings.ShowLibrary ? new GridLength(6) : new GridLength(0);
        EditorPane.IsVisible = _settings.ShowEditor;
        PreviewPane.IsVisible = _settings.ShowPreview;
        var showPaneSplitter = _settings.ShowEditor && _settings.ShowPreview;
        PreviewSplitter.IsVisible = showPaneSplitter;
        PreviewSplitColumn.Width = showPaneSplitter ? new GridLength(6) : new GridLength(0);
        EditorColumn.Width = _settings.ShowEditor
            ? new GridLength(showPaneSplitter ? _settings.EditorPaneRatio : 1, GridUnitType.Star)
            : new GridLength(0);
        PreviewColumn.Width = _settings.ShowPreview
            ? new GridLength(showPaneSplitter ? 1 - _settings.EditorPaneRatio : 1, GridUnitType.Star)
            : new GridLength(0);
        ApplyPanelRailState();
        if (_settings.ShowPreview)
            UpdatePreview(_active?.Text ?? Editor.Text ?? "");
        RebuildTabs();
    }

    private void ApplyPanelRailState()
    {
        LibraryVisibilityButton.Background = _settings.ShowLibrary ? AppTheme.HoverBrush : Avalonia.Media.Brushes.Transparent;
        EditorVisibilityButton.Background = _settings.ShowEditor ? AppTheme.HoverBrush : Avalonia.Media.Brushes.Transparent;
        PreviewVisibilityButton.Background = _settings.ShowPreview ? AppTheme.HoverBrush : Avalonia.Media.Brushes.Transparent;
        LibraryVisibilityIcon.Path = ThemedIconPath("folder");
        EditorVisibilityIcon.Path = ThemedIconPath("file");
        PreviewVisibilityIcon.Path = ThemedIconPath("eye");
        OpenFolderIcon.Path = ThemedIconPath("folder");
    }

    private static string ThemedIconPath(string iconName) =>
        $"/Assets/Icons/{iconName}-{(AppTheme.IsDark ? "dark" : "light")}.svg";

    private void ApplyEditorHighlighting()
    {
        Editor.SyntaxHighlighting = MarkdownHighlighting.LoadSourceLinks(AppTheme.IsDark);
        Editor.TextArea.TextView.LinkTextForegroundBrush = AppTheme.LinkBrush;
        Editor.TextArea.TextView.LinkTextBackgroundBrush = Avalonia.Media.Brushes.Transparent;
    }

    private void ApplyWhitespaceSettings()
    {
        Editor.Options.ShowSpaces = _settings.ShowWhitespaceCharacters;
        Editor.Options.ShowTabs = _settings.ShowWhitespaceCharacters;
        Editor.TextArea.TextView.NonPrintableCharacterBrush = AppTheme.MutedTextBrush;
        Editor.Options.ShowSpacesGlyph = "·";
        Editor.Options.ShowTabsGlyph = "→";
        Editor.Options.ShowEndOfLine = _settings.ShowLineEndings;
        Editor.Options.EndOfLineCRLFGlyph = "↵";
        Editor.Options.EndOfLineCRGlyph = "↵";
        Editor.Options.EndOfLineLFGlyph = "↵";
    }

    private void ApplyWatermarkContrast()
    {
        var styledWatermarks = 0;
        foreach (var textBox in new[] { SearchBox, FindTextBox, ReplaceTextBox })
        {
            textBox.ApplyTemplate();
            foreach (var watermark in textBox.GetVisualDescendants().OfType<TextBlock>())
            {
                if (watermark.Name is not ("PART_Watermark" or "PART_FloatingWatermark")) continue;
                watermark.Foreground = AppTheme.SubtleTextBrush;
                watermark.Opacity = 1;
                styledWatermarks++;
            }
        }
        AppDiagnostics.Debug("watermark-style", ("count", styledWatermarks.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("foreground", DescribeBrush(AppTheme.SubtleTextBrush)));
    }

    private void PersistLayoutSettings()
    {
        if (_settings.ShowLibrary) _settings.SidebarWidth = LibraryColumn.ActualWidth;
        var paneWidth = EditorColumn.ActualWidth + PreviewColumn.ActualWidth;
        if (_settings.ShowEditor && _settings.ShowPreview && paneWidth > 0)
            _settings.EditorPaneRatio = EditorColumn.ActualWidth / paneWidth;
        SaveSettings(_settings);
    }

    private void SaveSettings(UserSettings settings)
    {
        try
        {
            UserSettingsStore.Save(settings);
            _settings = settings;
            StatusText.Text = "Paramètres enregistrés.";
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("save-settings", ex);
            StatusText.Text = $"Impossible d’enregistrer les paramètres : {ex.Message}";
        }
    }

    private void PersistSettingsSilently()
    {
        try { UserSettingsStore.Save(_settings); }
        catch (Exception ex) { AppDiagnostics.Error("save-settings", ex); }
    }

    private void SettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        var fontSize = new Slider
        {
            Minimum = 8,
            Maximum = 32,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            Value = _settings.EditorFontSize
        };
        var sidebarWidth = new Slider
        {
            Minimum = 190,
            Maximum = 420,
            TickFrequency = 10,
            IsSnapToTickEnabled = false,
            Value = _settings.SidebarWidth
        };
        Avalonia.Automation.AutomationProperties.SetName(fontSize, "Taille du texte de l’éditeur");
        Avalonia.Automation.AutomationProperties.SetName(sidebarWidth, "Largeur de la bibliothèque");
        var theme = new ComboBox
        {
            ItemsSource = new[] { "Clair", "Sombre" },
            SelectedIndex = _settings.ThemeMode == "Dark" ? 1 : 0
        };
        Avalonia.Automation.AutomationProperties.SetName(theme, "Thème de l’interface");
        var showLibrary = new CheckBox { Content = "Afficher la bibliothèque", IsChecked = _settings.ShowLibrary };
        var showEditor = new CheckBox { Content = "Afficher l’éditeur", IsChecked = _settings.ShowEditor };
        var showPreview = new CheckBox { Content = "Afficher l’aperçu en direct", IsChecked = _settings.ShowPreview };
        var showWhitespace = new CheckBox { Content = "Afficher les espaces et les tabulations (· →)", IsChecked = _settings.ShowWhitespaceCharacters };
        var showLineEndings = new CheckBox { Content = "Afficher les fins de ligne (↵)", IsChecked = _settings.ShowLineEndings };
        var resumeLastDocument = new CheckBox { Content = "Reprendre le dernier document au démarrage", IsChecked = _settings.ResumeLastDocument };
        _associationStatus = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontSize = 12 };
        _associationButton = CreateModalButton("");
        _associationButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        _associationButton.Click += ToggleFileAssociationClick;
        UpdateAssociationControls();
        var dialog = new Window
        {
            Title = "Paramètres de l’interface",
            Width = 560,
            Height = Math.Clamp(ClientSize.Height * 0.9, 420, 740),
            MinWidth = 480,
            MinHeight = 360,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        ApplyModalWindowPalette(dialog);
        _settingsWindow = dialog;
        var cancel = CreateModalButton("Annuler");
        cancel.IsCancel = true;
        var save = CreateModalButton("Enregistrer", "modal-primary");
        save.IsDefault = true;
        cancel.Click += (_, _) => dialog.Close();
        var settingsBody = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Taille du texte de l’éditeur", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                fontSize,
                new TextBlock { Text = "Largeur de la bibliothèque", FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 8, 0, 0) },
                sidebarWidth,
                new TextBlock { Text = "Thème", FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 8, 0, 0) },
                theme,
                showLibrary,
                showEditor,
                showPreview,
                showWhitespace,
                showLineEndings,
                resumeLastDocument,
                new Separator { Margin = new Avalonia.Thickness(0, 8, 0, 0) },
                new TextBlock { Text = "Association des fichiers Markdown", FontWeight = Avalonia.Media.FontWeight.SemiBold },
                _associationStatus,
                _associationButton,
                new TextBlock
                {
                    Text = "Windows contrôle séparément le choix de l’application par défaut dans ses paramètres.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 11,
                    Foreground = AppTheme.TextBrush
                }
            }
        };
        dialog.Content = CreateModalContent(settingsBody, cancel, save);
        dialog.Closed += (_, _) =>
        {
            _associationStatus = null;
            _associationButton = null;
            if (ReferenceEquals(_settingsWindow, dialog)) _settingsWindow = null;
        };
        save.Click += (_, _) =>
        {
            var settings = new UserSettings
            {
                EditorFontSize = fontSize.Value,
                SidebarWidth = sidebarWidth.Value,
                EditorPaneRatio = _settings.EditorPaneRatio,
                ShowLibrary = showLibrary.IsChecked == true,
                ShowEditor = showEditor.IsChecked == true,
                ShowPreview = showPreview.IsChecked == true,
                ShowWhitespaceCharacters = showWhitespace.IsChecked == true,
                ShowLineEndings = showLineEndings.IsChecked == true,
                ThemeMode = theme.SelectedIndex == 1 ? "Dark" : "Light",
                ResumeLastDocument = resumeLastDocument.IsChecked == true,
                LastDocumentPath = _settings.LastDocumentPath,
                RecentFiles = _settings.RecentFiles
            };
            settings.Normalize();
            try
            {
                UserSettingsStore.Save(settings);
                _settings = settings;
                ApplySettings();
                StatusText.Text = "Paramètres enregistrés.";
                dialog.Close();
            }
            catch (Exception ex)
            {
                AppDiagnostics.Error("save-settings", ex);
                StatusText.Text = $"Impossible d’enregistrer les paramètres : {ex.Message}";
                if (_associationStatus is not null)
                    _associationStatus.Text = $"Impossible d’enregistrer les paramètres : {ex.Message}";
            }
        };
        dialog.Show(this);
    }

    private void ToggleLibraryClick(object? sender, RoutedEventArgs e) => TogglePanelVisibility(nameof(UserSettings.ShowLibrary));
    private void ToggleEditorClick(object? sender, RoutedEventArgs e) => TogglePanelVisibility(nameof(UserSettings.ShowEditor));
    private void TogglePreviewClick(object? sender, RoutedEventArgs e) => TogglePanelVisibility(nameof(UserSettings.ShowPreview));

    private void TogglePanelVisibility(string panel)
    {
        switch (panel)
        {
            case nameof(UserSettings.ShowLibrary): _settings.ShowLibrary = !_settings.ShowLibrary; break;
            case nameof(UserSettings.ShowEditor): _settings.ShowEditor = !_settings.ShowEditor; break;
            case nameof(UserSettings.ShowPreview): _settings.ShowPreview = !_settings.ShowPreview; break;
        }
        _settings.Normalize();
        ApplySettings();
        SaveSettings(_settings);
    }

    private void ToggleFileAssociationClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var isRegistered = FileAssociationService.IsRegistered;
            if (isRegistered && FileAssociationService.IsRegisteredToCurrentApplication)
                FileAssociationService.UnregisterCurrentApplication();
            else
                FileAssociationService.RegisterCurrentApplication();
            UpdateAssociationControls();
            StatusText.Text = FileAssociationService.IsRegistered
                ? "Ouvrir avec pointe vers cette version de Marginalia."
                : "Marginalia a été retirée de Ouvrir avec.";
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("file-association", ex);
            if (_associationStatus is not null) _associationStatus.Text = ex.Message;
        }
    }

    private void UpdateAssociationControls()
    {
        if (_associationStatus is null || _associationButton is null) return;
        if (!FileAssociationService.IsSupported)
        {
            _associationStatus.Text = "L’association interactive est gérée par le paquet d’installation de ce système.";
            SetModalButtonLabel(_associationButton, "Indisponible sur ce système");
            _associationButton.IsEnabled = false;
            return;
        }

        var registered = FileAssociationService.IsRegistered;
        var pointsToThisVersion = registered && FileAssociationService.IsRegisteredToCurrentApplication;
        _associationStatus.Text = !registered
            ? "Marginalia n’est pas encore enregistrée dans Ouvrir avec."
            : pointsToThisVersion
                ? "Ouvrir avec lance cette version de Marginalia."
                : "L’association pointe vers une autre version de Marginalia.";
        var label = !registered
            ? "Activer l’association"
            : pointsToThisVersion ? "Désactiver l’association" : "Mettre à jour l’association";
        SetModalButtonLabel(_associationButton, label);
        _associationButton.IsEnabled = FileAssociationService.CanRegisterCurrentApplication || pointsToThisVersion;
    }

    private void WindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ToggleFullScreen();
            e.Handled = true;
            return;
        }

        var control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (!control) return;

        switch (e.Key)
        {
            case Key.O when shift: OpenFolderClick(this, new RoutedEventArgs()); break;
            case Key.O: OpenFileClick(this, new RoutedEventArgs()); break;
            case Key.N: NewDocumentClick(this, new RoutedEventArgs()); break;
            case Key.F: FindInDocumentClick(this, new RoutedEventArgs()); break;
            case Key.H: ReplaceInDocumentClick(this, new RoutedEventArgs()); break;
            case Key.S when shift: SaveAsClick(this, new RoutedEventArgs()); break;
            case Key.S: SaveClick(this, new RoutedEventArgs()); break;
            case Key.W: CloseActiveDocumentClick(this, new RoutedEventArgs()); break;
            case Key.E when shift: ExportPdfClick(this, new RoutedEventArgs()); break;
            case Key.E: FocusEditor(); break;
            case Key.K: SearchBox.Focus(); break;
            case Key.Tab when shift: ActivateAdjacentDocument(-1); break;
            case Key.Tab: ActivateAdjacentDocument(1); break;
            case Key.PageUp when shift: MoveActiveTab(-1); break;
            case Key.PageDown when shift: MoveActiveTab(1); break;
            case Key.F12 when shift: CaptureDiagnosticScreenshot(); break;
            default: return;
        }
        e.Handled = true;
    }

    private void FullScreenClick(object? sender, RoutedEventArgs e) => ToggleFullScreen();

    private void ToggleFullScreen()
    {
        if (WindowState == Avalonia.Controls.WindowState.FullScreen)
        {
            WindowState = _windowStateBeforeFullScreen;
            FullScreenMenuItem.Header = "Plein écran     F11";
            return;
        }

        _windowStateBeforeFullScreen = WindowState;
        WindowState = Avalonia.Controls.WindowState.FullScreen;
        FullScreenMenuItem.Header = "Quitter le plein écran     F11";
    }

    private void EditorPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.Delta.Y == 0) return;

        var fontSize = Math.Clamp(Editor.FontSize + Math.Sign(e.Delta.Y), 8, 32);
        if (fontSize != Editor.FontSize)
        {
            Editor.FontSize = fontSize;
            _settings.EditorFontSize = fontSize;
            PersistSettingsSilently();
            StatusText.Text = $"Taille du texte : {fontSize:0} pt";
        }
        e.Handled = true;
    }

    private void FocusEditor() => Editor.Focus();
    private void FocusSearchClick(object? sender, RoutedEventArgs e) => SearchBox.Focus();
    private void FocusEditorClick(object? sender, RoutedEventArgs e) => FocusEditor();
    private void SelectAllClick(object? sender, RoutedEventArgs e) { Editor.Focus(); Editor.SelectAll(); }

    private void WorkspaceDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void WorkspaceDrop(object? sender, DragEventArgs e)
    {
        var items = e.Data.GetFiles();
        if (items is null)
        {
            e.Handled = true;
            return;
        }

        var remainingCapacity = Math.Max(0, MaximumOpenDocuments - _documents.Count);
        try
        {
            foreach (var item in items)
            {
                using (item)
                {
                    if (remainingCapacity <= 0) continue;
                    remainingCapacity--;
                    await OpenPathAsync(item.Path.LocalPath);
                }
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("drop-files", ex);
            StatusText.Text = $"Impossible d’ouvrir les éléments déposés : {ex.Message}";
        }
        finally { e.Handled = true; }
    }

    private void FindInDocumentClick(object? sender, RoutedEventArgs e) => ShowDocumentSearch(false);
    private void ReplaceInDocumentClick(object? sender, RoutedEventArgs e) => ShowDocumentSearch(true);
    private void CloseFindPanelClick(object? sender, RoutedEventArgs e)
    {
        EditorSearchPanel.IsVisible = false;
        ReplacePanel.IsVisible = false;
        Editor.Focus();
    }

    private void ShowDocumentSearch(bool showReplace)
    {
        EditorSearchPanel.IsVisible = true;
        ReplacePanel.IsVisible = showReplace;
        Dispatcher.UIThread.Post(() =>
        {
            FindTextBox.Focus();
            FindTextBox.SelectAll();
        }, DispatcherPriority.Background);
    }

    private void FindTextChanged(object? sender, TextChangedEventArgs e)
    {
        FindStatusText.Text = "";
    }

    private void FindTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseFindPanelClick(sender, new RoutedEventArgs());
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Enter) return;

        FindMatch(backwards: e.KeyModifiers.HasFlag(KeyModifiers.Shift), keepSearchFocus: true);
        e.Handled = true;
    }

    private void FindNextClick(object? sender, RoutedEventArgs e) => FindMatch(backwards: false);
    private void FindPreviousClick(object? sender, RoutedEventArgs e) => FindMatch(backwards: true);

    private bool FindMatch(bool backwards, bool keepSearchFocus = false)
    {
        var query = FindTextBox.Text ?? "";
        var text = Editor.Text ?? "";
        if (query.Length == 0 || query.Length > text.Length)
        {
            FindStatusText.Text = "Aucun résultat";
            return false;
        }

        var selectionStart = Editor.SelectionStart;
        var currentPosition = Editor.SelectionLength > 0 ? selectionStart : Editor.TextArea.Caret.Offset;
        int match;
        if (backwards)
        {
            var start = currentPosition - 1;
            match = start >= 0 ? text.LastIndexOf(query, start, StringComparison.CurrentCultureIgnoreCase) : -1;
            if (match < 0) match = text.LastIndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        }
        else
        {
            var start = Editor.SelectionLength > 0 ? selectionStart + Editor.SelectionLength : currentPosition;
            match = start < text.Length ? text.IndexOf(query, start, StringComparison.CurrentCultureIgnoreCase) : -1;
            if (match < 0) match = text.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        }

        if (match < 0)
        {
            FindStatusText.Text = "Aucun résultat";
            return false;
        }

        var lineNumber = Editor.Document.GetLineByOffset(match).LineNumber;
        Editor.Select(match, query.Length);
        Editor.ScrollToLine(lineNumber);
        Editor.TextArea.Caret.BringCaretToView();
        Dispatcher.UIThread.Post(() =>
        {
            if (Editor.Document.LineCount < lineNumber) return;
            Editor.ScrollToLine(lineNumber);
            Editor.TextArea.Caret.BringCaretToView();
        }, DispatcherPriority.Loaded);
        if (keepSearchFocus) FindTextBox.Focus();
        else Editor.Focus();
        FindStatusText.Text = $"Ligne {lineNumber}";
        return true;
    }

    private void ReplaceCurrentClick(object? sender, RoutedEventArgs e)
    {
        var query = FindTextBox.Text ?? "";
        if (query.Length == 0) return;
        var text = Editor.Text ?? "";
        var start = Editor.SelectionStart;
        var selectedMatches = Editor.SelectionLength == query.Length
            && string.Compare(text, start, query, 0, query.Length, StringComparison.CurrentCultureIgnoreCase) == 0;
        if (!selectedMatches && !FindMatch(backwards: false)) return;

        start = Editor.SelectionStart;
        var replacement = ReplaceTextBox.Text ?? "";
        Editor.Document.Replace(start, Editor.SelectionLength, replacement);
        Editor.Select(start, replacement.Length);
        Editor.Focus();
        FindStatusText.Text = "Occurrence remplacée";
    }

    private void ReplaceAllClick(object? sender, RoutedEventArgs e)
    {
        const int maximumReplacements = 10_000;
        var query = FindTextBox.Text ?? "";
        if (query.Length == 0) return;
        var text = Editor.Text ?? "";
        var matches = new List<int>();
        var offset = 0;
        while (offset <= text.Length - query.Length)
        {
            var match = text.IndexOf(query, offset, StringComparison.CurrentCultureIgnoreCase);
            if (match < 0) break;
            if (matches.Count == maximumReplacements)
            {
                FindStatusText.Text = $"Plus de {maximumReplacements:N0} résultats; précisez votre recherche.";
                return;
            }
            matches.Add(match);
            offset = match + query.Length;
        }

        if (matches.Count == 0)
        {
            FindStatusText.Text = "Aucun résultat";
            return;
        }

        var replacement = ReplaceTextBox.Text ?? "";
        var document = Editor.Document;
        document.BeginUpdate();
        try
        {
            for (var index = matches.Count - 1; index >= 0; index--)
                document.Replace(matches[index], query.Length, replacement);
        }
        finally { document.EndUpdate(); }
        FindStatusText.Text = $"{matches.Count:N0} occurrence{(matches.Count == 1 ? "" : "s")} remplacée{(matches.Count == 1 ? "" : "s")}";
        Editor.Focus();
    }

    private void PreviousDocumentClick(object? sender, RoutedEventArgs e) => ActivateAdjacentDocument(-1);
    private void NextDocumentClick(object? sender, RoutedEventArgs e) => ActivateAdjacentDocument(1);
    private void QuitClick(object? sender, RoutedEventArgs e) => Close();

    private void CaptureDiagnosticScreenshot()
    {
        if (!AppDiagnostics.DebugEnabled)
        {
            StatusText.Text = "Lancez Marginalia avec --debug pour activer les captures de diagnostic.";
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (AppDiagnostics.DebugMenuOpen)
                {
                    FileMenuItem.IsSelected = true;
                    FileMenuItem.IsSubMenuOpen = true;
                }
                var path = AppDiagnostics.CreateScreenshotPath();
                var scale = RenderScaling;
                var pixelSize = new Avalonia.PixelSize(
                    Math.Max(1, (int)Math.Ceiling(ClientSize.Width * scale)),
                    Math.Max(1, (int)Math.Ceiling(ClientSize.Height * scale)));
                var previewText = PreviewContent.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
                var fileText = FileList.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
                AppDiagnostics.Debug("ui-state",
                    ("window-enabled", IsEffectivelyEnabled.ToString()),
                    ("preview-enabled", PreviewContent.IsEffectivelyEnabled.ToString()),
                    ("preview-foreground", DescribeBrush(previewText?.Foreground)),
                    ("preview-opacity", previewText?.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing"),
                    ("preview-weight", previewText?.FontWeight.ToString() ?? "missing"),
                    ("library-enabled", FileList.IsEffectivelyEnabled.ToString()),
                    ("library-foreground", DescribeBrush(fileText?.Foreground)),
                    ("library-opacity", fileText?.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing"));
                using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize, new Avalonia.Vector(96 * scale, 96 * scale));
                bitmap.Render(this);
                bitmap.Save(path);
                AppDiagnostics.Debug("ui-capture", ("source", "render-target"), ("width", pixelSize.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("height", pixelSize.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                StatusText.Text = $"Capture de diagnostic enregistrée : {path}";
                if (AppDiagnostics.DebugMenuOpen) CaptureDebugMenuScreenshot();
            }
            catch (Exception ex)
            {
                AppDiagnostics.Error("ui-capture", ex);
                StatusText.Text = "Impossible de créer la capture de diagnostic.";
            }
        }, DispatcherPriority.Render);
    }

    private static string DescribeBrush(Avalonia.Media.IBrush? brush) =>
        brush is Avalonia.Media.ISolidColorBrush solid ? solid.Color.ToString() : brush?.GetType().Name ?? "missing";

    private void CaptureDebugMenuScreenshot()
    {
        var popup = FileMenuItem.GetVisualDescendants().OfType<Popup>().FirstOrDefault(candidate => candidate.IsOpen);
        var menuContent = popup?.Child;
        if (menuContent is null || menuContent.Bounds.Width <= 0 || menuContent.Bounds.Height <= 0)
        {
            AppDiagnostics.Debug("ui-menu-capture", ("phase", "popup-not-ready"));
            return;
        }

        try
        {
            ApplyMenuContrast(FileMenuItem);
            var menuItems = menuContent.GetLogicalDescendants().OfType<MenuItem>()
                .Concat(menuContent.GetVisualDescendants().OfType<MenuItem>())
                .Distinct()
                .ToArray();
            var firstItem = menuItems.FirstOrDefault();
            var textVisuals = menuContent.GetVisualDescendants()
                .Where(visual => visual is TextBlock or AccessText)
                .Take(12)
                .Select(visual => $"{visual.GetType().Name}:{DescribeBrush((visual as TextBlock)?.Foreground)}:o={(visual as Control)?.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}:e={(visual as Control)?.IsEffectivelyEnabled}");
            AppDiagnostics.Debug("ui-menu-state",
                ("open", (popup?.IsOpen ?? false).ToString()),
                ("items", menuItems.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("enabled", FileMenuItem.IsEffectivelyEnabled.ToString()),
                ("foreground", DescribeBrush(firstItem?.Foreground)),
                ("header-type", firstItem?.Header?.GetType().Name ?? "missing"),
                ("header-foreground", DescribeBrush((firstItem?.Header as TextBlock)?.Foreground)),
                ("header-opacity", (firstItem?.Header as Control)?.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing"),
                ("text-visuals", string.Join(",", textVisuals)),
                ("item-enabled", firstItem?.IsEffectivelyEnabled.ToString() ?? "missing"),
                ("header-presenter", DescribeBrush(firstItem?.FindControl<ContentPresenter>("PART_HeaderPresenter")?.Foreground)),
                ("popup-enabled", menuContent.IsEffectivelyEnabled.ToString()),
                ("popup-opacity", menuContent.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ("popup-background", DescribeBrush((menuContent as Border)?.Background)));
            var scale = RenderScaling;
            var pixelSize = new Avalonia.PixelSize(
                Math.Max(1, (int)Math.Ceiling(menuContent.Bounds.Width * scale)),
                Math.Max(1, (int)Math.Ceiling(menuContent.Bounds.Height * scale)));
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize, new Avalonia.Vector(96 * scale, 96 * scale));
            bitmap.Render(menuContent);
            var path = AppDiagnostics.CreateScreenshotPath("menu");
            bitmap.Save(path);
            AppDiagnostics.Debug("ui-menu-capture", ("phase", "saved"), ("width", pixelSize.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)), ("height", pixelSize.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            StatusText.Text = $"Capture du menu enregistrée : {path}";
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("ui-menu-capture", ex);
        }
    }

    private void ApplyMenuContrast(MenuItem menuItem)
    {
        menuItem.Foreground = AppTheme.TextBrush;
        menuItem.Background = AppTheme.SurfaceBrush;
        if (_contrastStyledMenuItems.Add(menuItem))
        {
            menuItem.PointerEntered += (_, _) => SetMenuItemContrast(menuItem, hovered: true);
            menuItem.PointerExited += (_, _) => SetMenuItemContrast(menuItem, hovered: false);
            menuItem.SubmenuOpened += (_, _) => Dispatcher.UIThread.Post(() => ApplyMenuContrast(menuItem), DispatcherPriority.Render);
        }
        SetMenuItemContrast(menuItem, menuItem.IsSelected);

        var popup = menuItem.GetVisualDescendants().OfType<Popup>().FirstOrDefault(candidate => candidate.IsOpen);
        if (popup?.Child is not { } menuContent) return;
        if (menuContent is Border popupBorder)
        {
            popupBorder.Background = AppTheme.SurfaceBrush;
            popupBorder.BorderBrush = AppTheme.BorderBrush;
        }
        var items = menuContent.GetLogicalDescendants().OfType<MenuItem>()
            .Concat(menuContent.GetVisualDescendants().OfType<MenuItem>())
            .Distinct()
            .ToArray();
        foreach (var childMenuItem in items) SetMenuItemContrast(childMenuItem, childMenuItem.IsSelected);
        foreach (var descendant in menuContent.GetVisualDescendants())
        {
            if (descendant is ContentPresenter { Name: "PART_HeaderPresenter" } header)
                header.Foreground = AppTheme.TextBrush;
            if (descendant is TextBlock { Name: "PART_InputGestureText" } gesture)
                gesture.Foreground = AppTheme.SubtleTextBrush;
            if (descendant is Border { Name: "PART_LayoutRoot" } itemBackground)
                itemBackground.Background = AppTheme.SurfaceBrush;
        }
    }

    private void SetMenuItemContrast(MenuItem menuItem, bool hovered)
    {
        var foreground = AppTheme.TextBrush;
        var background = hovered ? AppTheme.HoverBrush : AppTheme.SurfaceBrush;
        if (menuItem.Header is string headerText)
        {
            menuItem.Header = new AccessText
            {
                Text = headerText,
                Foreground = AppTheme.IsDark ? foreground : Avalonia.Media.Brushes.Black,
                FontSize = 15,
                FontWeight = Avalonia.Media.FontWeight.Bold
            };
        }
        else if (menuItem.Header is AccessText accessText)
        {
            accessText.Foreground = foreground;
        }
        menuItem.Foreground = foreground;
        menuItem.Background = background;
        menuItem.ApplyTemplate();
        if (menuItem.FindControl<ContentPresenter>("PART_HeaderPresenter") is { } headerPresenter)
            headerPresenter.Foreground = foreground;
        if (menuItem.FindControl<TextBlock>("PART_InputGestureText") is { } gestureText)
            gestureText.Foreground = AppTheme.SubtleTextBrush;
        if (menuItem.FindControl<Border>("PART_LayoutRoot") is { } rootBorder)
            rootBorder.Background = background;
        foreach (var descendant in menuItem.GetVisualDescendants())
        {
            if (descendant is ContentPresenter { Name: "PART_HeaderPresenter" } header)
                header.Foreground = foreground;
            if (descendant is TextBlock { Name: "PART_InputGestureText" } gesture)
                gesture.Foreground = AppTheme.SubtleTextBrush;
            if (descendant is Border { Name: "PART_LayoutRoot" } itemBackground)
                itemBackground.Background = background;
        }
    }

    private async Task CaptureAfterStartupAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(15));
        CaptureDiagnosticScreenshot();
    }

    private void ActivateAdjacentDocument(int delta)
    {
        if (_documents.Count < 2) return;
        var index = _active is null ? 0 : _documents.IndexOf(_active);
        var next = (index + delta + _documents.Count) % _documents.Count;
        _ = ActivateAsync(_documents[next]);
    }

    private void ShortcutsClick(object? sender, RoutedEventArgs e)
    {
        if (_shortcutsWindow is { IsVisible: true })
        {
            _shortcutsWindow.Activate();
            return;
        }

        var closeButton = CreateModalButton("Fermer");
        closeButton.IsDefault = true;
        var shortcuts = new (string Gesture, string Description)[]
        {
            ("Ctrl+O", "Ouvrir un fichier"),
            ("Ctrl+N", "Nouveau document"),
            ("Ctrl+Maj+O", "Ouvrir un dossier"),
            ("Ctrl+S", "Enregistrer"),
            ("Ctrl+Maj+S", "Enregistrer sous"),
            ("Ctrl+W", "Fermer l’onglet"),
            ("Ctrl+Maj+E", "Exporter en PDF"),
            ("F11", "Basculer en plein écran"),
            ("Ctrl+molette", "Agrandir/réduire le texte de l’éditeur"),
            ("Ctrl+Tab", "Document suivant"),
            ("Ctrl+Maj+Tab", "Document précédent"),
            ("Ctrl+Maj+Page ↑ / Page ↓", "Réordonner l’onglet"),
            ("Ctrl+K", "Aller à la recherche"),
            ("Ctrl+E", "Aller à l’éditeur"),
            ("Ctrl+Maj+F12", "Capture de diagnostic (avec --debug)"),
            ("Ctrl+A", "Tout sélectionner dans l’éditeur")
        };
        var shortcutsGrid = new Grid();
        shortcutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        shortcutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var index = 0; index < shortcuts.Length; index++)
        {
            shortcutsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var gesture = new TextBlock
            {
                Text = shortcuts[index].Gesture,
                Margin = new Avalonia.Thickness(0, 0, 16, 7),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap
            };
            var description = new TextBlock
            {
                Text = shortcuts[index].Description,
                Margin = new Avalonia.Thickness(0, 0, 0, 7),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
            Grid.SetRow(gesture, index);
            Grid.SetColumn(gesture, 0);
            Grid.SetRow(description, index);
            Grid.SetColumn(description, 1);
            shortcutsGrid.Children.Add(gesture);
            shortcutsGrid.Children.Add(description);
        }
        var body = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Raccourcis clavier", FontSize = 20, FontWeight = Avalonia.Media.FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 0, 0, 6) },
                shortcutsGrid
            }
        };
        var dialog = new Window
        {
            Title = "Raccourcis clavier",
            Width = 520,
            Height = Math.Clamp(ClientSize.Height * 0.8, 360, 640),
            MinWidth = 460,
            MinHeight = 300,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = CreateModalContent(body, closeButton)
        };
        ApplyModalWindowPalette(dialog);
        _shortcutsWindow = dialog;
        closeButton.Click += (_, _) => dialog.Close();
        dialog.Closed += (_, _) =>
        {
            if (ReferenceEquals(_shortcutsWindow, dialog)) _shortcutsWindow = null;
        };
        dialog.Show(this);
    }

    private void OpenDebugModal(string modal)
    {
        switch (modal)
        {
            case "about": AboutClick(this, new RoutedEventArgs()); break;
            case "shortcuts": ShortcutsClick(this, new RoutedEventArgs()); break;
            case "settings": SettingsClick(this, new RoutedEventArgs()); break;
        }
    }

    private async void AboutClick(object? sender, RoutedEventArgs e)
    {
        using var iconStream = AssetLoader.Open(new Uri("avares://Marginalia/Assets/marginalia.ico"));
        using var icon = new Avalonia.Media.Imaging.Bitmap(iconStream);
        var closeButton = CreateModalButton("Fermer");
        closeButton.IsDefault = true;
        closeButton.Click += (_, _) => { if (TopLevel.GetTopLevel(closeButton) is Window window) window.Close(); };
        var body = new StackPanel
        {
            Margin = new Avalonia.Thickness(28),
            Spacing = 10,
            Children =
            {
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 14,
                    Children =
                    {
                        new Image { Source = icon, Width = 56, Height = 56 },
                        new StackPanel
                        {
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            Spacing = 3,
                            Children =
                            {
                                new TextBlock { Text = "Marginalia", FontSize = 25, FontWeight = Avalonia.Media.FontWeight.SemiBold },
                                 new TextBlock { Text = $"Version {ProductInfo.Version}", FontSize = 14, Foreground = AppTheme.MutedTextBrush }
                            }
                        }
                    }
                },
                new TextBlock
                {
                    Text = "Un espace de travail multiplateforme pour lire, écrire et exporter vos documents Markdown.",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 4, 0, 8)
                }
            }
        };
        var dialog = new Window
        {
            Title = "À propos de Marginalia",
            Width = 380,
            SizeToContent = Avalonia.Controls.SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = CreateModalContent(body, closeButton)
        };
        ApplyModalWindowPalette(dialog);
        await dialog.ShowDialog(this);
    }

    private void FileSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (!_tabRebuild && !_suppressFileSelection && FileList.SelectedItem is DocumentTab doc) _ = ActivateAsync(doc);
    }
    private void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshFileList();
    }

    private void RefreshFileList()
    {
        var query = SearchBox.Text?.Trim() ?? "";
        _suppressFileSelection = true;
        try
        {
            IEnumerable<DocumentTab> filtered = string.IsNullOrEmpty(query)
                ? _documents
                : _documents.Where(x => x.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) || x.Path.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
            FileList.ItemsSource = filtered;
            FileList.SelectedItem = _active is not null && (string.IsNullOrEmpty(query) || filtered.Contains(_active)) ? _active : null;
        }
        finally { _suppressFileSelection = false; }
    }
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        if (_active is not null) await SaveDocumentAsync(_active);
    }

    private async void SaveAsClick(object? sender, RoutedEventArgs e)
    {
        var document = _active;
        if (document is null)
        {
            StatusText.Text = "Ouvrez un document avant d’utiliser Enregistrer sous.";
            return;
        }
        await SaveAsDocumentAsync(document);
    }

    private async Task<bool> SaveAsDocumentAsync(DocumentTab document)
    {
        try
        {
            var extension = Path.GetExtension(document.Path);
            if (extension is not ".md" and not ".markdown") extension = ".md";
            var output = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Enregistrer le document sous",
                SuggestedFileName = document.IsUntitled ? $"{document.Title}.md" : Path.GetFileName(document.Path),
                DefaultExtension = extension.TrimStart('.'),
                ShowOverwritePrompt = true,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Markdown") { Patterns = new[] { "*.md", "*.markdown" } }
                }
            });
            if (output is null) return false;
            using (output)
            {
                var targetPath = output.Path.LocalPath;
                var targetExtension = Path.GetExtension(targetPath);
                if (!targetExtension.Equals(".md", StringComparison.OrdinalIgnoreCase)
                    && !targetExtension.Equals(".markdown", StringComparison.OrdinalIgnoreCase))
                {
                    targetPath = Path.ChangeExtension(targetPath, ".md");
                }
                targetPath = Path.GetFullPath(targetPath);

                var pathComparer = FilePathComparer;
                if (!document.IsUntitled && pathComparer.Equals(Path.GetFullPath(document.Path), targetPath))
                {
                    return await SaveDocumentAsync(document);
                }
                if (_documents.Any(openDocument => openDocument != document && !openDocument.IsUntitled && pathComparer.Equals(Path.GetFullPath(openDocument.Path), targetPath)))
                {
                    StatusText.Text = "Ce fichier est déjà ouvert dans un autre onglet.";
                    return false;
                }

                await document.WriteToFileAtomicallyAsync(targetPath);
                document.MoveToPath(targetPath);
                document.MarkSaved();
                _settings.LastDocumentPath = targetPath;
                RebuildTabs();
                RefreshFileList();
                UpdateStatusBar();
                RecordRecentFiles([targetPath]);
                if (_active == document) Title = $"Marginalia — {document.Title}";
                StatusText.Text = $"Enregistré sous {document.Title} · {DateTime.Now:t}";
                return true;
            }
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("save-as", ex);
            StatusText.Text = $"Échec de l’enregistrement sous : {ex.Message}";
            return false;
        }
    }

    private async Task<bool> SaveDocumentAsync(DocumentTab document)
    {
        if (document.IsUntitled) return await SaveAsDocumentAsync(document);

        try
        {
            await document.WriteToFileAtomicallyAsync(document.Path);
            document.MarkSaved();
            RebuildTabs();
            if (_active == document) Title = $"Marginalia — {document.Title}";
            StatusText.Text = $"Enregistré · {DateTime.Now:t}";
            return true;
        }
        catch (Exception ex)
        {
            AppDiagnostics.Error("save", ex);
            StatusText.Text = $"Échec de l’enregistrement : {ex.Message}";
            return false;
        }
    }

    private async void CloseTabClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DocumentTab document }) await CloseDocumentAsync(document);
    }

    private async void CloseActiveDocumentClick(object? sender, RoutedEventArgs e)
    {
        if (_active is not null) await CloseDocumentAsync(_active);
    }

    private async void CloseDocumentFromLibraryClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: DocumentTab document }) await CloseDocumentAsync(document);
    }

    private async Task CloseDocumentAsync(DocumentTab document)
    {
        if (!_documents.Contains(document)) return;
        if (document.Dirty)
        {
            var choice = await ShowUnsavedChangesDialog(1);
            if (choice == 0) return;
            if (choice == 1 && !await SaveDocumentAsync(document)) return;
        }

        var index = _documents.IndexOf(document);
        var wasActive = _active == document;
        _suppressFileSelection = true;
        try
        {
            _documents.Remove(document);
            FileList.SelectedItem = null;
        }
        finally { _suppressFileSelection = false; }
        RefreshFileList();

        if (!wasActive)
        {
            RebuildTabs();
            return;
        }
        if (_documents.Count > 0)
        {
            var nextDocument = _documents[Math.Min(index, _documents.Count - 1)];
            if (!await ActivateAsync(nextDocument))
            {
                _active = null;
                ShowWelcome();
            }
            return;
        }

        _active = null;
        RebuildTabs();
        ShowWelcome();
    }
    private async void ExportPdfClick(object? sender, RoutedEventArgs e)
    {
        if (_active is null) { StatusText.Text = "Ouvrez un document avant l’export PDF."; return; }
        var output = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Exporter en PDF", SuggestedFileName = Path.GetFileNameWithoutExtension(_active.Path) + ".pdf", FileTypeChoices = new[] { new FilePickerFileType("Document PDF") { Patterns = new[] { "*.pdf" } } } });
        if (output is null) return;
        try
        {
            var content = _active.Text;
            var title = Path.GetFileName(_active.Path);
            await Task.Run(() => MarkdownPdfExporter.Export(content, title, output.Path.LocalPath));
            StatusText.Text = "PDF exporté.";
        }
        catch (Exception ex) { AppDiagnostics.Error("pdf-export", ex); StatusText.Text = $"Échec de l’export PDF : {ex.Message}"; }
    }

    private void MoveTabLeftClick(object? sender, RoutedEventArgs e) => MoveActiveTab(-1);
    private void MoveTabRightClick(object? sender, RoutedEventArgs e) => MoveActiveTab(1);
    private void MoveActiveTab(int delta)
    {
        if (_active is null) return;
        var from = _documents.IndexOf(_active);
        var to = from + delta;
        if (to < 0 || to >= _documents.Count) return;
        _documents.Move(from, to);
        RebuildTabs();
    }

    private async void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _closePromptOpen) return;
        var unsaved = _documents.Where(document => document.Dirty).ToArray();
        if (unsaved.Length == 0) return;

        e.Cancel = true;
        _closePromptOpen = true;
        try
        {
            var choice = await ShowUnsavedChangesDialog(unsaved.Length);
            if (choice == 0) return;
            if (choice == 1)
            {
                foreach (var document in unsaved)
                {
                    if (!await SaveDocumentAsync(document))
                    {
                        return;
                    }
                }
                RebuildTabs();
            }

            _allowClose = true;
            Close();
        }
        finally
        {
            _closePromptOpen = false;
        }
    }

    private async Task<int> ShowUnsavedChangesDialog(int count)
    {
        var dialog = new Window
        {
            Title = "Modifications non enregistrées",
            Width = 420,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        ApplyModalWindowPalette(dialog);
        dialog.Content = CreateModalContent(new StackPanel
        {
            Margin = new Avalonia.Thickness(22),
            Spacing = 18,
            Children =
            {
                new TextBlock
                {
                    Text = count == 1
                        ? "Ce document contient des modifications non enregistrées. Voulez-vous les enregistrer avant de le fermer ?"
                        : $"Ces {count} documents contiennent des modifications non enregistrées. Voulez-vous les enregistrer avant de les fermer ?",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    FontSize = 14
                }
            }
        }, DialogButton("Annuler", 0), DialogButton("Abandonner", 2), DialogButton("Enregistrer", 1));
        return await dialog.ShowDialog<int>(this);

        Button DialogButton(string label, int result)
        {
            var button = CreateModalButton(label, result switch
            {
                1 => "modal-primary",
                2 => "modal-destructive",
                _ => null
            });
            if (result == 0) button.IsCancel = true;
            if (result == 1) button.IsDefault = true;
            button.Click += (_, _) => dialog.Close(result);
            return button;
        }
    }

    private static Button CreateModalButton(string label, string? variant = null)
    {
        var labelForeground = variant switch
        {
            "modal-primary" => AppTheme.OnAccentBrush,
            "modal-destructive" => AppTheme.OnDangerBrush,
            _ => AppTheme.TextBrush
        };
        var button = new Button
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Foreground = labelForeground,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        SetModalButtonLabel(button, label, labelForeground);
        button.Classes.Add("modal-button");
        if (variant is not null) button.Classes.Add(variant);
        button.PropertyChanged += (_, args) =>
        {
            if (args.Property == InputElement.IsPointerOverProperty)
                SetModalButtonVisualState(button, button.IsPointerOver);
        };
        SetModalButtonVisualState(button, hovered: false);
        return button;
    }

    private static void SetModalButtonLabel(Button button, string label, Avalonia.Media.IBrush? foreground = null)
    {
        button.Content = new TextBlock
        {
            Text = label,
            Foreground = foreground ?? AppTheme.TextBrush,
            FontSize = 15,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
    }

    private static void SetModalButtonVisualState(Button button, bool hovered)
    {
        if (!button.IsEnabled) return;

        var isPrimary = button.Classes.Contains("modal-primary");
        var isDestructive = button.Classes.Contains("modal-destructive");
        var foreground = isPrimary
            ? AppTheme.OnAccentBrush
            : isDestructive ? AppTheme.OnDangerBrush : AppTheme.TextBrush;
        var background = isPrimary
            ? hovered ? AppTheme.AccentHoverBrush : AppTheme.AccentBrush
            : isDestructive
                ? hovered ? AppTheme.DangerHoverBrush : AppTheme.DangerBrush
                : hovered ? AppTheme.HoverBrush : AppTheme.SurfaceBrush;
        var border = isPrimary || isDestructive
            ? background
            : hovered ? AppTheme.AccentBrush : AppTheme.BorderBrush;

        button.Background = background;
        button.BorderBrush = border;
        button.Foreground = foreground;
        if (button.Content is TextBlock label) label.Foreground = foreground;
    }

    private static Control CreateModalContent(Control body, params Button[] buttons)
    {
        var footerGrid = new Grid();
        for (var index = 0; index < buttons.Length; index++)
        {
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttons[index].HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            buttons[index].Margin = new Avalonia.Thickness(index == 0 ? 0 : 5, 0, index == buttons.Length - 1 ? 0 : 5, 0);
            Grid.SetColumn(buttons[index], index);
            footerGrid.Children.Add(buttons[index]);
        }

        var footer = new Border
        {
            Background = AppTheme.SurfaceBrush,
            BorderBrush = AppTheme.BorderBrush,
            BorderThickness = new Avalonia.Thickness(0, 1, 0, 0),
            Padding = new Avalonia.Thickness(16, 12),
            Child = footerGrid
        };
        var layout = new DockPanel();
        DockPanel.SetDock(footer, Dock.Bottom);
        layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer
        {
            Content = body,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        });
        return new Border { Background = AppTheme.SurfaceBrush, Child = layout };
    }

    private static void ApplyModalWindowPalette(Window window)
    {
        window.Background = AppTheme.SurfaceBrush;
        window.Foreground = AppTheme.TextBrush;
        window.RequestedThemeVariant = AppTheme.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        window.Opened += async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var bodyTextBlocks = window.GetVisualDescendants()
                    .OfType<TextBlock>()
                    .Where(textBlock => !textBlock.GetVisualAncestors().OfType<Button>().Any())
                    .ToArray();
                foreach (var textBlock in bodyTextBlocks)
                {
                    textBlock.Foreground = AppTheme.TextBrush;
                    textBlock.Opacity = 1;
                    textBlock.FontSize = Math.Max(textBlock.FontSize, 16);
                    textBlock.FontWeight = Avalonia.Media.FontWeight.SemiBold;
                }
                foreach (var checkBox in window.GetVisualDescendants().OfType<CheckBox>())
                {
                    checkBox.Foreground = AppTheme.TextBrush;
                    checkBox.FontSize = 16;
                    checkBox.FontWeight = Avalonia.Media.FontWeight.SemiBold;
                    if (checkBox.GetVisualDescendants().OfType<ContentPresenter>()
                        .FirstOrDefault(presenter => presenter.Name == "PART_ContentPresenter") is { } contentPresenter)
                    {
                        contentPresenter.Foreground = AppTheme.TextBrush;
                        contentPresenter.FontSize = 16;
                        contentPresenter.FontWeight = Avalonia.Media.FontWeight.SemiBold;
                    }
                }
                AppDiagnostics.Debug("modal-contrast-applied",
                    ("title", window.Title ?? string.Empty),
                    ("body-text-count", bodyTextBlocks.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    ("foreground", DescribeBrush(AppTheme.TextBrush)),
                    ("theme", window.RequestedThemeVariant?.Key?.ToString() ?? "inherit"));
                for (var index = 0; index < bodyTextBlocks.Length; index++)
                {
                    var textBlock = bodyTextBlocks[index];
                    var ancestors = string.Join(",", textBlock.GetVisualAncestors().OfType<Control>()
                        .Select(control => $"{control.GetType().Name}[opacity={control.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)},enabled={control.IsEffectivelyEnabled}]"));
                    AppDiagnostics.Debug("modal-text-state",
                        ("index", index.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        ("foreground", DescribeBrush(textBlock.Foreground)),
                        ("opacity", textBlock.Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        ("enabled", textBlock.IsEffectivelyEnabled.ToString()),
                        ("ancestors", ancestors));
                }
            }, DispatcherPriority.Render);

            // Let Avalonia commit the changed visual tree before creating the diagnostic image.
            await Task.Delay(100);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (AppDiagnostics.DebugModal is null || !window.IsVisible) return;
                window.UpdateLayout();
                if (AppDiagnostics.DebugModal == "settings")
                    window.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()?.ScrollToEnd();
                if (AppDiagnostics.DebugModalHover)
                {
                    var buttonToHover = window.GetVisualDescendants().OfType<Button>()
                        .FirstOrDefault(button => button.Content is TextBlock labelBlock
                            && labelBlock.Text is { } text
                            && (text.StartsWith("Mettre à jour", StringComparison.Ordinal)
                                || text.StartsWith("Désactiver l’association", StringComparison.Ordinal)))
                        ?? window.GetVisualDescendants().OfType<Button>().FirstOrDefault(button => button.Classes.Contains("modal-primary"));
                    if (buttonToHover is not null) SetModalButtonVisualState(buttonToHover, hovered: true);
                }
                window.UpdateLayout();
                var pixelSize = new Avalonia.PixelSize(
                    Math.Max(1, (int)Math.Ceiling(window.ClientSize.Width)),
                    Math.Max(1, (int)Math.Ceiling(window.ClientSize.Height)));
                using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize, new Avalonia.Vector(96, 96));
                bitmap.Render(window);
                var path = AppDiagnostics.CreateScreenshotPath("modal");
                bitmap.Save(path);
                AppDiagnostics.Debug("modal-capture", ("path", path), ("width", pixelSize.Width.ToString()), ("height", pixelSize.Height.ToString()));
            }, DispatcherPriority.Render);
        };
    }
}
