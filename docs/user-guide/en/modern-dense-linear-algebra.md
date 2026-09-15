# Modern dense linear algebra — 2026 additions

The classical matrix chapter remains valid. The modernization layer adds decompositions better matched to current workloads.

## Householder QR

General and polynomial least squares now use Householder QR by default instead of normal equations. QR avoids forming `A^T*A` and exposes practical full-column-rank diagnostics.

## Cholesky

For a symmetric positive-definite matrix:

```csharp
var factorization = CholeskyFactorization.Decompose(a);
var x = factorization.Solve(b);
```

The factorization stores `A = L*L^T`, can solve several right-hand sides without refactorization, and provides `LogDeterminant()` for scale-sensitive statistical work.

| Problem structure | Preferred starting point |
| --- | --- |
| General square dense system | Pivoted LU |
| Symmetric positive-definite system | Cholesky |
| Square/tall full-rank least squares | Householder QR |
| Rank-deficient or underdetermined problem | Planned SVD |

SVD is the next major dense-linear-algebra milestone because it enables robust rank diagnostics, pseudoinverses and later PCA.
