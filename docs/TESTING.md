# Tests et qualité

## Commandes locales

```sh
dotnet restore Marginalia.slnx
dotnet format Marginalia.slnx --verify-no-changes
dotnet build Marginalia.slnx -c Release --no-restore
dotnet test Marginalia.slnx -c Release --no-build
dotnet list Marginalia.slnx package --vulnerable --include-transitive
```

Les tests actuels couvrent le nom affiché, la transition propre/modifié, le marquage après sauvegarde, le chargement paresseux, la création réelle d’un PDF, la persistance des préférences, le format de commande « Ouvrir avec », les thèmes de coloration, les alias des fences et la conversion Markdig pour listes imbriquées, liens, tableaux et tâches. `MarkdownDesk.Tests/Fixtures/MarkdownItFeatureCoverage.md` sert de corpus représentatif du guide markdown-it : niveaux de titres, règles horizontales, listes imbriquées/numérotées, code indenté et fences, tables alignées, liens/images de référence, notes de bas de page, listes de définition, abréviations, conteneurs et extensions d’emphase. Les tests documentent aussi deux différences intentionnelles : les shortcodes emoji markdown-it restent textuels et les substitutions typographiques ne sont pas activées. Le fichier `MarkdownDesk.Tests/Fixtures/NestedNumberedCodeList.md` est aussi ouvert dans le smoke test Windows pour la régression de plantage signalée. Les tests UI natifs restent à ajouter avec des runners graphiques dédiés.

## Mémoire et performances

L’aperçu utilise un debounce afin de réduire les allocations de rendu. L’ouverture de dossier charge chaque document ouvert dans son onglet afin de garantir une édition sans perte lors du changement de sélection; prévoir un profilage dédié sur corpus volumineux avant d’augmenter cette stratégie. Le journal est borné et son échec n’interrompt jamais le flux d’édition.

## CI

La CI (`.github/workflows/ci.yml`) restaure en mode verrouillé, vérifie le formatage, compile et exécute les tests sous Windows, macOS et Linux, puis signale les vulnérabilités NuGet connues. Les tags de release (`.github/workflows/release.yml`) doivent correspondre à la version de `Directory.Build.props`; ils produisent les cinq RID déclarés dans `CONTRIBUTING.md` et un fichier SHA-256 par archive. L’installateur Windows valide également les fichiers du payload depuis le manifeste inclus avant toute copie.
