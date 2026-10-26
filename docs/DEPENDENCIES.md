# Dépendances et licences

Les versions directes sont centralisées dans `Directory.Packages.props`; les graphes résolus sont verrouillés dans les fichiers `packages.lock.json` de chaque projet.

| Dépendance | Usage | Licence / vérification |
|---|---|---|
| Avalonia et packages Avalonia | UI native multiplateforme | MIT (vérifier les avis des paquets transitifs au moment de chaque release) |
| AvaloniaEdit | édition professionnelle, numéros de ligne et moteur de coloration | MIT |
| Markdig | analyse CommonMark et extensions avancées GFM du Markdown | BSD-2-Clause |
| HtmlAgilityPack | conversion sûre de la structure HTML Markdig en contrôles Avalonia | MIT |
| PDFsharp-MigraDoc | génération et mise en page PDF locale | MIT (version 6.2.4) |
| Microsoft.Extensions.Logging.Abstractions | dépendance transitive PDFsharp | MIT |
| System.Security.Cryptography.Pkcs | dépendance transitive PDFsharp | MIT; version 8.0.2 épinglée pour inclure les correctifs de sécurité |
| xUnit et Microsoft.NET.Test.Sdk | tests automatisés | licences du paquet NuGet épinglé |
| Tmds.DBus.Protocol | intégration desktop Linux transitive/explicite | version corrigée épinglée à `0.21.3`; licence/avis à conserver avec l’archive de distribution |

Avant une distribution publique, vérifier les licences de toutes les dépendances transitives (`dotnet list Marginalia.slnx package --include-transitive`) et inclure les notices requises. Aucun paquet ne doit être mis à jour sans restore, tests, audit de vulnérabilités et revue de licence.
