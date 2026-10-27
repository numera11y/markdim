---
name: markdim
description: A calm, practical desk for reading and editing Markdown.
colors:
  light-window: "#F5F6F8"
  light-surface: "#FFFFFF"
  light-sidebar: "#EEF1F5"
  light-canvas: "#FFFFFF"
  light-border: "#C7CDD6"
  light-divider: "#D0D5DD"
  light-ink: "#171A21"
  light-muted: "#344054"
  light-subtle: "#475467"
  light-accent: "#275DAD"
  light-accent-hover: "#1F4F9D"
  light-hover: "#DCE8F8"
  light-danger: "#B42318"
  dark-window: "#141820"
  dark-surface: "#1C222C"
  dark-sidebar: "#191F29"
  dark-canvas: "#151A22"
  dark-border: "#343C49"
  dark-divider: "#303846"
  dark-ink: "#E6EAF0"
  dark-muted: "#D0D6E0"
  dark-subtle: "#B1BBC9"
  dark-accent: "#A3B8FF"
  dark-accent-hover: "#B6C7FF"
  dark-hover: "#34445E"
  dark-danger: "#C5342B"
typography:
  body:
    fontFamily: "Platform default UI sans-serif (Segoe UI on Windows)"
    fontSize: "14px"
    fontWeight: 500
  label:
    fontFamily: "Platform default UI sans-serif (Segoe UI on Windows)"
    fontSize: "12px"
    fontWeight: 600
  editor:
    fontFamily: "JetBrains Mono, monospace"
    fontSize: "14px"
rounded:
  control: "7px"
spacing:
  pane-horizontal: "18px"
  pane-vertical: "20px"
  control-height: "36px"
components:
  button-default:
    backgroundColor: "transparent"
    textColor: "{colors.light-ink}"
    rounded: "{rounded.control}"
    padding: "12px 8px"
  button-modal-primary:
    backgroundColor: "{colors.light-accent}"
    textColor: "#FFFFFF"
    rounded: "{rounded.control}"
    padding: "14px 9px"
  input-search:
    backgroundColor: "{colors.light-surface}"
    textColor: "{colors.light-ink}"
    rounded: "{rounded.control}"
    padding: "10px 9px"
---

# Design System: markdim

## Overview

**Creative North Star: "The Quiet Writing Desk"**

markdim is a focused native desktop workspace for a person moving through a collection of living Markdown notes. The visual language stays out of the document’s way: cool paper-like surfaces, crisp ink, quiet separators, and restrained blue for action and selection. The light and dark palettes preserve the same clear hierarchy rather than treating dark mode as a separate identity.

The workspace is organized around doing, not surveying: a slim utility rail and library support a broad source-and-preview canvas, with compact tabs connecting related documents. Native Fluent controls keep familiar interaction behavior; typography distinguishes interface labels from the monospaced source editor.

**Key Characteristics:**
- Quiet, high-contrast ink and paper in paired light and dark themes.
- Restrained blue action color and fine structural dividers.
- Document-centered, dense desktop workspace with native control behavior.

## Colors

The palette pairs cool slate surfaces with readable ink; blue marks action and selection while danger red is reserved for destructive states. Values are represented in the frontmatter and are authoritative there.

### Primary
- **Slate Blue (light):** The light-theme action and selection color.
- **Periwinkle Blue (dark):** The dark-theme action and selection color, tuned to remain legible on charcoal surfaces.

### Neutral
- **Cool Window Gray / Charcoal:** Window surround and outer workspace background.
- **Paper White / Deep Slate:** Surface and reading canvas fills.
- **Pale Slate / Night Slate:** Library pane fills, separating navigation from the document canvas.
- **Ink / Soft White:** Primary interface text and editor foreground.
- **Muted Slate:** Supporting labels, metadata, and status text.
- **Cool Gray Dividers:** Thin borders and pane separators; use the distinct border and divider tokens consistently.

### Named Rules
**The Blue-For-Action Rule.** Use the accent for selection and clear primary actions; keep ordinary workspace chrome neutral.

## Typography

**Display Font:** Platform default UI sans-serif (Segoe UI on Windows)
**Body Font:** Platform default UI sans-serif (Segoe UI on Windows)
**Label/Mono Font:** JetBrains Mono for Markdown source; Fluent UI sans-serif for labels.

**Character:** Interface text follows the host platform's default UI sans-serif, compact and direct, with medium-weight defaults and semibold emphasis for headings and section labels. The source editor’s monospaced face makes Markdown structure legible without confusing it with rendered reading content.

### Hierarchy
- **Title** (semibold, 19px): Pane headings such as the library title.
- **Body** (medium, 14px): File names, ordinary controls, and workspace text.
- **Editor** (regular, 14px): Markdown source in JetBrains Mono.
- **Label** (semibold, 11–12px): Pane captions, metadata, and status information.

### Named Rules
**The Source-Has-Its-Own-Voice Rule.** Keep Markdown source monospaced; keep application chrome in the platform UI sans-serif.

## Layout

This is a desktop-first, resizable three-part work area: a narrow utility rail, a library pane, and a broad document region. The document region combines a compact horizontal tab strip with a side-by-side source editor and rendered preview. Splitters let users adjust library and editor/preview proportions; panels can be collapsed and restored. The main window’s minimum size is 900 × 600px. Keep information compact enough for sustained desktop work while preserving generous reading and editing canvases. Pane interiors use approximately 18px horizontal and 20px vertical margins; the editor text area has its own 24px by 18px inset.

## Elevation & Depth

The interface is tonal and mostly flat. Pane fills and one-pixel borders/dividers establish structure; Fluent-native transient surfaces such as menus and tooltips supply their own lightweight separation. Avoid adding decorative shadows to the persistent workspace.

### Named Rules
**The Divider-Defines-Structure Rule.** Use the established surface fills and fine borders to separate persistent regions rather than floating cards or shadow stacks.

## Shapes

The form language is restrained and rectangular, with modest rounding reserved for controls. Standard buttons and search fields use a 7px corner radius; pane and canvas boundaries remain square and are defined by clean divider lines.

## Components

Components feel restrained and dependable, with quiet resting states and explicit hover, selection, and focus feedback.

### Buttons
- **Shape:** Gently rounded controls (7px); utility-rail buttons are compact icon targets.
- **Default:** Transparent background, ink-colored content, and comfortable internal padding (12px 8px).
- **Primary modal action:** Accent fill, contrasting white text in light mode, semibold weight, and a darker hover state.
- **Hover / Focus:** Use the theme’s hover fill for ordinary controls; primary actions shift to the theme’s accent-hover color.
- **Destructive:** Use the dedicated danger color and its hover variant only for destructive actions.

### Cards / Containers
- **Corner Style:** Workspace panes are square-edged; controls within them may use the 7px control radius.
- **Background:** Window, sidebar, surface, and canvas fills retain their distinct theme roles.
- **Shadow Strategy:** Flat persistent surfaces; refer to Elevation & Depth.
- **Border:** Fine theme border or divider lines define pane edges.
- **Internal Padding:** Library pane uses 18px horizontal and 20px vertical margins; editor content uses its dedicated inset.

### Inputs / Fields
- **Style:** Search fields sit on the surface fill with a subtle border and modest corner radius; other text boxes can remain visually quiet until placed in a panel.
- **Focus:** Preserve the platform’s native keyboard focus treatment and clear text contrast.
- **Disabled:** Use the sidebar tone and subtle text color rather than reducing contrast through opacity.

### Navigation
- **Style:** A 44px utility rail and compact horizontal document tabs keep navigation adjacent to the work area. The library lists open documents with a stronger title and quieter location metadata.
- **States:** Selected documents use the theme accent; hover states use the theme hover fill. Keep tab order and visual selection easy to scan.

### Editor and Preview
The source pane uses JetBrains Mono at 14px with line numbers and a clear canvas background. The rendered preview sits on the surface background with comfortable reading inset; keep its typographic hierarchy readable and distinct from application chrome.

### Status Bar
A compact 28px footer uses the sidebar tone and fine top divider to present file location, readiness, cursor position, document details, and format without competing with the document.

## Do's and Don'ts

### Do:
- **Do** preserve the paired light and dark theme roles and their contrast hierarchy.
- **Do** use blue for selection and primary actions, with danger red reserved for destructive actions.
- **Do** keep document content visually dominant over navigation and status information.
- **Do** use Fluent-native interaction conventions for menus, keyboard focus, and transient surfaces.

### Don't:
- **Don't** turn the writing workspace into a dashboard of floating cards.
- **Don't** use accent color as persistent decoration across the chrome.
- **Don't** replace the source editor’s monospaced typography with the interface font.
- **Don't** introduce decorative shadows on persistent panes.
