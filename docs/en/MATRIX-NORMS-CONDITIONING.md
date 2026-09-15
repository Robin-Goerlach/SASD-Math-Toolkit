# Matrix norms and conditioning diagnostics

This note documents the scale and conditioning utilities added during the 2026 modernization of the dense linear-algebra layer.

## Why this exists

A solver returning a small residual does not prove that the computed solution is close to the exact mathematical solution. Ill-conditioned problems can amplify small perturbations in input data and rounding error. SASD Math Toolkit therefore exposes explicit matrix-scale and rank/conditioning diagnostics instead of hiding those questions inside individual solvers.

## Matrix norms

`MatrixNorms` provides:

- `MaxAbsoluteEntry(A)` — largest absolute entry;
- `OneNorm(A)` — maximum absolute column sum;
- `InfinityNorm(A)` — maximum absolute row sum;
- `FrobeniusNorm(A)` — square root of the sum of squared entries;
- `SpectralNorm(A)` — largest singular value.

The 1-, infinity- and Frobenius-norm implementations use scaled accumulation where useful so intermediate arithmetic does not overflow merely because entries are large. If the mathematically requested norm itself lies outside the finite `double` range, the method throws `ArithmeticException` rather than silently returning infinity.

`SpectralNorm` requires an SVD. Applications that also need rank, singular vectors or conditioning should create one `SingularValueDecomposition` and reuse it rather than asking for the spectral norm separately.

## Unified conditioning report

`MatrixConditionDiagnostics.Analyze(A)` combines the inexpensive entry-based norms with one SVD and reports:

- dimensions;
- maximum absolute entry;
- 1-, infinity-, Frobenius- and spectral norms;
- numerical rank;
- absolute and relative rank threshold;
- left and right nullity;
- 2-norm condition number;
- reciprocal 2-norm condition number.

The report deliberately separates rectangular full rank from null-space dimension. For example, a 2x3 matrix can have rank 2 and therefore be full rank in the rectangular sense while still having right nullity 1. That right null space is exactly why an underdetermined system can have infinitely many solutions.

## Rank threshold

The SVD rank rule remains

`singularValue > largestSingularValue * relativeRankTolerance`.

Changing the rank tolerance changes the numerical model of the problem. It is not a generic way to make a difficult matrix "pass". A direction truncated as numerical null space is also omitted by pseudoinverse and SVD least-squares operations.

## Condition number

For a numerically full-rank matrix, the reported 2-norm condition number is based on

`kappa2(A) = sigma_max / sigma_min`.

When the matrix is rank deficient at the configured threshold, `ConditionNumber2` is positive infinity and `ReciprocalConditionNumber2` is zero.

Condition number is a property of the problem representation, not a solver quality score. A stable algorithm can correctly report that the original problem is sensitive.

## Performance boundary

The entry-based norms are single-pass operations. SVD-based diagnostics are intentionally more expensive. `MatrixConditionDiagnostics.Analyze` performs only one SVD and reuses it for spectral norm, rank and conditioning so callers do not accidentally repeat the decomposition.

The managed implementation remains a readable reference path. Large production workloads may later use optional BLAS/LAPACK-backed implementations behind the same diagnostic semantics.
