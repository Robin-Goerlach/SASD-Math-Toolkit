# SASD Math Toolkit – C++-Dokumentation

Dieses Verzeichnis dokumentiert die eigenständige moderne C++-Ausgabe des SASD
Math Toolkit. Sie lässt sich ohne das C#/.NET-Handbuch verstehen und bauen. Die
.NET-Implementierung bleibt während der Entwicklung die mathematische Referenz,
ist aber keine Quellcodevorlage.

## Aktueller Stand

CPP-M3 erweitert die Bibliothek auf Version `0.4.0`:

- C++20- und CMake-Projektstruktur;
- öffentlicher Root-Namespace `sasd::numerics`;
- Iterationsstatus, Ergebnisobjekt und numerische Standardwerte;
- explizite serielle, automatische und parallele CPU-Ausführung;
- CTest-Tests, ausführbares Beispiel und Installations-Smoke-Test;
- installierbares CMake-Paket `SasdMathToolkit`;
- Doxygen-kompatible Kommentare und optionales API-Ziel;
- deskriptive Statistik mit stabilem Mittelwert, Median und Populations-/Stichprobenvarianz;
- deterministische Mehrkernreduktionen für ausreichend große Mittelwert-/Varianzeingaben;
- unveränderliche komplexfähige Polynome mit Horner-Auswertung und Deflation;
- skalare, komplexe und polynomspezifische Nullstellensolver mit Diagnostik;
- lineare, Lagrange- und Newton-Interpolation sowie unveränderliche natürliche und eingespannte kubische Splines;
- steuerbare parallele Batch-Auswertung für unabhängige Interpolationsziele.

Ein vollständiger numerischer Funktionskatalog wird noch nicht behauptet. C++
erreicht 1.0 erst nach Abschluss des definierten Funktionsumfangs und der
sprachübergreifenden Konformitätsprüfungen.

## Dokumente

- [Architektur](ARCHITECTURE.md)
- [Build, Tests und Installation](BUILDING.md)
- [Parallele Ausführung](PARALLELISM.md)
- [Deskriptive Statistik](STATISTIK.md)
- [Polynome und Nullstellensuche](NULLSTELLENSUCHE.md)
- [Interpolation und kubische Splines](INTERPOLATION.md)
- [Deutsches C++-Benutzerhandbuch](../../user-guide/cpp/de/README.md)
- [Englische C++-Dokumentation](../en/README.md)

## Trennungsregel

C++-Code, Tests, Beispiele, Buildlogik und Handbücher liegen ausschließlich in
ihren C++-Bäumen. Sprachneutrales Verhalten gehört nach `spec/`, gemeinsame
Referenzdaten nach `conformance/`. Fortran- und JavaScript-Produktionscode wird
erst begonnen, wenn C++ als zweite Implementierung ausreichend ausgereift ist.
