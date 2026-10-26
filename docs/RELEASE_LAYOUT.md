# Hiérarchie des sorties de release

Les builds locaux suivent une arborescence par version, système et architecture :

```text
release/
└── v0.3.0-beta.2/
    ├── README.txt
    └── windows/
        └── x64/
            ├── Marginalia-v0.3.0-beta.2-windows-x64.zip
            ├── Marginalia-v0.3.0-beta.2-windows-x64.zip.sha256
            ├── Marginalia-Setup-v0.3.0-beta.2-windows-x64.exe
            ├── Marginalia-Setup-v0.3.0-beta.2-windows-x64.exe.sha256
            ├── Install-Marginalia.ps1
            ├── register-file-association.ps1
            └── Marginalia/
                ├── Marginalia.exe
                └── dépendances de publication
```

Les dossiers `macos/{x64,arm64}` et `linux/{x64,arm64}` sont générés de la même façon. Le dossier `Marginalia/` est le payload autonome complet; ne distribuer que les paquets générés, jamais les fichiers nus à la racine du dépôt. Sous Windows, l’EXE Setup fournit l’installation graphique; le ZIP reste portable et peut être installé avec `Install-Marginalia.ps1`. `README.txt` identifie les paquets et leurs sommes SHA-256.

Commande de production sous PowerShell :

```powershell
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.2 -RuntimeIdentifier win-x64
./packaging/windows/Build-Installer.ps1 -Version 0.3.0-beta.2
```

Le workflow GitHub Actions (`.github/workflows/ci.yml`) vérifie les pull requests. Les tags `vX.Y.Z-beta.N` ne publient que Windows; les tags stables `vX.Y.Z` construisent les cinq RID. Chaque archive et l’EXE Setup Windows sont joints à la GitHub Release avec leur SHA-256; les releases bêta sont marquées comme préversions.
