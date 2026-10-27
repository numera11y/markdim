# Testing and quality

## Local commands

```sh
dotnet restore markdim.slnx --locked-mode
dotnet format markdim.slnx --verify-no-changes --no-restore
dotnet build markdim.slnx -c Release --no-restore
dotnet test markdim.slnx -c Release --no-build
dotnet list markdim.slnx package --vulnerable --include-transitive
```

Current tests cover the displayed product version, clean/modified transitions, save state, lazy loading, real PDF creation, preference persistence, Open With command formatting, highlighting themes, code-fence aliases, and Markdig conversion for nested lists, links, tables, and task lists.

`MarkdownDesk.Tests/Fixtures/MarkdownItFeatureCoverage.md` is a representative markdown-it feature corpus: heading levels, horizontal rules, nested/ordered lists, indented and fenced code, aligned tables, reference links/images, footnotes, definition lists, abbreviations, containers, and emphasis extensions. Tests also document two intentional differences: markdown-it emoji shortcodes remain literal text, and typographic substitutions are disabled. `MarkdownDesk.Tests/Fixtures/NestedNumberedCodeList.md` is included in the Windows smoke test for a reported crash regression. Native UI tests still require dedicated graphical runners.

## Memory and performance

Preview updates are debounced to reduce rendering allocations. Opening a folder indexes documents and loads each file when it is activated, avoiding eagerly reading every document while preserving edits when switching tabs. Profile large corpora before changing this strategy. The log is bounded and logging failures never interrupt editing.

## CI

CI (`.github/workflows/ci.yml`) restores in locked mode, verifies formatting, builds, runs tests on Windows, macOS, and Linux, and reports known NuGet vulnerabilities. Release tags (`.github/workflows/release.yml`) must match `Directory.Build.props`; beta tags build Windows and Linux, while stable tags build all five runtime identifiers. Release packages include SHA-256 checksums. The Windows installer validates its payload against the included manifest before copying files.
