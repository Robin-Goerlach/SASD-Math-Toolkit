# Modernisierung 2026

## Ziel

Der historische Numerical-Methods-Kompatibilitätskatalog ist jetzt ein abgeschlossenes Fundament und nicht mehr die Produktgrenze. Die nächste Phase entwickelt das SASD Math Toolkit zu einer modernen, wiederverwendbaren Numerikplattform für heutige SASD-Anwendungen weiter und behält gleichzeitig die verständliche, abhängigkeitfreie Referenzimplementierung bei.

Die Leitregel lautet: **Klassische Verfahren bleiben dort erhalten, wo sie weiterhin sinnvoll sind; der Funktionsumfang einer Bibliothek aus den 1980er-Jahren bestimmt aber nicht mehr, welche Mathematik das Projekt enthalten soll.**

## Architekturprinzipien

1. Numerisch stabilere Formulierungen haben Vorrang vor historisch einfachen Verfahren, wenn der öffentliche Vertrag kompatibel bleiben kann.
2. Korrektheit und Diagnose werden nicht auf einen nackten Rückgabewert reduziert. Rang, Residuum, Konvergenz, Konditionierung und Fehlerursache sind verschiedene Größen.
3. Verwaltete Referenzimplementierungen bleiben deterministisch und testbar. Optionale native oder beschleunigte Backends können später hinter stabilen Schnittstellen ergänzt werden.
4. Optimiert werden gemessene Hotspots und offensichtliche Allokations-/Lokalitätsprobleme. Unsafe-, SIMD- oder Parallel-Komplexität wird nicht ohne repräsentative Benchmarks eingebaut.
5. Besitzverhältnisse bleiben standardmäßig sicher. Performanceorientierte Buffer-APIs können ausdrücklich ergänzt werden, statt bestehende Verträge still aufzuweichen.
6. Sprachübergreifende Kompatibilität ist eine Spezifikationsaufgabe. C#, Fortran und spätere Implementierungen sollen gemeinsame Referenzvektoren und dokumentierte Konventionen teilen, statt Zeile für Zeile portiert zu werden.

## Modernisierungsreihenfolge

### M3.1 — Moderne dichte lineare Algebra und Regression

Dieser Bereich hat höchste Priorität, weil viele spätere Statistik-, Optimierungs- und Scientific-Features darauf aufbauen.

- **Householder-QR-Faktorisierung — implementiert**
- **Standardpfad für Least Squares von Normalgleichungen auf QR umgestellt — implementiert**
- Cholesky-/LDL-artige Faktorisierungen für symmetrisch positiv definite bzw. symmetrische Probleme
- SVD mit Singulärwerten, Rangdiagnostik, Pseudoinverser und robustem Least Squares
- Konditionsschätzer und explizite Rangdiagnostik
- Matrixnormen und weitere zerlegungsorientierte Hilfsfunktionen

Normalgleichungen sind nicht mehr der Standard für allgemeines lineares Least Squares, weil die Bildung von `A^T*A` die Konditionszahl quadriert. QR ist jetzt die robuste dichte Basis; SVD soll später rangdefiziente und unterbestimmte Fälle übernehmen.

### M3.2 — Dünnbesetzte lineare Algebra

- CSR-/CSC-Speicher
- Sparse-Matrix-Vektor-Multiplikation
- Conjugate Gradient für SPD-Systeme
- GMRES und BiCGSTAB für allgemeine Sparse-Systeme
- Preconditioner-Abstraktionen
- Residuen- und Abbruchdiagnostik passend zu den dichten Solver-Verträgen

### M3.3 — Optimierung und nichtlineare Systeme

- Brent-artige skalare Minimierung/Bracketing-Verfahren
- mehrdimensionale nichtlineare Gleichungssysteme
- Nelder-Mead für ableitungsfreie Optimierung
- BFGS/L-BFGS für glatte unbeschränkte Optimierung
- Line-Search-Infrastruktur und explizite Abbruchdiagnostik
- später Constraints, sobald ein konkreter SASD-Anwendungsfall sie benötigt

### M3.4 — Moderne ODE-Fähigkeiten

- Dormand-Prince-artiges adaptives Runge-Kutta
- Dense Output / Interpolation zwischen akzeptierten Schritten
- Event Detection und terminale Ereignisse
- wiederverwendbare Vektor-State-APIs mit weniger Allokationsdruck
- später steife Solver, wenn reale Workloads dies rechtfertigen

### M3.5 — Statistik, Zufall und Spezialfunktionen

Primär getrieben durch die SASD Statistical Workbench:

- numerisch stabile deskriptive Statistik und Online-Momente
- Wahrscheinlichkeitsverteilungen und Quantile
- Regressionsdiagnostik
- Bausteine für Hypothesentests
- deterministische Zufallsabstraktionen und reproduzierbare Streams
- Gamma-/Beta-/Fehlerfunktion und weitere für Verteilungen benötigte Spezialfunktionen
- PCA, sobald SVD vorhanden ist

### M3.6 — Geometrie, Transformationen und Simulation

Primär getrieben durch Game Toolkit / Grafik / Simulation:

- Vector3/Vector4 und Matrixtransformationen
- Quaternionen
- Kurven, Schnitte, Bounding Volumes und geometrische Prädikate
- FFT für weitere Längen als reine Zweierpotenzen, sofern benötigt
- mehrdimensionale Transformationen bei konkretem Bedarf

### M3.7 — Performance-Backends und Packaging

- BenchmarkDotNet-Suite mit repräsentativen Workloads
- optionale BLAS-/LAPACK-Adapter
- gezieltes SIMD erst nach Messungen
- Package-Metadaten, API-Kompatibilitätsprüfungen und Release-Automation

## Prioritäten der Verbraucher

**SASD Statistical Workbench** profitiert zuerst von QR, SVD, Statistik, Verteilungen, Regressionsdiagnostik, PCA und Optimierung.

**SASD Game Toolkit** profitiert zuerst von Vektoren, Transformationen, Quaternionen, Geometrie, deterministischem Random/Simulation und ausgewählter schneller linearer Algebra.

**Fully Encrypted** kann allgemeine Zahlentheorie-/Hilfsmathematik nutzen; das SASD Math Toolkit darf aber kein selbstgebauter Ersatz für auditierte kryptographische Primitive werden.

## Release-Strategie

Die klassische Kompatibilitätsimplementierung bleibt als dokumentierte Baseline bestehen. Die Modernisierung darf vor dem öffentlichen 1.0-Tag weiterlaufen; das abschließende repositoryweite Release-Audit muss aber gegen den tatsächlichen Release Candidate ausgeführt werden, damit auch die modernen APIs geprüft werden.

Das Projekt soll nicht warten, bis jede denkbare moderne Numerikfunktion existiert. Jeder Modernisierungsschritt muss für sich nutzbar, getestet, dokumentiert und releasefähig sein.
