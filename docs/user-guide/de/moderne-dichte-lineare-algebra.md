# Moderne dichte lineare Algebra — Erweiterungen 2026

Das klassische Matrixkapitel bleibt gültig. Die Modernisierung ergänzt Zerlegungen, die besser zu heutigen Workloads passen.

## Householder-QR

Allgemeines und polynomiales Least Squares verwenden inzwischen Householder-QR statt Normalgleichungen. QR vermeidet die Bildung von `A^T*A` und macht eine praktische Vollrangdiagnostik sichtbar.

## Cholesky

Für eine symmetrisch positiv definite Matrix:

```csharp
var factorization = CholeskyFactorization.Decompose(a);
var x = factorization.Solve(b);
```

Die Faktorisierung speichert `A = L*L^T`, kann mehrere rechte Seiten ohne erneute Zerlegung lösen und bietet `LogDeterminant()` für skalierungsempfindliche Statistikaufgaben.

| Problemstruktur | Sinnvoller Ausgangspunkt |
| --- | --- |
| Allgemeines quadratisches dichtes System | Pivotierte LU |
| Symmetrisch positiv definites System | Cholesky |
| Quadratisches/hohes Least Squares mit vollem Rang | Householder-QR |
| Rangdefizientes oder unterbestimmtes Problem | Geplante SVD |

Die SVD ist der nächste große Dense-Linear-Algebra-Meilenstein, weil sie robuste Rangdiagnostik, Pseudoinversen und später PCA ermöglicht.
