# Contribuer

## Prérequis

- .NET SDK 10.0.100 ou plus récent dans la ligne .NET 10 (résolu par `global.json`).
- Git et accès à NuGet pour restaurer les dépendances.

## Flux de travail

1. Créer une branche à partir de `main`.
2. Faire un changement ciblé, avec tests pour les comportements modifiés.
3. Exécuter `dotnet restore Marginalia.slnx --locked-mode`, `dotnet format Marginalia.slnx --verify-no-changes`, `dotnet build Marginalia.slnx -c Release` et `dotnet test Marginalia.slnx -c Release`.
4. Vérifier `git diff --check`, mettre à jour `CHANGELOG.md` pour tout changement utilisateur, puis ouvrir une pull request.

Les pull requests doivent passer les checks GitHub Actions. Ne jamais inclure documents utilisateur, secrets, binaires de build ou journaux locaux.

## Version et release

La version suit SemVer, sa source est `Directory.Build.props` et les changements sont décrits dans `CHANGELOG.md`. Les tags bêta `vX.Y.Z-beta.N` publient Windows et Linux (avec des `.deb` pour Debian/Ubuntu) et sont marqués préversions; les tags stables `vX.Y.Z` publient les cinq RID. Les clés de signature/notarisation ne sont pas stockées dans le dépôt; la signature pourra être activée par secrets CI dédiés.
