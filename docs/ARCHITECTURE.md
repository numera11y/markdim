# Architecture

## Current components

- `MarkdownDesk`: Avalonia desktop app, lifecycle management, native dialogs, editing, preview, and PDF generation.
- `DocumentTab`: observable model for an open document and its modified state.
- `AppDiagnostics`: bounded local log (1 MB plus one rotated file), limited to timestamps, operations, and exception types.
- `UserSettingsStore`: local JSON UI preferences, written atomically and normalized to valid values.
- `FileAssociationService`: per-user registration/removal in the Windows Registry; it does not change the protected system default-app choice.
- `AppTheme`: semantic light/dark palette exposed through dynamic Avalonia resources.
- `MarkdownHighlighting`: lightweight XSHD definitions for source Markdown highlighting without an external service.
- `MarkdownPreviewRenderer`: Markdig CommonMark/GFM pipeline and Avalonia controls for structured HTML, nested lists, tables, images, and code blocks.
- `Localization`: embedded JSON catalogs for English and French, with system-language selection and fallback behavior.
- `MarkdownDesk.Tests`: unit tests independent of the UI.
- `packaging/`: metadata and scripts for OS-level file association integration and release packages.

## Files and memory

Documents stay at their existing locations and are never copied to a remote service. Folder tabs are indexed without loading every file: content is read on first activation and then retained in memory so edits survive tab switches. Preview rendering is debounced by 180 ms after the last keystroke; above 500,000 characters it pauses without disabling the editor. Rendering errors are isolated from navigation. Filtering and preview do not create persistent temporary files.

The application log is stored at `%LOCALAPPDATA%/markdim/logs/application.log` (Windows), `~/Library/Application Support/markdim/logs/application.log` (macOS), or `~/.local/share/markdim/logs/application.log` (Linux, via .NET). Rotation keeps the current log and one `.1` backup. Document text and paths are not logged. Runtime telemetry and general network access are not added; remote images referenced by a document are fetched over HTTPS for preview rendering.

User preferences are stored separately in `%LOCALAPPDATA%/markdim/settings.json` on Windows, `~/Library/Application Support/markdim/settings.json` on macOS, or `~/.local/share/markdim/settings.json` on Linux. Settings are written atomically. Theme and language choices do not modify Markdown files.

## Dependencies and audit

Direct NuGet versions are explicit in the project files and resolved graphs are locked. NuGet restore audits known advisories; CI must fail if a vulnerable dependency is introduced. `Tmds.DBus.Protocol` is pinned to a fixed version (`0.21.3`) to avoid the vulnerable version previously pulled transitively by Avalonia.

## Distribution

Publishing is self-contained per runtime identifier. Configured targets are `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`, and `linux-arm64`. The beta provides a Windows Setup installer and ZIP, Debian/Ubuntu `.deb` packages, and portable Linux TAR.GZ archives. The Windows beta installer is unsigned. macOS releases require signing and notarization for distribution without Gatekeeper warnings. Linux distributions may require system graphics libraries.
