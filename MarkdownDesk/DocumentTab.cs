using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MarkdownDesk;

public sealed class DocumentTab : INotifyPropertyChanged
{
    public const long MaximumFileBytes = 16 * 1024 * 1024;
    private string? _text;
    private string _title;
    private bool _dirty;
    private Encoding _encoding = new UTF8Encoding(false);
    private string _lineEnding = Environment.NewLine;
    public DocumentTab(string path) : this(path, null) { }

    public DocumentTab(string path, string? text)
    {
        Path = path;
        _text = text;
        _title = System.IO.Path.GetFileName(path);
    }
    private DocumentTab(string path, string? text, string title)
    {
        Path = path;
        _text = text;
        _title = title;
    }
    public static DocumentTab CreateUntitled(string title) => new("", "", title);
    public bool IsUntitled => string.IsNullOrEmpty(Path);
    public string LocationLabel => IsUntitled ? "Nouveau document" : Path;
    public string Path { get; private set; }
    public string Text
    {
        get => _text ?? "";
        set
        {
            if (_text == value) return;
            _text = value;
            Dirty = true;
            Changed();
            Changed(nameof(Text));
        }
    }
    public Encoding TextEncoding => _encoding;
    public bool IsLoaded => _text is not null;

    public async Task<string> LoadTextAsync(CancellationToken cancellationToken = default)
    {
        if (_text is not null || IsUntitled) return _text ?? "";

        await using var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > MaximumFileBytes)
            throw new InvalidDataException($"Ce fichier dépasse la limite de {MaximumFileBytes / (1024 * 1024)} Mo prise en charge par l’éditeur.");
        using var buffer = new MemoryStream((int)stream.Length);
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > MaximumFileBytes)
                throw new InvalidDataException($"Ce fichier dépasse la limite de {MaximumFileBytes / (1024 * 1024)} Mo prise en charge par l’éditeur.");
            buffer.Write(chunk, 0, read);
        }
        var bytes = buffer.GetBuffer();
        var byteLength = (int)buffer.Length;
        var (encoding, preambleLength) = DetectEncoding(bytes.AsSpan(0, byteLength));
        var source = encoding.GetString(bytes, preambleLength, byteLength - preambleLength);
        _encoding = encoding;
        _lineEnding = DetectLineEnding(source);
        _text = NormalizeLineEndings(source);
        Changed(nameof(Text));
        return _text;
    }

    public string GetTextForSaving() => NormalizeLineEndings(Text).Replace("\n", _lineEnding, StringComparison.Ordinal);

    public async Task WriteToFileAtomicallyAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        var fullPath = System.IO.Path.GetFullPath(destinationPath);
        var directory = System.IO.Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The destination path must include a directory.", nameof(destinationPath));
        var temporaryPath = System.IO.Path.Combine(directory, $".markdim-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, GetTextForSaving(), _encoding, cancellationToken);
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public string Title { get => _title; }
    public bool Dirty { get => _dirty; private set { _dirty = value; Changed(); Changed(nameof(TabLabel)); } }
    public string TabLabel => (Dirty ? "● " : "") + Title;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public void MoveToPath(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        if (string.Equals(Path, fullPath, StringComparison.Ordinal)) return;
        Path = fullPath;
        _title = System.IO.Path.GetFileName(fullPath);
        Changed(nameof(Path));
        Changed(nameof(Title));
        Changed(nameof(LocationLabel));
        Changed(nameof(TabLabel));
    }
    public void MarkSaved() { Dirty = false; }

    private static (Encoding Encoding, int PreambleLength) DetectEncoding(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
            return (new UTF32Encoding(true, true, false), 4);
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
            return (new UTF32Encoding(false, true, false), 4);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (new UTF8Encoding(true, false), 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return (new UnicodeEncoding(true, true, false), 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (new UnicodeEncoding(false, true, false), 2);
        return (new UTF8Encoding(false, false), 0);
    }

    private static string DetectLineEnding(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\r') return index + 1 < text.Length && text[index + 1] == '\n' ? "\r\n" : "\r";
            if (text[index] == '\n') return "\n";
        }
        return Environment.NewLine;
    }

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
