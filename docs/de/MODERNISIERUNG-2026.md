# Modernisierung 2026

Der historische Numerical-Methods-Kompatibilitätskatalog ist abgeschlossen. Die Bibliothek wird jetzt bewusst als Numerikplattform für 2026 weiterentwickelt.

## M3.1 — Moderne dichte lineare Algebra und Regression

- **Householder-QR-Faktorisierung — implementiert**
- **Least-Squares-Standardpfad von Normalgleichungen auf QR umgestellt — implementiert**
- **Cholesky-Faktorisierung für symmetrisch positiv definite Matrizen — implementiert**
- **wiederverwendbare Cholesky-Lösungen sowie Determinanten-/Log-Determinanten-Diagnostik — implementiert**
- **einseitige Jacobi-SVD für hohe, quadratische und breite dichte Matrizen — implementiert**
- **Singulärwerte, numerischer Rang, Pseudoinverse, 2-Norm-Konditionsdiagnostik und Minimalnorm-Least-Squares — implementiert**
- Matrixnormen und weitere Zerlegungshilfen — nächster Aufräumschritt
- später: optionaler LAPACK-artiger Backend hinter stabilen SASD-Zerlegungskonzepten

QR bleibt für dichtes Least Squares mit vollem Spaltenrang der bevorzugte Weg, weil es günstiger als eine vollständige SVD ist. Cholesky liefert den strukturierten Pfad für SPD-Systeme. Die SVD übernimmt jetzt rangdefiziente und unterbestimmte Probleme und bildet zugleich die mathematische Basis für spätere PCA- und fortgeschrittene Regressionsdiagnostik.

Die aktuelle Managed-SVD ist bewusst eine nachvollziehbare einseitige Jacobi-Referenzimplementierung. Sie vermeidet `A^T*A`, verwendet skalierungsbewusste Spaltenorthogonalisierung und hält die Rangabschneidung explizit. Für große Hochdurchsatz-Workloads kann später ein optionaler etablierter nativer Backend ergänzt werden, ohne die höheren SASD-Verträge zu ändern.

## M3.2 — Dünnbesetzte lineare Algebra

CSR/CSC, Sparse-Matrix-Vektor-Multiplikation, Conjugate Gradient, GMRES, BiCGSTAB und Preconditioner.

## M3.3 — Optimierung und nichtlineare Systeme

Brent-artige Verfahren, mehrdimensionale nichtlineare Systeme, Nelder-Mead, BFGS/L-BFGS und Line-Search-Infrastruktur.

## M3.4 — Moderne ODE-Fähigkeiten

Dormand-Prince, Dense Output, Event Detection, wiederverwendbare Vektor-State-APIs und später steife Solver.

## M3.5 — Statistik, Zufall und Spezialfunktionen

Stabile deskriptive Statistik, Verteilungen/Quantile, Regressionsdiagnostik, Hypothesentest-Bausteine, reproduzierbare Zufallsströme, Spezialfunktionen und **PCA auf Basis der neuen SVD**.

## M3.6 — Geometrie, Transformationen und Simulation

Vector3/Vector4, Transformationen, Quaternionen, Kurven/Schnitte, geometrische Prädikate und breitere FFT-Unterstützung nach Bedarf der Verbraucher.

## M3.7 — Performance-Backends, Dokumentation und Packaging

BenchmarkDotNet, optionale BLAS-/LAPACK-Backends, gemessenes SIMD, Release-Automation und Neugenerierung der deutschen/englischen LaTeX-PDF-Handbücher an Release-Candidate-Punkten.
