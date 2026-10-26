# Hiérarchie des sorties de release

Les builds locaux suivent une arborescence par version, système et architecture :

```text
release/
└── v0.3.0-beta.1/
    ├── README.txt
    └── windows/
        └── x64/
            ├── Marginalia-v0.3.0-beta.1-windows-x64.zip
            ├── Marginalia-v0.3.0-beta.1-windows-x64.zip.sha256
            ├── Install-Marginalia.ps1
            ├── register-file-association.ps1
            └── Marginalia/
                ├── Marginalia.exe
                └── dépendances de publication
```

Les dossiers `macos/{x64,arm64}` et `linux/{x64,arm64}` sont générés de la même façon. Le dossier `Marginalia/` est le payload autonome complet; ne distribuer qu’une archive créée par le script, jamais les fichiers nus à la racine du dépôt. Sous Windows, extraire le ZIP puis lancer `Install-Marginalia.ps1` pour copier cette version vers un emplacement utilisateur stable et remettre à jour l’association `.md`. `README.txt` identifie la version, la plateforme, la commande de lancement et l’archive correspondante.

Commande de production sous PowerShell :

```powershell
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.1 -RuntimeIdentifier win-x64
```

Le workflow GitHub Actions (`.github/workflows/ci.yml`) vérifie les pull requests. Les tags `vX.Y.Z-beta.N` ne publient que Windows; les tags stables `vX.Y.Z` construisent les cinq RID. Chaque archive est jointe à une GitHub Release avec SHA-256; les releases bêta sont marquées comme préversions.
