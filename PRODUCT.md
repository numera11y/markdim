# Product

<!-- impeccable:product-schema 1 -->

## Platform

adaptive

## Stack

C# with Avalonia UI, confirmed by the user for a shared Windows, macOS, and Linux desktop application.

## Users

The primary user produces Markdown files frequently and wants a dedicated desktop application to read and correct them.

## Product Purpose

Open, read, edit, organize, and export Markdown documents to PDF when needed.

## Operating Context

Local desktop use. Users open individual `.md` files or folders containing multiple Markdown files, switch between documents, and reorder document tabs.

## Capabilities and Constraints

Confirmed requirements: distributable application for Windows, macOS, and Linux; associate `.md` files with the application; open a file or a folder of files; multiple reorderable document tabs; read and edit Markdown; PDF export. Additional requested interface capabilities: collapse/restore the library, editor, and preview panels; resize them; persist layout preferences; and offer light/dark themes. Windows exposes a user-level Open With registration toggle; changing the system default remains an OS-controlled action. Packaging and signing strategy remains undecided.

## Product Principles

- Keep documents local and easy to open.
- Make switching among many documents immediate.
- Preserve the source text while making it comfortable to read.
- Keep editing and export within one desktop workflow.
- Support the three desktop operating systems from one codebase.
