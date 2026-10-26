# Marginalia

Application desktop C#/Avalonia pour ouvrir, lire, corriger et exporter des documents Markdown sous Windows, macOS et Linux.

Version actuelle : **0.3.0-beta.1** — bêta publique Windows. Elle est visible dans **Aide → À propos** et centralisée dans `Directory.Build.props`.

## Développement

Prérequis : .NET 10 SDK (le dépôt contient un `global.json` pour sélectionner une version compatible).

```sh
dotnet restore --locked-mode
dotnet run --project MarkdownDesk/MarkdownDesk.csproj
dotnet test Marginalia.slnx -c Release
```

## Publier une version autonome

Depuis un environnement avec le SDK .NET 10 :

```sh
./packaging/Publish-Release.ps1 -Version 0.3.0-beta.1 -RuntimeIdentifier win-x64
```

La bêta Windows est publiée dans `release/v0.3.0-beta.1/windows/x64/Marginalia/Marginalia.exe`. Pour installer ou mettre à jour en conservant l’association `.md`, extraire le ZIP et lancer `Install-Marginalia.ps1`; il vérifie l’intégrité du payload et installe la version pour l’utilisateur courant.

L’interface utilise la police par défaut de chaque plateforme; l’éditeur et le code utilisent JetBrains Mono NL, distribuée sous licence SIL Open Font License 1.1 (`Assets/Fonts/JetBrainsMono-OFL.txt`, copiée dans les publications).

## Raccourcis clavier

- `Ctrl+O` ouvrir un fichier; `Ctrl+Maj+O` ouvrir un dossier.
- `Ctrl+N` créer un document Markdown vierge.
- `Ctrl+S` enregistrer; `Ctrl+W` fermer l’onglet; `Ctrl+Maj+E` exporter en PDF.
- `Ctrl+Maj+S` enregistrer sous un autre nom.
- `Ctrl+Tab` / `Ctrl+Maj+Tab` document suivant/précédent.
- `Ctrl+Maj+Page ↑` / `Ctrl+Maj+Page ↓` réordonner l’onglet.
- `Ctrl+K` recherche; `Ctrl+E` éditeur; `F11` plein écran.
- `Ctrl` + molette de la souris agrandit ou réduit le texte dans l’éditeur.

Le menu **Aide → Raccourcis clavier** récapitule ces commandes. En cas de plantage, le journal local (sans texte ni chemin des fichiers) se trouve sous `%LOCALAPPDATA%\Marginalia\logs\application.log`.

Pour diagnostiquer le thème ou les images de l’aperçu, lancer `Marginalia.exe --debug fichier.md`, reproduire le problème, puis utiliser `Ctrl+Maj+F12` pour enregistrer une capture. Pour une capture automatique après chargement et attente des images : `Marginalia.exe --debug-capture --debug-theme=light --debug-menu fichier.md` (remplacer par `dark` pour l’autre thème). Cette option ouvre Fichier et enregistre également son menu déroulant dans un fichier `menu-*.png`. Les journaux sont dans `%LOCALAPPDATA%\Marginalia\logs\application.log`; les captures sont dans `%LOCALAPPDATA%\Marginalia\diagnostics\`. Les captures peuvent contenir le document affiché : vérifie leur contenu avant de les partager. Le journal de debug trace le thème effectif et les étapes de chargement d’image sans enregistrer l’URL ni le contenu du document.

L’éditeur source affiche les numéros de ligne sans coloration syntaxique, afin de préserver chaque caractère; les espaces/tabulations et fins de ligne sont visibles par défaut et se règlent indépendamment dans les paramètres. L’aperçu s’appuie sur Markdig (CommonMark et extensions avancées GFM), rend les listes imbriquées, liens automatiques et de référence, tableaux, tâches et images locales; les blocs de code disposent de coloration selon le langage, numéros de ligne espacés, d’un bouton icône Copier et d’une scrollbar uniquement lorsqu’ils sont longs.

Le menu **Paramètres** permet de choisir le thème clair/sombre, régler la taille du texte et la largeur de la bibliothèque, puis afficher/masquer chaque panneau. Les séparateurs entre bibliothèque, éditeur et aperçu se redimensionnent à la souris; les choix et dimensions sont mémorisés dans `%LOCALAPPDATA%\Marginalia\settings.json`. Le menu **Affichage** permet aussi de replier rapidement un panneau.

Le workflow GitHub Actions `.github/workflows/release.yml` publie uniquement Windows pour les tags bêta `vX.Y.Z-beta.N`; un tag stable `vX.Y.Z` publie les cinq RID. Chaque archive inclut une somme SHA-256. La signature/notarisation macOS et les paquets natifs MSIX/DMG/AppImage/DEB restent à configurer pour une diffusion grand public.

La génération PDF utilise QuestPDF; la distribution de cette bêta est soumise aux conditions de sa licence Community ou à une licence commerciale. Vérifier l’éligibilité de Numera11y avant de distribuer l’archive.

## Association des fichiers

Sous Windows, **Paramètres → Association des fichiers Markdown** permet d’ajouter ou retirer Marginalia du menu contextuel « Ouvrir avec », sans privilèges administrateur. Pour en faire l’application par défaut, sélectionner ensuite Marginalia dans les paramètres Windows des applications par défaut. macOS et Linux utilisent les déclarations de leurs paquets (`CFBundleDocumentTypes` et fichier `.desktop`).

Le premier prototype ouvre les fichiers passés en arguments, un fichier Markdown ou tous les fichiers `.md`/`.markdown` d’un dossier. Les onglets se sélectionnent dans la bande supérieure et se réordonnent avec les commandes fléchées. L’export PDF est local et indépendant d’un navigateur.
