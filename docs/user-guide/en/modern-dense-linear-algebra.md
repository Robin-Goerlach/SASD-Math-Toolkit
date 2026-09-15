# Modern dense linear algebra — 2026 additions

The classical matrix chapter remains valid. The modernization layer adds decompositions better matched to current workloads and keeps the choice of algorithm tied to matrix structure rather than treating every problem as generic Gaussian elimination.

## Householder QR

General and polynomial least squares prefer Householder QR instead of normal equations. QR avoids forming `A^T*A`, does not square the condition number merely to solve the fit, and exposes practical full-column-rank diagnostics.

```csharp
var qr = QrFactorization.Decompose(design);
if (qr.IsFullColumnRank)
{
    var coefficients = qr.SolveLeastSquares(observations);
}
```

For ordinary square/tall full-rank regression, QR remains the preferred starting point because it is cheaper than computing a complete SVD.

## Cholesky

For a symmetric positive-definite matrix:

```csharp
var factorization = CholeskyFactorization.Decompose(a);
var x = factorization.Solve(b);
```

The factorization stores `A = L*L^T`, can solve several right-hand sides without refactorization, and provides `LogDeterminant()` for scale-sensitive statistical work.

Cholesky should not be forced onto a merely symmetric or arbitrary matrix. Positive definiteness is part of the mathematical contract, not an implementation detail.

## Singular value decomposition

The SVD handles the cases where rank and conditioning are themselves part of the problem:

```csharp
var svd = SingularValueDecomposition.Decompose(a);

Console.WriteLine(svd.EstimatedRank);
Console.WriteLine(svd.ConditionNumber);

var minimumNorm = svd.SolveLeastSquares(b);
var pseudoInverse = svd.PseudoInverse();
```

For an `m x n` matrix the toolkit exposes thin factors satisfying

```text
A = U * S * V^T
```

with `min(m,n)` singular values sorted from largest to smallest.

The managed reference implementation uses a one-sided Jacobi SVD. It deliberately avoids obtaining the decomposition from `A^T*A`; the normal matrix would square the condition number and can erase weak singular directions that are exactly what SVD is supposed to diagnose.

### Numerical rank is tolerance-dependent

`EstimatedRank` is based on

```text
singularValue > LargestSingularValue * RelativeRankTolerance
```

The resulting absolute value is available as `RankThreshold`. This is a numerical decision. A mathematically nonzero singular value may intentionally be discarded when it is too small relative to the dominant scale to support a stable solution.

The same threshold controls `SolveLeastSquares` and `PseudoInverse`, so diagnostics and computation do not disagree about which directions are retained.

### Rank-deficient least squares

For a rank-deficient or underdetermined problem, `SolveLeastSquares` returns the minimum-Euclidean-norm solution associated with the retained singular directions. This is often more meaningful than choosing an arbitrary subset of columns.

For example:

```csharp
var a = new DenseMatrix(new double[,]
{
    { 1.0, 0.0, 1.0 },
    { 0.0, 1.0, 1.0 }
});

var svd = SingularValueDecomposition.Decompose(a);
var x = svd.SolveLeastSquares(new[] { 1.0, 1.0 });
```

The system has infinitely many exact solutions. The SVD returns the minimum-norm solution, approximately `(1/3, 1/3, 2/3)`.

### Pseudoinverse

`PseudoInverse()` forms the Moore-Penrose pseudoinverse using the same truncation threshold. It is useful when the matrix itself is genuinely required by a later algorithm. For repeated right-hand sides, reuse the SVD and call `SolveLeastSquares` instead of materializing the pseudoinverse each time.

### Condition number

For a numerically full-rank matrix, `ConditionNumber` reports the ratio of largest to smallest singular value and therefore the matrix 2-norm condition number. If the matrix is numerically rank deficient at the configured threshold, the reported condition number is positive infinity and `ReciprocalConditionNumber` is zero.

A large condition number means that small input perturbations can cause much larger changes in the solution. It is not a statement about whether the implementation is “good” or “bad”; it is a property of the numerical problem.

## Least-squares routing in the toolkit

The general `LeastSquares.FitBasis` API now chooses its dense solver deliberately:

1. square/tall full-column-rank design: Householder QR;
2. rank-deficient or underdetermined design: SVD minimum-norm solution.

Named models can retain stricter identifiability rules. For example, the five-term Fourier helper still rejects a phase design that cannot identify all five named coefficients instead of silently returning one of infinitely many equivalent coefficient vectors.

## Method selection

| Problem structure | Preferred starting point |
| --- | --- |
| General square dense system | Pivoted LU |
| Symmetric positive-definite system | Cholesky |
| Square/tall full-rank least squares | Householder QR |
| Rank-deficient least squares | SVD |
| Underdetermined minimum-norm system | SVD |
| Numerical rank / 2-norm conditioning | SVD |
| Explicit pseudoinverse required | SVD |
| Very large dense production workload | Later optional BLAS/LAPACK-class backend |

These methods are complementary. SVD is the most diagnostic of the three modern decompositions, but it is not the cheapest solution to every well-structured system.

## Performance perspective

The current QR, Cholesky and SVD implementations are managed, dependency-free reference algorithms. They avoid obvious unnecessary work and dangerous numerical formulations, but they are not intended to beat tuned vendor BLAS/LAPACK on very large matrices. The SASD strategy is to keep these implementations deterministic and auditable, benchmark real workloads, and later add optional accelerated backends behind stable concepts rather than replacing the reference layer.
