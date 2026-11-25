# Documentation layout

Documentation is separated by implementation language. A reader must never
need to infer whether a code example is C#, C++, Fortran or JavaScript.

| Area | Purpose |
| --- | --- |
| `dotnet/{en,de}` | .NET implementation and API notes |
| `cpp/{en,de}` | C++ implementation and API notes |
| `user-guide/<language>/{en,de}` | Language-specific Markdown handbooks |
| `user-guide/LaTeX/<language>` | Independent PDF build for one implementation |
| `shared/{en,de}` | Project-wide decisions and migration records |
| `../spec` | Language-neutral mathematical contracts |
| `../conformance` | Shared machine-readable reference cases |

Each published handbook is a standalone document. Mathematical explanations
that are genuinely identical may be composed from a shared editorial source,
but language-specific API descriptions, build instructions and code examples
remain in the corresponding language tree. Shared text is extracted gradually
when a second implementation of a domain is written; existing manuals are not
mass-moved without a matching implementation and review.

The intended publication pipeline is therefore:

1. language-neutral mathematical contract and common prose;
2. implementation-specific API text and examples;
3. one language-specific LaTeX manifest;
4. one independently readable PDF per implementation and natural language.

# Dokumentationsstruktur

Die Dokumentation ist nach Implementierungssprachen getrennt. Codebeispiele,
API-Beschreibungen und Build-Anleitungen liegen immer im Sprachverzeichnis.
Fachlich identische Mathematiktexte dürfen aus einer gemeinsamen redaktionellen
Quelle eingebunden werden; jedes erzeugte Handbuch und PDF bleibt trotzdem
eigenständig und vollständig lesbar.

Gemeinsame Kapitel werden erst dann herausgelöst, wenn die entsprechende
Funktion in einer zweiten Sprache implementiert und der Text gemeinsam geprüft
wurde. Dadurch vermeiden wir sowohl divergierende Kopien als auch eine riskante
Komplettumsortierung der bestehenden Handbücher.
