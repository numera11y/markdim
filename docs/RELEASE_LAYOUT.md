# Release output layout

Local release builds use a directory tree organized by version, operating system, and architecture:

```text
release/
└── v0.3.0-beta.7/
    ├── README.txt
    ├── windows/
    │   └── x64/
    │       ├── markdim-v0.3.0-beta.7-windows-x64.zip
    │       ├── markdim-v0.3.0-beta.7-windows-x64.zip.sha256
    │       ├── markdim-Setup-v0.3.0-beta.7-windows-x64.exe
    │       ├── markdim-Setup-v0.3.0-beta.7-windows-x64.exe.sha256
    │       ├── Install-markdim.ps1
    │       ├── register-file-association.ps1
    │       └── markdim/ (complete payload)
    └── linux/
        ├── x64/
        │   ├── markdim-v0.3.0-beta.7-linux-amd64.deb
        │   └── markdim-v0.3.0-beta.7-linux-x64.tar.gz
        └── arm64/
            ├── markdim-v0.3.0-beta.7-linux-arm64.deb
            └── markdim-v0.3.0-beta.7-linux-arm64.tar.gz
```

macOS output directories are generated in the same way. `markdim/` is the complete self-contained payload; distribute generated packages, not loose files from the repository root. The Windows Setup EXE provides a graphical installer; Debian/Ubuntu `.deb` packages install the app and desktop entry. ZIP/TAR.GZ archives are portable. `README.txt` identifies packages and their SHA-256 checksums.

Build examples (PowerShell unless marked `bash`):

```powershell
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.7 -RuntimeIdentifier win-x64
./packaging/windows/Build-Installer.ps1 -Version 0.3.0-beta.7
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.7 -RuntimeIdentifier linux-x64
bash ./packaging/linux/Build-DebianPackage.sh 0.3.0-beta.7 linux-x64
```

GitHub Actions CI (`.github/workflows/ci.yml`) verifies pull requests. Beta tags (`vX.Y.Z-beta.N`) publish Windows and Linux; stable tags (`vX.Y.Z`) build all five runtime identifiers. Each archive, `.deb` package, and Setup EXE is attached to the GitHub Release with its SHA-256 checksum; beta releases are marked as prereleases.
