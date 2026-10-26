# Changelog

Ce fichier suit [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) et le versionnement [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.3.0-beta.4] - 2026-10-26

### Changed
- Remplace QuestPDF par PDFsharp-MigraDoc 6.2.4 (MIT) pour la génération PDF.
- Épingle System.Security.Cryptography.Pkcs à 8.0.2 pour éviter les avis CVE de la version transitive précédente.

## [0.3.0-beta.3] - 2026-10-26

### Added
- Ajoute des paquets installables `.deb` pour Debian/Ubuntu sur x64 et ARM64, avec entrée de menu, icône et association « Ouvrir avec ».
- Conserve l’archive `.tar.gz` portable pour les autres distributions Linux.

## [0.3.0-beta.2] - 2026-10-26

### Added
- Ajoute un installateur graphique Windows `.exe` avec raccourcis et désinstallation propre de l’association `.md`.
- Garde l’archive ZIP portable disponible à côté de l’installateur.

## [0.3.0-beta.1] - 2026-10-26

### Beta
- Première préversion publique Windows de Marginalia, éditeur Markdown multiplateforme avec aperçu en direct, onglets et export PDF.
- La préversion ne distribue qu’un paquet Windows x64; les plateformes macOS et Linux seront publiées après validation dédiée.

## [0.2.34] - 2026-10-25

### Fixed
- Révèle aussi horizontalement la ligne et le caret de chaque résultat de recherche, y compris lorsque l’occurrence déborde à droite.

## [0.2.33] - 2026-10-25

### Changed
- Valide la recherche du document avec Entrée (Maj+Entrée pour l’occurrence précédente) sans quitter le champ, et donne le focus au champ après ouverture depuis le menu.

## [0.2.32] - 2026-10-25

### Changed
- Présente les raccourcis clavier dans une grille à deux colonnes réellement alignées, indépendante de la police proportionnelle du système.

## [0.2.31] - 2026-10-25

### Changed
- Utilise la police d’interface par défaut de la plateforme au lieu d’embarquer IBM Plex Sans; conserve JetBrains Mono NL dans l’éditeur.

## [0.2.30] - 2026-10-25

### Fixed
- Conserve le pinceau de texte contrasté du bouton d’association quand son libellé est actualisé dans Paramètres.
- Fixe directement les états de survol des boutons modaux afin que les boutons principaux gardent un fond d’accent foncé et un texte contrasté.

## [0.2.29] - 2026-10-25

### Changed
- Simplifie l’état de l’association Markdown; n’affiche plus le chemin complet de l’autre installation.

## [0.2.28] - 2026-10-25

### Fixed
- Améliore la lisibilité réelle des modales : corps en 16 px semi-gras et couleurs fortes sur le texte rendu par les présentateurs de cases à cocher Fluent.
- Vérifie visuellement les modales À propos, Raccourcis et Paramètres en thèmes clair et sombre par captures du rendu Avalonia.

## [0.2.27] - 2026-10-25

### Fixed
- Cible enfin tous les textes visuels réellement présents dans les fenêtres modales après leur ouverture, sans dépendre de leur position sous un `ScrollViewer`.
- Ajoute une capture et un relevé des couleurs effectives des modales en mode `--debug-modal=about|shortcuts|settings` pour vérifier le rendu produit.

## [0.2.26] - 2026-10-25

### Fixed
- Applique les couleurs contrastées des textes de dialogue après l’ouverture, une fois les fenêtres et templates Fluent réellement matérialisés.

## [0.2.25] - 2026-10-25

### Fixed
- Force l’héritage du thème clair/sombre et des couleurs de texte à contraste élevé dans toutes les fenêtres modales, y compris leurs boutons.

## [0.2.24] - 2026-10-25

### Fixed
- Supprime le traitement spécial qui empêchait le menu « Fichier » de suivre les mêmes états Fluent que les autres menus.
- Relie les couleurs de sélection ListBox/ComboBox (normal, survol, pression) aux couleurs contrastées du thème clair ou sombre.

## [0.2.23] - 2026-10-25

### Fixed
- Remplace la palette claire grise et trop uniforme par une hiérarchie papier blanc / coque gris clair / bibliothèque légèrement teintée, avec texte plus dense.
- Renforce nettement le fond de survol des menus et couvre les états pointeur, sélection et pression des contrôles Fluent.
- Applique directement les pinceaux du thème aux fenêtres modales et au contenu textuel, avec fonds blancs explicites.
- Vérifie le contraste AA du texte courant (4,5:1 minimum) pour les surfaces et tokens des deux thèmes.

## [0.2.22] - 2026-10-25

### Fixed
- Force un contraste explicite pour les tooltips, les libellés d’onglets, le plan du document et la liste de fichiers dans les deux thèmes.
- Rend les watermarks des champs de recherche entièrement opaques et journalise leur application effective en mode debug.

## [0.2.21] - 2026-10-25

### Fixed
- Applique les couleurs Marginalia au contenu réel des présentateurs Fluent pour les menus, onglets, listes, sélections et boutons (pas seulement au contrôle parent).
- Rend le mode clair noir sur des surfaces grises et renforce les libellés de bibliothèque/plan.

## [0.2.20] - 2026-10-25

### Fixed
- Assombrit fortement le texte du thème clair et augmente le poids des titres de fichier et libellés pour répondre à la demande « noir sur gris ».
- Force l’accent de sélection des listes à être opaque, avec texte blanc lisible; configure aussi les popups de menu selon le thème.
- Le mode debug capture séparément le popup Fichier et inscrit ses couleurs/opacités effectives.

## [0.2.19] - 2026-10-25

### Fixed
- Applique explicitement les couleurs du thème aux menus Fluent qui continuaient d’utiliser leur palette système claire en mode sombre.
- Renforce la palette claire avec des fonds gris lisibles et un texte sombre plus affirmé.

## [0.2.18] - 2026-10-25

### Fixed
- Renforce le texte clair en adoptant des fonds gris légèrement soutenus et des libellés noirs plus lourds.
- Écrase les couleurs de sélection Fluent avec une couleur accent opaque et conserve le texte accentué contrasté.
- Ajoute un export séparé du popup Fichier en debug, car il vit dans une fenêtre native distincte.

## [0.2.17] - 2026-10-25

### Fixed
- Passe le thème clair à des surfaces grises distinctes avec texte presque noir et typographie renforcée.
- Applique explicitement les couleurs de survol et de texte aux menus déroulants Fluent.
- Ajoute un mode debug qui peut ouvrir le menu Fichier dans une capture pour valider ses couleurs.

## [0.2.16] - 2026-10-25

### Fixed
- Adopte un vrai canevas gris clair avec texte quasi noir et poids typographique plus marqué pour la lisibilité.
- Ouvre le menu Fichier dans les captures de diagnostic avec `--debug-menu`, pour vérifier ses couleurs en clair et en sombre.
- Configure la sélection et les popups de menu avec des surfaces et textes assortis à chaque thème.

## [0.2.15] - 2026-10-25

### Fixed
- Évite l’exception visuelle quand une image est le seul contenu d’un paragraphe; détache le bloc avant son insertion finale.
- Rend les images Markdown dans des blocs séparés afin qu’elles ne soient plus rognées à la hauteur d’une ligne de texte.
- Conserve les liens automatiques de l’éditeur dans la couleur du thème au lieu du bleu par défaut d’AvaloniaEdit.
- Renforce le texte courant clair et les labels avec des valeurs plus sombres et un poids supérieur.

## [0.2.14] - 2026-10-25

### Fixed
- Détache correctement les blocs d’image issus d’un paragraphe avant leur insertion dans l’aperçu, au lieu de déclencher `A visual is already attached to another control`.
- Applique le pinceau de thème d’AvaloniaEdit aux URL automatiques de l’éditeur et dimensionne les paragraphes d’image comme des blocs.
- Renforce la typographie courante claire.

### Added
- Capture debug avec état effectif des contrôles (opacité/foreground/enabled) et trace détaillée de chaque phase de connexion/décodage d’image.

## [0.2.13] - 2026-10-25

### Fixed
- Rend les images Markdown autonomes en blocs, hors des lignes de texte qui les coupaient.
- Garde les téléchargements d’images pendant les détachements transitoires du visual tree; annule seulement après un vrai retrait prolongé.
- Lie la couleur des liens automatiques de l’éditeur au pinceau du thème et renforce les poids typographiques du thème clair.

## [0.2.12] - 2026-10-25

### Fixed
- Force les hyperliens automatiques AvaloniaEdit à utiliser le pinceau de lien marginalia au lieu du bleu système par défaut.
- Diffère l’annulation réseau d’une image pendant 750 ms pour ignorer les détachements transitoires de layout; une vraie navigation annule toujours les téléchargements obsolètes.
- Rend les images Markdown comme blocs autonomes plutôt que comme objets inline coupés par la hauteur de ligne.
- Augmente le contraste et le poids du texte courant en thème clair.

## [0.2.11] - 2026-10-25

### Fixed
- Utilise le pinceau du thème pour les hyperliens automatiques de l’éditeur (AvaloniaEdit les peignait en bleu système par défaut).
- Ne coupe plus le téléchargement d’une image lors d’un détachement transitoire pendant la mise en page; les previews lui attribuent une taille après décodage.
- Renforce le poids du texte courant de l’aperçu clair pour rendre la lecture plus nette.

### Added
- Ajoute le mode `--debug` avec journal et capture (`--debug-capture`, `--debug-theme=light|dark`).

## [0.2.10] - 2026-10-25

### Added
- Mode `--debug` avec journal de diagnostic exploitable et captures PNG à la demande (`Ctrl+Maj+F12`) ou au démarrage (`--debug-capture`).
- Surcharge de thème pour capturer séparément Light ou Dark (`--debug-theme=light|dark`).

### Fixed
- Corrige le bleu système automatique des URL dans l’éditeur avec le pinceau dédié au thème.
- Dimensionne la preview des images distantes décodées à partir de leur taille réelle.

## [0.2.9] - 2026-10-25

### Added
- Mode `--debug` : journalise les couleurs Fluent réellement activées et les résultats du chargement d’images; `Ctrl+Maj+F12` exporte une capture locale du rendu.
- Le chargement des images distantes borne les requêtes et pixels décodés, refuse les hôtes non publics et n’effectue pas de redirections.
- Dimensionne les images distantes décodées pour leur donner une place visible dans le preview.

## [0.2.8] - 2026-10-25

### Fixed
- Dimensionne explicitement les images distantes décodées à partir de leurs pixels, pour qu’elles prennent une taille visible dans la mise en page du preview.

## [0.2.7] - 2026-10-25

### Fixed
- Configure le thème Fluent depuis ses palettes officielles Marginalia pour rendre les couleurs réellement cohérentes sur les contrôles natifs.
- Affiche les images Markdown HTTPS distantes en plus des images relatives locales, avec validation DNS publique, HTTPS strict et limites mémoire/décodeur.

## [0.2.6] - 2026-10-25

### Fixed
- Remplace les overrides dispersés des couleurs Fluent par les palettes officielles clair/sombre `ColorPaletteResources`, configurées depuis les tokens audités.
- Conserve un token de lien dédié par thème et applique explicitement le contraste des watermarks après chargement des templates.

## [0.2.5] - 2026-10-25

### Fixed
- Configure les palettes Fluent Light/Dark officielles avec les tokens de contraste de Marginalia, au lieu de patcher ses ressources internes contrôle par contrôle.
- Retire effectivement l’opacité de 50 % du watermark des champs de recherche une fois les templates appliqués.
- Le plan force maintenant une seconde mise à jour du défilement après le layout de l’éditeur.

## [0.2.4] - 2026-10-25

### Fixed
- Force la couleur du texte de chaque run de l’aperçu selon le thème, pour empêcher l’héritage de couleurs trop pâles.
- Le plan positionne maintenant le caret, sélectionne le titre et force le défilement de l’éditeur jusqu’à la ligne choisie.
- Applique explicitement la couleur de lien du thème au texte de l’éditeur et de l’aperçu.

## [0.2.3] - 2026-10-25

### Fixed
- Renforce le contraste des listes sélectionnées, des menus et des champs de recherche; retire l’atténuation translucide des textes indicatifs.
- Vérifie toutes les paires texte/fond des palettes, ainsi que les couleurs des syntaxes Markdown/code en thèmes clair et sombre.

## [0.2.2] - 2026-10-25

### Fixed
- Remplace les bleus à faible contraste en mode sombre pour les liens de l’éditeur et de l’aperçu.
- Applique une palette syntaxique contrôlée par Marginalia à tous les langages des blocs de code, avec vérification WCAG AA en thèmes clair et sombre.

## [0.2.1] - 2026-10-25

### Added
- Réorganisation des onglets par glisser-déposer.
- Recherche dans l’éditeur avec défilement automatique à chaque occurrence trouvée.
- Couleur des liens Markdown adaptée au thème et vérifiée avec un contraste WCAG AA; les autres caractères source restent sans coloration.

## [0.2.0] - 2026-10-25

### Added
- Limites de ressources et chargement asynchrone pour les documents, dossiers et aperçus; chemins d’images et liens externes filtrés.
- Recherche/remplacement, plan du document et ouverture par glisser-déposer.
- Export PDF avec blocs de code et remplacement atomique du fichier de destination.

### Fixed
- Préserve l’encodage et les fins de ligne à la sauvegarde, avec écriture atomique.
- Accepte les chemins d’images locales valides même avant la création du fichier cible.

## [0.1.31] - 2026-10-25

### Changed
- Remplace l’icône historique par le symbole vectoriel fourni, avec fond ardoise et accent bleu assortis au thème Marginalia; met à jour l’icône Windows multi-résolution.

## [0.1.30] - 2026-10-25

### Changed
- Renforce la lisibilité des en-têtes de panneaux, métadonnées de bibliothèque et textes de la barre d’état par une taille et un contraste accrus.

## [0.1.29] - 2026-10-25

### Changed
- Utilise les trois icônes SVG fournies pour la navigation et le bouton d’ouverture de dossier, avec ressources de couleur adaptées aux thèmes clair et sombre.

## [0.1.28] - 2026-10-25

### Changed
- Adopte le duo typographique IBM Plex Sans pour l’interface et JetBrains Mono NL pour le code.

## [0.1.27] - 2026-10-25

### Changed
- Ouvre Paramètres et Raccourcis clavier dans des fenêtres redimensionnables non modales, avec corps défilant et pied de page fixe.
- Rend le contenu des autres fenêtres d’aide et de confirmation défilant, en gardant leurs actions en pied de fenêtre.

## [0.1.26] - 2026-10-25

### Changed
- Remplace Geist Mono par JetBrains Mono NL pour l’éditeur et le code, choisi pour sa lisibilité et ses caractères distincts; conserve Geist pour l’interface.

## [0.1.25] - 2026-10-25

### Fixed
- Embarque des variantes Geist Mono sans substitutions de ligatures `#`, afin que l’éditeur affiche les caractères source séparément.

## [0.1.24] - 2026-10-25

### Fixed
- Met à jour AvaloniaEdit en 11.2.0 pour corriger le rendu des caractères répétés dans l’éditeur.

## [0.1.23] - 2026-10-25

### Changed
- Retire la coloration syntaxique de l’éditeur source pour préserver la visibilité exacte de tous les caractères.
- Affiche les espaces, tabulations et fins de ligne par défaut, avec deux options indépendantes dans les paramètres.

## [0.1.22] - 2026-10-25

### Fixed
- Retire le gras du surlignage des lignes de titre dans l’éditeur pour préserver l’affichage de chaque marqueur `#`.

## [0.1.21] - 2026-10-25

### Fixed
- Désactive les ligatures dans les zones de code afin que chaque caractère Markdown saisi reste visible dans l’éditeur.

## [0.1.20] - 2026-10-25

### Fixed
- Utilise les variantes statiques régulière/gras/italique de Geist Mono pour stabiliser le rendu du texte Markdown accentué dans l’éditeur.
- Aligne la géométrie des icônes de navigation dans une boîte de rendu commune.

## [0.1.19] - 2026-10-25

### Added
- Commande « Nouveau » (Ctrl+N) pour créer un document Markdown vierge; Enregistrer le dirige vers Enregistrer sous.

### Fixed
- Contraste du bouton destructif « Abandonner » conforme à 4,5:1 en thèmes clair et sombre.
- Alignement central explicite des boutons contenant les icônes de dossier.

## [0.1.18] - 2026-10-25

### Added
- Commande « Enregistrer sous… » pour créer une copie Markdown et continuer l’édition sous le nouveau nom.

### Changed
- Boutons des fenêtres modales visibles, différenciés selon l’action et alignés dans un pied de dialogue pleine largeur.

## [0.1.17] - 2026-10-25

### Changed
- Toute la ligne d’un fichier de la bibliothèque cible maintenant les clics et le menu contextuel.
- Remplace Inter et les polices de code système par Geist pour l’interface/aperçu et Geist Mono pour l’éditeur et le code.

## [0.1.16] - 2026-10-25

### Added
- Menu contextuel dans la bibliothèque (clic droit) pour fermer le document visé, avec protection des modifications non enregistrées.

## [0.1.15] - 2026-10-25

### Changed
- Réduit et centre l’icône de fermeture des onglets.

## [0.1.14] - 2026-10-25

### Fixed
- Évite le double remplacement du texte de l’éditeur lors de la fermeture du dernier document.
- Remplace la marge AvaloniaEdit des blocs de code de l’aperçu par une gouttière de numéros stable, après un crash signalé à la fermeture d’un document.

## [0.1.13] - 2026-10-25

### Added
- Sous-menu « Ouvrir récent » avec les cinq derniers fichiers Markdown, mémorisés entre les sessions.
- Action pour effacer la liste; les chemins manquants sont retirés lorsqu’ils sont sélectionnés.

## [0.1.12] - 2026-10-25

### Added
- Démarrage de la fenêtre en mode maximisé et bascule plein écran avec F11 ou le menu Affichage.
- Zoom du texte de l’éditeur avec Ctrl+molette, mémorisé entre les sessions.
- Plage de taille de texte étendue de 8 à 32 points.

## [0.1.11] - 2026-10-25

### Fixed
- Contraste insuffisant des libellés secondaires en thème clair.
- Aperçu Markdown et onglets actualisés après un changement de thème pour éviter de conserver d’anciennes couleurs.

## [0.1.10] - 2026-10-25

### Added
- Icône de fermeture sur chaque onglet et raccourci Ctrl+W, avec choix d’enregistrer ou d’abandonner les modifications.
- Page d’accueil Markdown décrivant les fonctions de l’application, affichée quand aucun document n’est ouvert.
- Préférence de démarrage pour ouvrir l’accueil ou reprendre le dernier document.

## [0.1.9] - 2026-10-25

### Added
- Bouton Copier dans l’en-tête de chaque bloc de code de l’aperçu, avec retour d’état.
- Barre d’état avec document, position du curseur et compte des lignes/caractères.

### Changed
- Espacement plus lisible entre gouttière de numéros et texte, dans l’éditeur et les blocs de code.
- Masque la scrollbar verticale des extraits courts; elle apparaît pour les blocs longs seulement.

## [0.1.8] - 2026-10-25

### Added
- Bouton Copier dans l’en-tête de chaque bloc de code de l’aperçu, avec retour d’état.

### Changed
- Gouttières des numéros de ligne et espacements intérieurs des blocs de code élargis.
- Masque la scrollbar verticale des extraits courts; elle apparaît pour les blocs longs seulement.

## [0.1.7] - 2026-10-25

### Added
- Bouton Copier et badge du langage dans chaque bloc de code de l’aperçu.
- Barre d’état avec document, position du curseur et compte des lignes/caractères.

### Changed
- Espacement plus lisible entre gouttière de numéros et texte, dans l’éditeur et les blocs de code.
- Scrollbar verticale des blocs de code uniquement lorsqu’ils dépassent la hauteur visible.

## [0.1.6] - 2026-10-25

### Added
- Installateur de mise à jour Windows par utilisateur, qui réenregistre `.md`/`.markdown` sur la version fraîchement installée.

### Fixed
- La commande Windows Ouvrir avec est actualisée vers le nouvel exécutable à chaque installation de release.

## [0.1.5] - 2026-10-25

### Added
- Barre d’état avec chemin et nom du fichier actif, position curseur, nombre de lignes et caractères, encodage de sauvegarde.
- Barre latérale persistante d’icônes pour restaurer bibliothèque, éditeur ou aperçu repliés.

### Changed
- Retire les chevrons de repli des en-têtes; la barre latérale sert de commande toujours visible.
- Remplace le bouton texte « Ouvrir dossier » par une action icône compacte dans la bibliothèque.

## [0.1.4] - 2026-10-25

### Added
- Aperçu basé sur Markdig pour CommonMark/GFM : autolinks, liens de référence, listes imbriquées, tableaux, tâches et HTML sûr.
- Icônes vectorielles de dossier et de repli des panneaux.

### Changed
- Déplace les paramètres d’interface dans le menu Fichier.

### Fixed
- Corrige le plantage provoqué par un bloc de code imbriqué dans une liste numérotée.
- Préserve le rendu et la structure HTML issue du Markdown dans l’aperçu.

## [0.1.3] - 2026-10-25

### Added
- Version de l’application portée à `0.1.3`, affichée dans **Aide → À propos**.
- Icône de marque SVG et icône Windows intégrées à l’application.
- Une seule rangée de menu principale; les commandes restent accessibles dans ses menus.
- Menus Fichier, Édition, Document et Aide, avec commandes accessibles au clavier.
- Raccourcis pour ouvrir, enregistrer, exporter, chercher, changer et réordonner les documents.
- Menu Paramètres avec options persistantes pour la taille de police, la bibliothèque, l’aperçu et les proportions de panneaux.
- Séparateurs à glisser pour redimensionner la bibliothèque et les panneaux éditeur/aperçu.
- Affichage/masquage indépendant des panneaux bibliothèque, éditeur et aperçu.
- Thème clair/sombre persistant, également appliqué à l’aperçu Markdown.
- Activation/retrait par utilisateur de Marginalia dans le menu Windows « Ouvrir avec » pour `.md` et `.markdown`.
- Éditeur de code avec numéros de ligne et coloration Markdown accordée au thème clair/sombre.
- Blocs de code d’aperçu avec coloration selon le langage de la fence, numéros de ligne, défilement borné et présentation affinée.

### Fixed
- Charge l’assemblage Markdown complet requis au rendu; l’aperçu ne plante plus sur `NotSupportedException`.
- Évite les changements de sélection involontaires lors du chargement ou du filtrage de la liste de documents.
- Isole les erreurs de rendu Markdown et suspend l’aperçu au-delà de 500 000 caractères pour préserver la réactivité.
- Garde les chemins de fichiers de la bibliothèque sur une ligne avec troncature et infobulle du chemin complet.
- Journalise les exceptions fatales sans conserver le texte ni le chemin des documents.

## [0.1.0] - 2026-10-25

### Added
- Première version de Marginalia : espace desktop d’édition et de lecture Markdown.
- Ouverture de fichiers et dossiers, recherche de documents, onglets et aperçu en direct.
- Export PDF local et journal de diagnostic local avec rotation, sans contenu ni chemin de document.
- Tests unitaires du modèle document et workflows CI/release multi-plateforme.
