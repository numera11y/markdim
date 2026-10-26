# Hiérarchie des sorties de release

Les builds locaux suivent une arborescence par version, système et architecture :

```text
release/
└── v0.3.0-beta.6/
    ├── README.txt
    ├── windows/
    │   └── x64/
    │       ├── markdim-v0.3.0-beta.6-windows-x64.zip
    │       ├── markdim-v0.3.0-beta.6-windows-x64.zip.sha256
    │       ├── markdim-Setup-v0.3.0-beta.6-windows-x64.exe
    │       ├── markdim-Setup-v0.3.0-beta.6-windows-x64.exe.sha256
    │       ├── Install-markdim.ps1
    │       ├── register-file-association.ps1
    │       └── markdim/ (payload complet)
    └── linux/
        ├── x64/
        │   ├── markdim-v0.3.0-beta.6-linux-amd64.deb
        │   └── markdim-v0.3.0-beta.6-linux-x64.tar.gz
        └── arm64/
            ├── markdim-v0.3.0-beta.6-linux-arm64.deb
            └── markdim-v0.3.0-beta.6-linux-arm64.tar.gz
```

Les dossiers macOS sont générés de la même façon. Le dossier `markdim/` est le payload autonome complet; ne distribuer que les paquets générés, jamais les fichiers nus à la racine du dépôt. Sous Windows, l’EXE Setup fournit l’installation graphique; sous Debian/Ubuntu Linux, les `.deb` installent l’application et l’entrée de menu. Les ZIP/TAR.GZ restent portables. `README.txt` identifie les paquets et leurs sommes SHA-256.

Commande de production sous PowerShell :

```powershell
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.6 -RuntimeIdentifier win-x64
./packaging/windows/Build-Installer.ps1 -Version 0.3.0-beta.6
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.6 -RuntimeIdentifier linux-x64
bash ./packaging/linux/Build-DebianPackage.sh 0.3.0-beta.6 linux-x64
```

Le workflow GitHub Actions (`.github/workflows/ci.yml`) vérifie les pull requests. Les tags `vX.Y.Z-beta.N` publient Windows et Linux; les tags stables `vX.Y.Z` construisent les cinq RID. Chaque archive, paquet `.deb` et EXE Setup sont joints à la GitHub Release avec leur SHA-256; les releases bêta sont marquées comme préversions.
