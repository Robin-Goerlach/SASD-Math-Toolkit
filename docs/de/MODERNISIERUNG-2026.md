# Modernisierung 2026

Der historische Numerical-Methods-Kompatibilitätskatalog ist abgeschlossen. Die Bibliothek wird jetzt als Numerikplattform für 2026 weiterentwickelt.

## M3.1 — Moderne dichte lineare Algebra und Regression

- **Householder-QR-Faktorisierung — implementiert**
- **Least-Squares-Standardpfad von Normalgleichungen auf QR umgestellt — implementiert**
- **Cholesky-Faktorisierung für symmetrisch positiv definite Matrizen — implementiert**
- wiederverwendbare Cholesky-Lösungen sowie Determinanten-/Log-Determinanten-Diagnostik — implementiert
- SVD mit Singulärwerten, Rangdiagnostik, Pseudoinverser und robustem Least Squares — **nächster großer Meilenstein**
- Konditionsschätzer, Matrixnormen und weitere Zerlegungswerkzeuge

QR ist die robuste dichte Least-Squares-Basis. Cholesky liefert den strukturierten Pfad für SPD-Systeme. Die SVD soll rangdefiziente und unterbestimmte Fälle abdecken und später PCA ermöglichen.

## Weitere Modernisierung

Danach folgen Sparse Linear Algebra, Optimierung/nichtlineare Systeme, moderne ODE-Verfahren, Statistik/Zufall/Spezialfunktionen, Geometrie/Simulation sowie gemessene Performance-Backends.

Die deutschen und englischen LaTeX-/PDF-Handbücher werden an Release-Candidate-Punkten aus dem aktuellen Markdown neu erzeugt.
