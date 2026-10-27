# Dependencies and licenses

Direct package versions are centralized in `Directory.Packages.props`; resolved graphs are locked in each project's `packages.lock.json` file.

| Dependency | Use | License / review |
|---|---|---|
| Avalonia and Avalonia packages | Cross-platform native UI | MIT (review transitive package notices for each release) |
| AvaloniaEdit | Source editing, line numbers, highlighting engine | MIT |
| Markdig | CommonMark parsing and advanced GFM extensions | BSD-2-Clause |
| HtmlAgilityPack | Convert Markdig HTML structure into Avalonia controls | MIT |
| PDFsharp-MigraDoc | Local PDF generation and layout | MIT (version 6.2.4) |
| Microsoft.Extensions.Logging.Abstractions | PDFsharp transitive dependency | MIT |
| System.Security.Cryptography.Pkcs | PDFsharp transitive dependency | MIT; pinned to 8.0.2 for security fixes |
| xUnit and Microsoft.NET.Test.Sdk | Automated tests | See pinned NuGet package licenses |
| Tmds.DBus.Protocol | Explicit/transitive Linux desktop integration | Pinned to fixed version `0.21.3`; retain its license notice with release archives |

Before a public release, review licenses for all transitive dependencies with `dotnet list markdim.slnx package --include-transitive` and include required notices. Do not update a package without restore, tests, vulnerability audit, and license review.
