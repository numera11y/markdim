# Audit de contraste des couleurs

L’application vise le contraste WCAG AA de **4,5:1** pour le texte courant dans les thèmes clair et sombre. Les ratios sont calculés selon la formule de luminance relative WCAG sur les couleurs sRGB opaques, sans arrondi avant la comparaison.

## Résultat vérifié

| Zone | Thème clair : ratio minimum | Thème sombre : ratio minimum |
|---|---:|---:|
| Texte, texte secondaire, accent, liens, texte sur boutons, états de survol et surfaces de l’interface | 4,57:1 | 5,25:1 |
| Toutes les couleurs déclarées de coloration Markdown et de code, y compris les fonds locaux de code | 5,64:1 | 4,93:1 |
| Liens du source Markdown | 7,14:1 | 11,15:1 |

Le minimum clair de la palette est l’accent `#315EC7` sur le fond de survol `#DDE3EB` (4,57:1). Le minimum sombre est le texte secondaire `#A0AAB8` sur le survol sélectionné `#29354A` (5,25:1). Les couleurs de syntaxe externes enregistrées par AvaloniaEdit ne sont plus utilisées dans l’aperçu : chaque langage utilise la palette contrôlée par markdim.

## Éléments couverts

- Surfaces de fenêtre, panneau, canevas, survol et sélection des listes/menus.
- Texte principal/secondaire, liens, accents, champs de recherche et watermarks (leur opacité par défaut de 50 % est neutralisée), boutons principaux/destructifs et leurs états de survol/désactivés.
- Couleurs de tous les tokens des définitions XSHD du source Markdown et des blocs de code, avec leur fond déclaré lorsqu’il existe.
- Le test `EveryApplicationTextTokenMeetsAaAcrossItsThemeSurfaces` vérifie les paires des palettes; `AllMarkdownAndCodeHighlightingColorsMeetAa` vérifie toutes les couleurs XSHD dans les deux thèmes.
- Les palettes officielles Light et Dark de Fluent sont définies depuis les tokens testés (BaseHigh/Medium, surfaces, accent, erreurs). Les styles de contrôles consomment ces palettes au lieu de recevoir des overrides individuels; le lien conserve un token dédié pour éviter un bleu peu lisible en sombre.

Les composants réellement désactivés restent reconnaissables, mais WCAG les exempte du critère 1.4.3; le style de l’application conserve néanmoins un texte lisible sans l’atténuation translucide par défaut. Les icônes et indicateurs non textuels ne sont pas inclus dans le seuil de 4,5:1 (ils relèvent du critère distinct de contraste non textuel).
