# Changelog

This file follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed
- Adds matching English Store poster and hero artwork alongside the French graphics; the artwork script now generates both locales from their corresponding app captures.

## [0.3.0-beta.7] - 2026-10-27

### Changed
- Localizes the app's user-facing workflows in English and French, including menus, dialogs, status messages, errors, previews, and export actions.
- Replaces the short French README with a detailed English project guide, badges, installation instructions, feature overview, and shortcuts.
- Translates the remaining repository documentation to English and updates the Windows Store package description.

### Added
- Adds embedded English and French JSON translation catalogs and documents the community contribution workflow.

## [0.3.0-beta.6] - 2026-10-26

### Changed
- Renames the application, installers, and distribution archives to markdim.
- Adds the Windows Store MSIX manifest and package preparation script, including `.md` and `.markdown` associations.

## [0.3.0-beta.5] - 2026-10-26

### Fixed
- Uses IBM Plex Sans for PDF text and headings; JetBrains Mono remains reserved for code blocks.

## [0.3.0-beta.4] - 2026-10-26

### Changed
- Replaces QuestPDF with PDFsharp-MigraDoc 6.2.4 (MIT) for PDF generation.
- Pins System.Security.Cryptography.Pkcs to 8.0.2 to avoid CVE advisories against the previous transitive version.

## [0.3.0-beta.3] - 2026-10-26

### Added
- Adds installable `.deb` packages for Debian/Ubuntu on x64 and ARM64, with menu entry, icon, and Open With association.
- Keeps the portable `.tar.gz` archive for other Linux distributions.

## [0.3.0-beta.2] - 2026-10-26

### Added
- Adds a graphical Windows `.exe` installer with shortcuts and clean removal of the `.md` association.
- Keeps the portable ZIP archive alongside the installer.

## [0.3.0-beta.1] - 2026-10-26

### Beta
- First public Windows preview of markdim, a cross-platform Markdown editor with live preview, tabs, and PDF export.
- This preview ships only as a Windows x64 package; macOS and Linux releases will follow platform-specific validation.

## [0.2.34] - 2026-10-25

### Fixed
- Horizontally reveals the line and caret for each search result, including matches extending beyond the viewport.

## [0.2.33] - 2026-10-25

### Changed
- Confirms a document search with Enter (Shift+Enter for the previous match) without leaving the search field, and focuses the field when opened from the menu.

## [0.2.32] - 2026-10-25

### Changed
- Presents keyboard shortcuts in a genuinely aligned two-column grid, independent of the system's proportional font.

## [0.2.31] - 2026-10-25

### Changed
- Uses each platform's default UI font instead of bundling IBM Plex Sans; retains JetBrains Mono NL in the editor.

## [0.2.30] - 2026-10-25

### Fixed
- Preserves the association button's contrasting text brush when its label is updated in Settings.
- Sets modal button hover states directly so primary buttons retain a dark accent background and readable text.

## [0.2.29] - 2026-10-25

### Changed
- Simplifies the Markdown association status and no longer displays the other installation's full path.

## [0.2.28] - 2026-10-25

### Fixed
- Improves modal readability with 16 px semibold body text and strong colors on text rendered by Fluent checkbox presenters.
- Visually verifies About, Shortcuts, and Settings dialogs in light and dark themes using Avalonia render captures.

## [0.2.27] - 2026-10-25

### Fixed
- Targets all visual text in modal windows after they open, without depending on their position under a `ScrollViewer`.
- Adds a capture and report of effective modal colors in `--debug-modal=about|shortcuts|settings` mode.

## [0.2.26] - 2026-10-25

### Fixed
- Applies contrasting dialog text colors after windows and Fluent templates have been materialized.

## [0.2.25] - 2026-10-25

### Fixed
- Forces light/dark theme inheritance and high-contrast text colors in every modal window, including its buttons.

## [0.2.24] - 2026-10-25

### Fixed
- Removes special handling that prevented the File menu from using the same Fluent states as other menus.
- Connects ListBox/ComboBox selection colors (normal, hover, and pressed) to contrasting light/dark theme colors.

## [0.2.23] - 2026-10-25

### Fixed
- Replaces the flat gray light palette with a clearer white-paper/light-shell/tinted-library hierarchy and denser text.
- Strengthens menu hover backgrounds and covers Fluent pointer, selected, and pressed states.
- Applies theme brushes directly to modal windows and text content, with explicit white backgrounds.
- Verifies AA contrast for body text (4.5:1 minimum) across surfaces and tokens in both themes.

## [0.2.22] - 2026-10-25

### Fixed
- Forces explicit contrast for tooltips, tab labels, document outline, and file list in both themes.
- Makes search-field watermarks fully opaque and logs their effective application in debug mode.

## [0.2.21] - 2026-10-25

### Fixed
- Applies markdim colors to the actual Fluent presenters for menus, tabs, lists, selections, and buttons, not just their parent controls.
- Makes light mode black-on-gray and strengthens library/outline labels.

## [0.2.20] - 2026-10-25

### Fixed
- Darkens light-theme text substantially and increases file-title and label weight for the requested black-on-gray appearance.
- Makes list selection accents opaque with readable white text and configures menu popups by theme.
- Debug mode separately captures the File popup and records its effective colors and opacity.

## [0.2.19] - 2026-10-25

### Fixed
- Explicitly applies theme colors to Fluent menus that continued to use the system light palette in dark mode.
- Strengthens the light palette with readable gray surfaces and more assertive dark text.

## [0.2.18] - 2026-10-25

### Fixed
- Strengthens light-theme text with slightly darker gray surfaces and heavier black labels.
- Overrides Fluent selection colors with an opaque accent and retains contrasting text.
- Adds a separate debug export of the File popup, which lives in a separate native window.

## [0.2.17] - 2026-10-25

### Fixed
- Changes light mode to distinct gray surfaces with near-black text and stronger typography.
- Explicitly applies hover and text colors to Fluent dropdown menus.
- Adds a debug mode that can open the File menu in a capture to verify its colors.

## [0.2.16] - 2026-10-25

### Fixed
- Uses a true light-gray canvas, near-black text, and stronger type for readability.
- Opens the File menu in diagnostic captures with `--debug-menu` to verify light and dark colors.
- Configures list selection and menu popups with surfaces and text matching each theme.

## [0.2.15] - 2026-10-25

### Fixed
- Avoids a visual-tree exception when an image is the only paragraph content by detaching the block before final insertion.
- Renders Markdown images in separate blocks so they are no longer clipped to text-line height.
- Keeps editor autolinks in the theme color instead of AvaloniaEdit's default blue.
- Strengthens light-theme body text and labels with darker colors and higher weight.

## [0.2.14] - 2026-10-25

### Fixed
- Detaches preview image blocks from their original paragraph before inserting them, avoiding `A visual is already attached to another control`.
- Applies AvaloniaEdit's theme brush to automatic URLs in the editor and sizes image paragraphs as blocks.
- Strengthens light-theme typography.

### Added
- Debug capture with effective control state (opacity/foreground/enabled) and detailed image connection/decoding phases.

## [0.2.13] - 2026-10-25

### Fixed
- Renders Markdown images as standalone blocks instead of clipping them to text lines.
- Keeps remote image downloads alive through transient visual-tree detachments; cancels only after a sustained removal.
- Connects editor autolink colors to the theme brush and strengthens light-theme font weights.

## [0.2.12] - 2026-10-25

### Fixed
- Forces AvaloniaEdit automatic hyperlinks to use the markdim link brush instead of the system blue.
- Delays image request cancellation by 750 ms to ignore transient layout detachments; real navigation still cancels obsolete downloads.
- Renders Markdown images as standalone blocks instead of clipped inline objects.
- Improves light-theme text contrast and weight.

## [0.2.11] - 2026-10-25

### Fixed
- Uses the theme brush for editor autolinks (AvaloniaEdit previously painted them in the system default blue).
- Avoids canceling image downloads during transient layout detachments and assigns previews a size after decoding.
- Strengthens light-theme preview body text.

### Added
- Adds `--debug` logging and capture options (`--debug-capture`, `--debug-theme=light|dark`).

## [0.2.10] - 2026-10-25

### Added
- Adds `--debug` logging and on-demand PNG captures (`Ctrl+Shift+F12`) or startup capture (`--debug-capture`).
- Adds a theme override for separate Light or Dark captures (`--debug-theme=light|dark`).

### Fixed
- Replaces the editor's automatic system-blue URLs with the theme-specific link brush.
- Sizes decoded remote-image previews from their actual dimensions.

## [0.2.9] - 2026-10-25

### Added
- Adds `--debug` logging of effective Fluent colors and image-load results; `Ctrl+Shift+F12` saves a local render capture.
- Bounds remote image requests and decoded pixels, rejects non-public hosts, and disables redirects.
- Sizes decoded remote images so they are visible in the preview.

## [0.2.8] - 2026-10-25

### Fixed
- Explicitly sizes decoded remote images from their pixel dimensions so they occupy visible space in the preview layout.

## [0.2.7] - 2026-10-25

### Fixed
- Configures Fluent from the markdim palettes so native controls use consistent theme colors.
- Displays HTTPS remote Markdown images as well as local relative images, with public-DNS validation, strict HTTPS, and memory/decoder limits.

## [0.2.6] - 2026-10-25

### Fixed
- Replaces scattered Fluent color overrides with official light/dark `ColorPaletteResources` configured from audited tokens.
- Keeps a dedicated link token per theme and explicitly applies watermark contrast after templates load.

## [0.2.5] - 2026-10-25

### Fixed
- Configures official Fluent Light/Dark palettes with markdim contrast tokens instead of patching internal resources control by control.
- Removes the search watermark's 50% opacity after templates are applied.
- Makes the outline perform a second scroll update after the editor layout completes.

## [0.2.4] - 2026-10-25

### Fixed
- Forces every preview text run to use the current theme, preventing overly pale inherited colors.
- Makes the document outline position the caret, select the heading, and scroll the editor to the chosen line.
- Explicitly applies the theme link color to editor and preview text.

## [0.2.3] - 2026-10-25

### Fixed
- Strengthens contrast for selected lists, menus, and search fields; removes translucent attenuation from hint text.
- Verifies all text/background pairs and Markdown/code syntax colors in both themes.

## [0.2.2] - 2026-10-25

### Fixed
- Replaces low-contrast dark-theme blues for editor and preview links.
- Applies a markdim-controlled syntax palette to all code-block languages and verifies WCAG AA in light and dark themes.

## [0.2.1] - 2026-10-25

### Added
- Adds drag-and-drop tab reordering.
- Adds editor search with automatic scrolling to each match.
- Adds theme-aware Markdown link colors verified for WCAG AA; other source characters remain unhighlighted.

## [0.2.0] - 2026-10-25

### Added
- Adds resource limits and asynchronous loading for documents, folders, and previews; filters image paths and external links.
- Adds find/replace, document outline, and drag-and-drop opening.
- Adds PDF export with code blocks and atomic destination replacement.

### Fixed
- Preserves file encoding and line endings on save, using atomic writes.
- Accepts valid local image paths even before the target file has been created.

## [0.1.31] - 2026-10-25

### Changed
- Replaces the legacy icon with the supplied vector symbol, matching the slate background and blue accent, and updates the multi-resolution Windows icon.

## [0.1.30] - 2026-10-25

### Changed
- Improves readability of panel headers, library metadata, and status-bar text with larger type and stronger contrast.

## [0.1.29] - 2026-10-25

### Changed
- Uses the supplied three SVG icons for navigation and folder opening, with light/dark color variants.

## [0.1.28] - 2026-10-25

### Changed
- Adopts IBM Plex Sans for the UI and JetBrains Mono NL for code.

## [0.1.27] - 2026-10-25

### Changed
- Moves Settings and Keyboard Shortcuts into resizable, non-modal windows with a scrollable body and fixed footer.
- Makes the other help and confirmation window content scrollable while keeping actions in the footer.

## [0.1.26] - 2026-10-25

### Changed
- Replaces Geist Mono with JetBrains Mono NL for the editor and code, chosen for readability and distinct characters; retains Geist for the UI.

## [0.1.25] - 2026-10-25

### Fixed
- Bundles static regular/bold/italic Geist Mono variants without `#` ligature substitutions so source characters remain separate in the editor.

## [0.1.24] - 2026-10-25

### Fixed
- Updates AvaloniaEdit to 11.2.0 to fix repeated-character rendering in the editor.

## [0.1.23] - 2026-10-25

### Changed
- Removes source-editor syntax highlighting to preserve exact visibility of every character.
- Shows spaces, tabs, and line endings by default, with two independent Settings options.

## [0.1.22] - 2026-10-25

### Fixed
- Removes bold styling from editor heading highlights so every `#` marker remains visible.

## [0.1.21] - 2026-10-25

### Fixed
- Disables ligatures in code areas so every typed Markdown character remains visible in the editor.

## [0.1.20] - 2026-10-25

### Fixed
- Uses static regular/bold/italic Geist Mono variants to stabilize accented Markdown rendering in the editor.
- Aligns navigation icon geometry within a shared render box.

## [0.1.19] - 2026-10-25

### Added
- Adds a New command (`Ctrl+N`) for blank Markdown documents; Save opens Save As for untitled documents.

### Fixed
- Brings the destructive Discard button to 4.5:1 contrast in light and dark themes.
- Explicitly centers folder icons in their buttons.

## [0.1.18] - 2026-10-25

### Added
- Adds **Save As…** to create a Markdown copy and continue editing under its new name.

### Changed
- Makes modal buttons visible, distinguishes actions by role, and aligns them in a full-width footer.

## [0.1.17] - 2026-10-25

### Changed
- Makes the whole file row in the library respond to clicks and context-menu actions.
- Replaces Inter and system code fonts with Geist for the UI/preview and Geist Mono for the editor/code.

## [0.1.16] - 2026-10-25

### Added
- Adds a library context menu (right-click) to close the selected document, with unsaved-change protection.

## [0.1.15] - 2026-10-25

### Changed
- Shrinks and centers the tab close icon.

## [0.1.14] - 2026-10-25

### Fixed
- Avoids a duplicate editor-text replacement when closing the final document.
- Replaces preview code-block line-number margins with a stable gutter after a reported close-document crash.

## [0.1.13] - 2026-10-25

### Added
- Adds an **Open Recent** submenu with the five most recently used Markdown files, persisted between sessions.
- Adds an action to clear the list; missing paths are removed when selected.

## [0.1.12] - 2026-10-25

### Added
- Starts the window maximized and toggles full screen with F11 or the View menu.
- Adds `Ctrl` + mouse-wheel editor zoom, persisted between sessions.
- Extends editor text size range from 8 to 32 pt.

## [0.1.11] - 2026-10-25

### Fixed
- Improves low-contrast secondary labels in the light theme.
- Refreshes Markdown preview and tabs after theme changes to avoid stale colors.

## [0.1.10] - 2026-10-25

### Added
- Adds a close icon to every tab and the `Ctrl+W` shortcut, with save/discard handling for unsaved changes.
- Adds a Markdown home page describing app features, shown when no document is open.
- Adds a startup preference to show the home page or resume the last document.

## [0.1.9] - 2026-10-25

### Added
- Adds a Copy button to each preview code-block header, with status feedback.
- Adds a status bar showing document, caret position, and line/character counts.

### Changed
- Improves spacing between the line-number gutter and text in the editor and preview code blocks.
- Hides the vertical scrollbar for short code samples; shows it only for long blocks.

## [0.1.8] - 2026-10-25

### Added
- Adds a Copy button to each preview code-block header, with status feedback.

### Changed
- Widens code-block line-number gutters and inner spacing.
- Hides the vertical scrollbar for short samples; shows it only for long blocks.

## [0.1.7] - 2026-10-25

### Added
- Adds a Copy button and language badge to each preview code block.
- Adds a status bar showing document, caret position, and line/character counts.

### Changed
- Improves spacing between line-number gutters and text in the editor and code blocks.
- Shows vertical scrollbars in code blocks only when they exceed the visible height.

## [0.1.6] - 2026-10-25

### Added
- Adds a per-user Windows update installer that re-registers `.md`/`.markdown` with the newly installed release.

### Fixed
- Updates the Windows Open With command to the new executable on every release install.

## [0.1.5] - 2026-10-25

### Added
- Adds a status bar with active file path/name, caret position, line/character counts, and save encoding.
- Adds a persistent icon rail to restore collapsed library, editor, or preview panels.

### Changed
- Removes collapse chevrons from panel headers; the icon rail becomes the always-visible control.
- Replaces the text-based Open Folder button with a compact icon action in the library.

## [0.1.4] - 2026-10-25

### Added
- Adds a Markdig-based preview for CommonMark/GFM: autolinks, reference links, nested lists, tables, tasks, and safe HTML.
- Adds vector folder and panel-collapse icons.

### Changed
- Moves UI settings into the File menu.

### Fixed
- Fixes a crash caused by a code block nested inside an ordered list.
- Preserves Markdown-derived HTML rendering and structure in the preview.

## [0.1.3] - 2026-10-25

### Added
- Sets the app version to `0.1.3` and displays it in **Help → About**.
- Integrates the SVG brand icon and Windows icon.
- Uses a single-row main menu; commands remain available in its menus.
- Adds File, Edit, Document, and Help menus with keyboard-accessible commands.
- Adds shortcuts for opening, saving, exporting, searching, switching, and reordering documents.
- Adds persistent Settings for editor font size, library, preview, and panel proportions.
- Adds draggable splitters for resizing the library and editor/preview panes.
- Adds independent show/hide controls for the library, editor, and preview.
- Adds a persistent light/dark theme, also applied to the Markdown preview.
- Adds per-user Windows registration/removal of markdim in **Open with** for `.md` and `.markdown`.
- Adds a code editor with line numbers and Markdown highlighting for both themes.
- Adds syntax-highlighted preview code blocks with line numbers, bounded scrolling, and refined layout.

### Fixed
- Loads the complete Markdown assembly required for rendering; preview no longer fails with `NotSupportedException`.
- Avoids unintended selection changes while loading or filtering the document list.
- Isolates Markdown rendering errors and pauses preview above 500,000 characters to preserve responsiveness.
- Keeps library file paths on one line with truncation and a tooltip containing the full path.
- Logs fatal exceptions without retaining document text or paths.

## [0.1.0] - 2026-10-25

### Added
- First markdim release: a desktop Markdown reading and editing workspace.
- Adds file/folder opening, document search, tabs, and live preview.
- Adds local PDF export and a rotating local diagnostic log that excludes document contents and paths.
- Adds unit tests for the document model and multi-platform CI/release workflows.
