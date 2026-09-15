# Singular value decomposition

## Purpose

`SingularValueDecomposition` is the modern dense fallback for problems where a simple full-rank QR or LU view is not enough. It supports arbitrary real dense matrix shapes and exposes the decomposition

`A = U * S * V^T`

with thin left/right singular-vector factors and singular values sorted from largest to smallest.

The implementation is a dependency-free managed reference algorithm. It uses a **one-sided Jacobi SVD**, not eigendecomposition of `A^T*A`. Avoiding the normal matrix matters because forming `A^T*A` squares the condition number and can destroy weak singular directions before rank analysis has even begun.

## Public diagnostics

The factorization exposes:

- singular values in descending order;
- `EstimatedRank`;
- `RankThreshold`;
- `ConditionNumber` and `ReciprocalConditionNumber` in the matrix 2-norm sense;
- the number of Jacobi sweeps used;
- thin left and right singular-vector matrices.

Rank is deliberately tolerance-dependent. A singular value is retained only when it is greater than

`largestSingularValue * relativeRankTolerance`.

This threshold is also used by least-squares solving and pseudoinverse construction so diagnostics and behavior remain consistent.

## Matrix shapes

The one-sided Jacobi kernel operates on square/tall matrices. Wide matrices are handled through the identity

`A^T = Ut * S * Vt^T`

which implies

`A = Vt * S * Ut^T`.

This gives the same singular values while swapping the roles of the thin left and right singular vectors. No normal equations are formed.

## Least squares and minimum norm

`SolveLeastSquares(b)` computes a truncated-SVD solution. For overdetermined systems it minimizes the residual norm. For underdetermined or rank-deficient systems it returns the minimum-Euclidean-norm solution associated with the configured rank threshold.

This is intentionally different from selecting an arbitrary set of independent columns or silently perturbing a singular system.

## Pseudoinverse

`PseudoInverse()` forms the Moore-Penrose pseudoinverse from retained singular triplets:

`A+ = V * S+ * U^T`.

Truncated singular directions contribute zero. The operation is useful for explicit diagnostics and algorithms that genuinely require a pseudoinverse; repeated solves should normally reuse the factorization through `SolveLeastSquares` instead of materializing `A+`.

## Numerical notes

- The Jacobi convergence criterion is based on normalized cross-correlation between working columns rather than raw dot products, which makes it scale-aware.
- Column norms use scaled sum-of-squares accumulation to avoid avoidable intermediate overflow/underflow.
- Singular-vector ownership is defensive at the public API boundary.
- Exact zero singular directions receive a deterministic orthonormal completion so the exposed thin `U` remains structurally useful even for exactly rank-deficient matrices.
- The managed algorithm favors auditability and robust diagnostics over peak throughput. Large production workloads can later use an optional LAPACK-class backend behind the same SASD concepts.

## Relationship to QR and Cholesky

SVD is not the cheapest first choice for every problem:

| Structure | Preferred factorization |
| --- | --- |
| General square system | pivoted LU |
| Symmetric positive-definite system | Cholesky |
| Square/tall full-column-rank least squares | Householder QR |
| Rank-deficient least squares | SVD |
| Underdetermined minimum-norm solve | SVD |
| Rank / 2-norm conditioning analysis | SVD |

The modernization goal is therefore a toolbox of complementary decompositions rather than one universal solver.
