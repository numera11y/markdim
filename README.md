# markdim

Application desktop C#/Avalonia pour ouvrir, lire, corriger et exporter des documents Markdown sous Windows, macOS et Linux.

Version actuelle : **0.3.0-beta.6** — bêta publique Windows et Linux. Elle est visible dans **Aide → À propos** et centralisée dans `Directory.Build.props`.

## Développement

Prérequis : .NET 10 SDK (le dépôt contient un `global.json` pour sélectionner une version compatible).

```sh
dotnet restore --locked-mode
dotnet run --project MarkdownDesk/MarkdownDesk.csproj
dotnet test markdim.slnx -c Release
```

## Publier une version autonome

Depuis un environnement avec le SDK .NET 10 :

```sh
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.6 -RuntimeIdentifier win-x64
./packaging/windows/Build-Installer.ps1 -Version 0.3.0-beta.6
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.6 -RuntimeIdentifier linux-x64
bash ./packaging/linux/Build-DebianPackage.sh 0.3.0-beta.6 linux-x64
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.6 -RuntimeIdentifier linux-arm64
bash ./packaging/linux/Build-DebianPackage.sh 0.3.0-beta.6 linux-arm64
```

La bêta Windows fournit un installateur graphique `.exe` et une archive ZIP portable. Linux fournit des paquets `.deb` installables sur Debian/Ubuntu (amd64 et arm64), ainsi qu’une archive `.tar.gz` portable pour les autres distributions. Les paquets installables créent les raccourcis et intègrent markdim au menu « Ouvrir avec ». Sous Debian/Ubuntu, installer le paquet téléchargé avec `sudo apt install ./markdim-v0.3.0-beta.6-linux-amd64.deb`.

L’interface utilise la police par défaut de chaque plateforme. L’export PDF intègre IBM Plex Sans pour le texte et JetBrains Mono NL pour le code; leurs licences SIL Open Font License 1.1 sont copiées dans les publications (`Assets/Fonts/IBMPlexSans-OFL.txt` et `Assets/Fonts/JetBrainsMono-OFL.txt`).

## Raccourcis clavier

- `Ctrl+O` ouvrir un fichier; `Ctrl+Maj+O` ouvrir un dossier.
- `Ctrl+N` créer un document Markdown vierge.
- `Ctrl+S` enregistrer; `Ctrl+W` fermer l’onglet; `Ctrl+Maj+E` exporter en PDF.
- `Ctrl+Maj+S` enregistrer sous un autre nom.
- `Ctrl+Tab` / `Ctrl+Maj+Tab` document suivant/précédent.
- `Ctrl+Maj+Page ↑` / `Ctrl+Maj+Page ↓` réordonner l’onglet.
- `Ctrl+K` recherche; `Ctrl+E` éditeur; `F11` plein écran.
- `Ctrl` + molette de la souris agrandit ou réduit le texte dans l’éditeur.

Le menu **Aide → Raccourcis clavier** récapitule ces commandes. En cas de plantage, le journal local (sans texte ni chemin des fichiers) se trouve sous `%LOCALAPPDATA%\markdim\logs\application.log`.

Pour diagnostiquer le thème ou les images de l’aperçu, lancer `markdim.exe --debug fichier.md`, reproduire le problème, puis utiliser `Ctrl+Maj+F12` pour enregistrer une capture. Pour une capture automatique après chargement et attente des images : `markdim.exe --debug-capture --debug-theme=light --debug-menu fichier.md` (remplacer par `dark` pour l’autre thème). Cette option ouvre Fichier et enregistre également son menu déroulant dans un fichier `menu-*.png`. Les journaux sont dans `%LOCALAPPDATA%\markdim\logs\application.log`; les captures sont dans `%LOCALAPPDATA%\markdim\diagnostics\`. Les captures peuvent contenir le document affiché : vérifie leur contenu avant de les partager. Le journal de debug trace le thème effectif et les étapes de chargement d’image sans enregistrer l’URL ni le contenu du document.

L’éditeur source affiche les numéros de ligne sans coloration syntaxique, afin de préserver chaque caractère; les espaces/tabulations et fins de ligne sont visibles par défaut et se règlent indépendamment dans les paramètres. L’aperçu s’appuie sur Markdig (CommonMark et extensions avancées GFM), rend les listes imbriquées, liens automatiques et de référence, tableaux, tâches et images locales; les blocs de code disposent de coloration selon le langage, numéros de ligne espacés, d’un bouton icône Copier et d’une scrollbar uniquement lorsqu’ils sont longs.

Le menu **Paramètres** permet de choisir le thème clair/sombre, régler la taille du texte et la largeur de la bibliothèque, puis afficher/masquer chaque panneau. Les séparateurs entre bibliothèque, éditeur et aperçu se redimensionnent à la souris; les choix et dimensions sont mémorisés dans `%LOCALAPPDATA%\markdim\settings.json`. Le menu **Affichage** permet aussi de replier rapidement un panneau.

Le workflow GitHub Actions `.github/workflows/release.yml` publie Windows et Linux pour les tags bêta `vX.Y.Z-beta.N`; un tag stable `vX.Y.Z` publie les cinq RID. Les archives, l’installateur EXE et les paquets `.deb` ont chacun une somme SHA-256. Pour préparer le paquet Microsoft Store MSIX x64, exécuter `./packaging/windows/Build-StorePackage.ps1 -PackageVersion 1.0.0.0 -Publisher "CN=…"`; le quatrième champ de version Store reste à `0` et cette version Store est distincte de la version affichée par l’application. Microsoft signe le MSIX après son envoi à Partner Center. La signature/notarisation macOS et les paquets natifs DMG/AppImage restent à configurer pour une diffusion grand public.

L’installateur de cette bêta n’est pas signé; Windows peut afficher un avertissement SmartScreen au téléchargement ou au lancement.

La génération PDF utilise PDFsharp-MigraDoc, sous licence MIT.

## Association des fichiers

Sous Windows, **Paramètres → Association des fichiers Markdown** permet d’ajouter ou retirer markdim du menu contextuel « Ouvrir avec », sans privilèges administrateur. Pour en faire l’application par défaut, sélectionner ensuite markdim dans les paramètres Windows des applications par défaut. macOS et Linux utilisent les déclarations de leurs paquets (`CFBundleDocumentTypes` et fichier `.desktop`).

Le premier prototype ouvre les fichiers passés en arguments, un fichier Markdown ou tous les fichiers `.md`/`.markdown` d’un dossier. Les onglets se sélectionnent dans la bande supérieure et se réordonnent avec les commandes fléchées. L’export PDF est local et indépendant d’un navigateur.
