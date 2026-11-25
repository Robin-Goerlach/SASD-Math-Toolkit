# Householder QR factorization

## Role in the modernized toolkit

QR is the first deliberately post-compatibility numerical foundation added after the classical Borland-inspired catalog was completed. It addresses a concrete weakness of the original least-squares implementation: normal equations are simple, but forming `A^T*A` squares the condition number.

`QrFactorization` therefore becomes the default dense least-squares foundation before the later SVD milestone.

## Contract

`QrFactorization.Decompose(A)` currently accepts square or tall finite dense matrices (`rows >= columns`). It stores Householder reflectors in packed row-major form plus the diagonal of `R`.

The public factorization exposes:

- `Rows` and `Columns`
- `RelativeRankTolerance`
- `EstimatedRank`
- `IsFullColumnRank`
- `UpperTriangularFactor`
- `ThinOrthogonalFactor`
- `SolveLeastSquares(b)`

The thin orthogonal factor has dimensions `m x n`; the upper factor is `n x n`.

## Numerical choices

Householder reflections are preferred over classical Gram-Schmidt because they preserve orthogonality substantially better for general dense matrices.

Column norms are accumulated with a scaled sum-of-squares algorithm instead of naïve `sqrt(sum(x*x))`. This avoids avoidable overflow and underflow while computing reflector norms.

Rank estimation compares each `|R(i,i)|` with a relative threshold based on the largest diagonal magnitude. This is intentionally described as an estimate. It is useful for refusing clearly rank-deficient triangular solves but is not equivalent to singular-value analysis.

## Least-squares solve

For full-column-rank `A`, the solver computes the minimizer of

`||A*x - b||2`

without constructing `Q` explicitly:

1. replay the packed reflectors to form `Q^T*b`;
2. solve the leading upper-triangular system with `R`;
3. return the `n` coefficients.

The unused tail of `Q^T*b` contains orthogonal residual coordinates.

## Deliberate boundaries

The first QR milestone does not pretend to solve every linear least-squares problem.

- underdetermined matrices (`m < n`) are rejected;
- rank-deficient least squares is rejected at the configured rank tolerance;
- pseudoinverse solutions are deferred to SVD;
- condition numbers are not inferred from the QR diagonal alone.

Those boundaries prevent the API from silently returning an arbitrary answer where stronger diagnostics are required.

## Performance

The factorization stores one packed row-major work array and the `R` diagonal. Least-squares solves reuse those reflectors and do not allocate a full `m x m` `Q`. `ThinOrthogonalFactor` is materialized only when explicitly requested for diagnostics or downstream algorithms.

This remains a managed reference implementation. Blocking, native BLAS/LAPACK, SIMD, and parallel kernels remain benchmark-driven future work.
