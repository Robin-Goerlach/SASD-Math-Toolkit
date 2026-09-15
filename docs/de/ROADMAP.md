# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breite numerische Grundlage (historischer Katalog vollständig)

Implementiert sind Nullstellensuche, Polynom-/Horner-/Deflationsfunktionen, Interpolation/Splines, Differentiation, Integration, lineare Algebra/Eigenwerte, RK4/RKF45/Adams, Least Squares, FFT, Faltung/Korrelation und die Demo-Ebene.

Die Borland-inspirierte Übertragungsphase ist damit funktional abgeschlossen. Sie bleibt als Kompatibilitäts-/Referenzbaseline erhalten, bestimmt aber nicht mehr die zukünftige Produktgrenze.

## M2 – Veröffentlichungsqualität und Release-Audit

Der historische Kompatibilitätskatalog und alle elf geplanten Kapitel des Benutzerhandbuchs sind vollständig. Die fachbezogenen API-/Test-Audits, die gemeinsame Diagnostikdokumentation und eine pragmatische Performance-Runde sind für das klassische Fundament ebenfalls abgeschlossen.

Vor einem öffentlichen 1.0-Tag bleibt das abschließende repositoryweite Release-Audit erforderlich. Da die Modernisierung jetzt begonnen hat, soll dieses Audit gegen den tatsächlichen Release Candidate laufen, statt die Weiterentwicklung nur für ein Audit eines Zwischenstands einzufrieren.

## M3 – Modernisierung 2026 (in Arbeit)

Das Projekt entwickelt sich jetzt bewusst über den Funktionskatalog der 1980er-Jahre hinaus. Der detaillierte Plan steht in [`MODERNISIERUNG-2026.md`](MODERNISIERUNG-2026.md).

### M3.1 – Moderne dichte lineare Algebra und Regression (in Arbeit)

- Householder-QR-Faktorisierung: **implementiert**
- Allgemeines/Polynom-Least-Squares von Normalgleichungen auf QR umgestellt: **implementiert**
- Cholesky / symmetrische Faktorisierungen: als Nächstes
- SVD, Rangdiagnostik, Pseudoinverse, robustes Least Squares
- Konditionsschätzer und Matrixnormen

### M3.2 – Dünnbesetzte lineare Algebra

CSR/CSC, Sparse-Matvec, CG, GMRES, BiCGSTAB und Preconditioning.

### M3.3 – Optimierung und nichtlineare Systeme

Mehrdimensionale Nullstellen, Nelder-Mead, BFGS/L-BFGS, Line Search und explizite Diagnostik.

### M3.4 – Moderne ODE-Fähigkeiten

Dormand-Prince, Dense Output, Event Detection, allokationsärmere Vektor-State-APIs und später steife Solver bei echtem Bedarf.

### M3.5 – Statistik, Random und Spezialfunktionen

Stabile deskriptive Statistik, Verteilungen, Quantile, Regressionsdiagnostik, reproduzierbare Random-Streams, Spezialfunktionen und PCA nach SVD.

### M3.6 – Geometrie, Transformationen und Simulation

Vector3/4, Transformationen, Quaternionen, Kurven/Schnitte, breitere FFT-Unterstützung und Simulationsgrundlagen.

### M3.7 – Performance-Backends und Release Engineering

Benchmarks, optionale BLAS-/LAPACK-Adapter, gemessenes SIMD, Package-/API-Kompatibilitätschecks und Release-Automation.

## M4 – Weitere Sprachimplementierungen

Äquivalente Sprachimplementierungen dürfen bei Bedarf parallel voranschreiten. Gemeinsames Verhalten soll zunehmend in maschinenlesbare `spec/`-Referenzvektoren und Cross-Language-Konformitätstests überführt werden.

## M5 – Optionale High-Performance-Backends

Adapter für etablierte Bibliotheken werden hinter stabilen SASD-Schnittstellen bewertet. Die abhängigkeitfreie Managed-Referenzimplementierung bleibt für Portabilität, Diagnostik und Lehre erhalten.
