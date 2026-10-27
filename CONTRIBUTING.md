# Contributing

Thanks for helping improve markdim. English is the primary language for project documentation and pull requests; the app UI currently supports English and French.

## Prerequisites

- .NET 10 SDK 10.0.100 or a later feature-band SDK in the .NET 10 line. The repository's `global.json` selects a compatible SDK.
- Git and NuGet access to restore dependencies.

## Development workflow

1. Create a branch from `main`.
2. Make a focused change and add or update tests for changed behavior.
3. Run the checks used by CI:

   ```sh
   dotnet restore markdim.slnx --locked-mode
   dotnet format markdim.slnx --verify-no-changes --no-restore
   dotnet build markdim.slnx -c Release --no-restore
   dotnet test markdim.slnx -c Release --no-build
   dotnet list markdim.slnx package --vulnerable --include-transitive
   ```

4. Run `git diff --check`, update `CHANGELOG.md` for user-visible changes, and open a pull request.

Pull requests must pass GitHub Actions checks. Do not include user documents, secrets, build binaries, or local logs.

## UI translations

The app's JSON catalogs are in `MarkdownDesk/Localization/`: `fr.json` contains the source labels (mostly French) and `en.json` contains the English translations.

To update or propose translations:

1. Keep JSON keys unchanged; they identify source labels used by the app. Edit only the values after the colon.
2. Preserve keyboard shortcuts, symbols, meaningful spacing, and format placeholders such as `{0}` and `{1}`.
3. Keep valid JSON: double quotes, commas between entries, no trailing comma, and a non-empty translation for each key.
4. For a new language, add its catalog, language code, UI selector option, and loading/fallback behavior in `Localization.cs` and the settings dialog.
5. Run `dotnet test markdim.slnx --no-restore` and describe the language and any typographic conventions in your pull request.

Tests verify that English and French catalogs have matching keys, non-empty values, unique JSON keys, and matching format placeholders.

## Versioning and releases

The app version follows SemVer and is defined in `Directory.Build.props`; user-visible changes are recorded in `CHANGELOG.md`. Beta tags (`vX.Y.Z-beta.N`) publish Windows and Linux packages. Stable tags (`vX.Y.Z`) build all five runtime identifiers (Windows, macOS, and Linux). Release assets include SHA-256 checksums; Debian/Ubuntu packages are provided for x64 and ARM64.

Signing and notarization credentials are not stored in the repository. Public macOS distribution still requires signing and notarization.
