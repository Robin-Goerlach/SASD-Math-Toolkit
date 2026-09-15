# Modernisierung 2026

Der historische Numerical-Methods-Kompatibilitätskatalog ist abgeschlossen. Die Bibliothek wird jetzt bewusst als Numerikplattform für 2026 weiterentwickelt.

## M3.1 — Moderne dichte lineare Algebra und Regression — Fundament abgeschlossen

- **Householder-QR-Faktorisierung — implementiert**
- **Least-Squares-Standardpfad von Normalgleichungen auf QR umgestellt — implementiert**
- **Cholesky-Faktorisierung für symmetrisch positiv definite Matrizen — implementiert**
- **wiederverwendbare Cholesky-Lösungen sowie Determinanten-/Log-Determinanten-Diagnostik — implementiert**
- **einseitige Jacobi-SVD für hohe, quadratische und breite dichte Matrizen — implementiert**
- **Singulärwerte, numerischer Rang, Pseudoinverse, 2-Norm-Konditionsdiagnostik und Minimalnorm-Least-Squares — implementiert**
- **Matrix-1-, Unendlich-, Frobenius- und Spektralnorm — implementiert**
- **gemeinsame Rang-, Nullitäts- und 2-Norm-Konditionsdiagnostik — implementiert**
- später: optionaler LAPACK-artiger Backend hinter stabilen SASD-Zerlegungskonzepten

QR bleibt für dichtes Least Squares mit vollem Spaltenrang der bevorzugte Weg, weil es günstiger als eine vollständige SVD ist. Cholesky liefert den strukturierten Pfad für SPD-Systeme. Die SVD übernimmt rangdefiziente und unterbestimmte Probleme und bildet zugleich die mathematische Basis für spätere PCA- und fortgeschrittene Regressionsdiagnostik.

Die aktuelle Managed-SVD ist bewusst eine nachvollziehbare einseitige Jacobi-Referenzimplementierung. Sie vermeidet `A^T*A`, verwendet skalierungsbewusste Spaltenorthogonalisierung und hält die Rangabschneidung explizit. Die Matrixdiagnostik verwendet dieselbe SVD gemeinsam für Spektralnorm, Rang, Nullität und Kondition, statt mehrfach identische Zerlegungen auszuführen.

Die dichte Referenzbasis ist breit genug, um in die nächste Modernisierungsschicht zu wechseln, ohne vorzugeben, bereits ein Hersteller-BLAS/LAPACK zu ersetzen. Weitere dichte Hilfen können bei konkretem Bedarf ergänzt werden.

## M3.2 — Dünnbesetzte lineare Algebra — in Arbeit

Das Speicher-/Arithmetikfundament ist jetzt implementiert:

- **kanonische unveränderliche CSR-Speicherung — implementiert**;
- **Koordinatenaufbau mit expliziter Aggregation doppelter Einträge — implementiert**;
- **validierte Raw-CSR- und Dense-Konvertierungsgrenzen — implementiert**;
- **Sparse-Matrix-Vektor-Multiplikation mit wiederverwendbaren Span-Puffern — implementiert**;
- **Transponieren ohne Dense-Materialisierung — implementiert**;
- **Sparse-Maximalwert-, 1-, Unendlich- und Frobenius-Norm — implementiert**;
- Conjugate Gradient für symmetrisch positiv definite dünnbesetzte Systeme — **als Nächstes**;
- CSC-Unterstützung, sobald dauerhaft spaltenorientierte Verbraucher eine eigene Darstellung rechtfertigen;
- später GMRES und BiCGSTAB für allgemeinere Systeme;
- Preconditioner-Abstraktionen erst dann, wenn der erste Solver sie tatsächlich benötigt.

Sparse-Solver werden die vorhandenen `IterationStatus`-, Residual- und Toleranzkonventionen wiederverwenden und kein zweites Konvergenzmodell einführen. Die CSR-API bietet bereits eine allokationsfreie Matrix-Vektor-Zielüberladung, damit iterative Solver ihre Arbeitsvektoren wiederverwenden können.

## M3.3 — Optimierung und nichtlineare Systeme

Brent-artige Verfahren, mehrdimensionale nichtlineare Systeme, Nelder-Mead, BFGS/L-BFGS und Line-Search-Infrastruktur.

## M3.4 — Moderne ODE-Fähigkeiten

Dormand-Prince, Dense Output, Event Detection, wiederverwendbare Vektor-State-APIs und später steife Solver.

## M3.5 — Statistik, Zufall und Spezialfunktionen

Stabile deskriptive Statistik, Verteilungen/Quantile, Regressionsdiagnostik, Hypothesentest-Bausteine, reproduzierbare Zufallsströme, Spezialfunktionen und **PCA auf Basis der SVD**.

## M3.6 — Geometrie, Transformationen und Simulation

Vector3/Vector4, Transformationen, Quaternionen, Kurven/Schnitte, geometrische Prädikate und breitere FFT-Unterstützung nach Bedarf der Verbraucher.

## M3.7 — Performance-Backends, Dokumentation und Packaging

BenchmarkDotNet, optionale BLAS-/LAPACK-Backends, gemessenes SIMD, Release-Automation und Neugenerierung der deutschen/englischen LaTeX-PDF-Handbücher an Release-Candidate-Punkten. Markdown bleibt die laufend gepflegte redaktionelle Quelle; LaTeX wird als Publikationsschicht synchronisiert und nicht als zweite unabhängige Textkopie gepflegt.
