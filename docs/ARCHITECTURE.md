# Architecture technique

## Couches actuelles

- `MarkdownDesk`: application desktop Avalonia, gestion du cycle de vie, dialogues natifs, édition/aperçu et génération PDF.
- `DocumentTab`: modèle observable d’un document ouvert et suivi du statut modifié.
- `AppDiagnostics`: journal local borné (1 Mo + un fichier de rotation), limité aux horodatages, opérations et types d’exception.
- `UserSettingsStore`: préférences d’interface JSON locales, écrites atomiquement et bornées à des valeurs valides.
- `FileAssociationService`: enregistrement/retrait par utilisateur dans le Registre Windows; ne modifie pas le choix système protégé de l’application par défaut.
- `AppTheme`: palette sémantique claire/sombre exposée en ressources Avalonia dynamiques.
- `MarkdownHighlighting`: définitions XSHD légères pour colorer le Markdown source sans service externe.
- `MarkdownPreviewRenderer`: pipeline Markdig CommonMark/GFM et construction de contrôles Avalonia pour le HTML structuré, listes imbriquées, tableaux, images locales et blocs de code.
- `MarkdownDesk.Tests`: tests unitaires indépendants de l’interface.
- `packaging/`: métadonnées et scripts d’intégration aux associations de fichiers par OS.

## Fichiers et mémoire

Les documents restent à leur emplacement et ne sont jamais copiés vers un service distant. Les onglets d’un dossier sont indexés sans charger tout leur contenu : le texte est lu à la première activation, puis conservé en mémoire pour préserver les modifications lors des changements d’onglet. L’aperçu est temporisé de 180 ms après la dernière frappe; au-delà de 500 000 caractères, il est suspendu sans désactiver l’éditeur. Les erreurs du moteur de rendu sont isolées de la navigation. Le filtrage et la prévisualisation ne créent pas de fichiers temporaires persistants.

Le journal applicatif se trouve dans `%LOCALAPPDATA%/Marginalia/logs/application.log` (Windows), `~/Library/Application Support/Marginalia/logs/application.log` (macOS) ou `~/.local/share/Marginalia/logs/application.log` (Linux selon le chemin .NET). La rotation conserve au plus le journal courant et sa copie `.1`. Les textes et chemins des documents ne sont pas journalisés. Aucun télémétrie ni réseau au runtime n’est ajouté.

Les préférences utilisateur sont séparées dans `%LOCALAPPDATA%/Marginalia/settings.json`, enregistrées de façon atomique. Le choix du thème ne modifie pas les fichiers Markdown.

## Dépendances et audit

Les versions NuGet sont explicites dans les fichiers projet. Le restore NuGet effectue l’audit des avis connus; les workflows CI doivent échouer si une dépendance vulnérable est ajoutée. Le protocole D-Bus est explicitement sur une version corrigée (0.21.3) pour éviter la version vulnérable transitivement fournie par la pile Avalonia.

## Distribution

Le publish est self-contained par RID. Les archives sont produites pour win-x64, osx-x64, osx-arm64, linux-x64 et linux-arm64. Ce sont des archives exécutables, pas encore des installateurs signés. Le premier lancement/association par défaut reste soumis au mécanisme de chaque OS. Linux requiert certaines bibliothèques graphiques système; macOS requiert signature et notarisation pour une distribution grand public sans avertissement Gatekeeper.
