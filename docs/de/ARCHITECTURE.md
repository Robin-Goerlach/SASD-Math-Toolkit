# Architektur

## Ziel

Das SASD Math Toolkit soll eine langfristig wiederverwendbare mathematische Grundlage werden. V1 orientiert sich funktional an der historischen Numerical-Methods-Sammlung, die Architektur wird aber nicht an alte Pascal-Dateien oder Kapitelstrukturen gebunden.

## Repository-Aufbau

- `spec/`: sprachneutrale Verträge, Referenzfälle und spätere Cross-Language-Konformität.
- `src/dotnet/`: erste Implementierung in C#/.NET 10.
- `tests/dotnet/`: automatisierte numerische Tests.
- `samples/dotnet/`: Beispiele und später Demonstrationsprogramme.
- `docs/en/`: primäre technische Dokumentation.
- `docs/de/`: deutsche Dokumentation.

Spätere Implementierungen kommen parallel hinzu, z. B. `src/cpp`, `src/fortran`, `src/java` und `src/javascript`. Dadurch bleibt kein Sprach-Frontend der architektonische Mittelpunkt des Gesamtprojekts.

## .NET-Struktur

Das Assembly/Package heißt `Sasd.Math.Toolkit`; der Namespace beginnt bewusst mit `Sasd.Numerics`, damit es keine unnötige Namenskollision mit `System.Math` gibt.

Die fachlichen Bereiche sind getrennte Namespaces für Root Finding, Interpolation, Differentiation, Integration, lineare Algebra, Differentialgleichungen, Approximation, Transformationen und Geometrie.

## Fehler- und Konvergenzmodell

Ungültige Aufrufparameter führen zu normalen .NET-Exceptions. Erwartbare numerische Ergebnisse wie „nicht konvergiert“, „Iterationslimit erreicht“ oder „numerischer Zusammenbruch“ werden dagegen als Status im Ergebnis zurückgegeben. So werden numerische Eigenschaften nicht als Programmierfehler behandelt.

## Numerische Leitlinien

- Toleranzen sind explizit und nicht als veränderbarer globaler Zustand versteckt.
- Robuste Varianten wie Partial Pivoting sind Standard, auch wenn Kompatibilitätsvarianten zusätzlich angeboten werden.
- Verständlichkeit und Testbarkeit gehen zunächst vor Mikrooptimierung.
- Für große/HPC-Probleme können später optionale BLAS/LAPACK-Backends angebunden werden.
