# Color contrast audit

The app targets WCAG AA contrast (**4.5:1**) for normal text in both light and dark themes. Ratios use the WCAG relative-luminance formula on opaque sRGB colors, without rounding before comparison.

## Verified results

| Area | Light theme: minimum ratio | Dark theme: minimum ratio |
|---|---:|---:|
| Main/secondary text, accents, links, button text, hover states, and UI surfaces | 4.57:1 | 5.25:1 |
| All declared Markdown and code highlighting colors, including local code backgrounds | 5.64:1 | 4.93:1 |
| Markdown source links | 7.14:1 | 11.15:1 |

The light palette minimum is accent `#315EC7` on hover background `#DDE3EB` (4.57:1). The dark palette minimum is secondary text `#A0AAB8` on selected hover `#29354A` (5.25:1). AvaloniaEdit's external syntax colors are not used in preview code blocks; every language uses the palette controlled by markdim.

## Coverage

- Window, panel, canvas, hover, and list/menu selection surfaces.
- Primary/secondary text, links, accents, search fields and watermarks (their default 50% opacity is neutralized), primary/destructive buttons, and hover/disabled states.
- Every token color in Markdown source and code XSHD definitions, including declared backgrounds.
- `EveryApplicationTextTokenMeetsAaAcrossItsThemeSurfaces` checks palette pairs; `AllMarkdownAndCodeHighlightingColorsMeetAa` checks XSHD colors in both themes.
- Official Fluent Light and Dark palettes are defined from tested tokens (BaseHigh/Medium, surfaces, accent, errors). Control styles consume these palettes instead of per-control overrides; links use a dedicated token to avoid an unreadable dark-theme blue.

Disabled components remain recognizable, but WCAG exempts them from criterion 1.4.3; the app nevertheless keeps their text readable without default translucent attenuation. Icons and non-text indicators are not included in the 4.5:1 threshold; they fall under the separate non-text contrast criterion.
