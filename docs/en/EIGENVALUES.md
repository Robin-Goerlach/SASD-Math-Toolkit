# Eigenvalues and eigenvectors

This document describes the publication-quality C# reference implementation for the historical V1 eigenvalue/eigenvector catalog. The algorithms are independently implemented and deliberately remain readable reference methods rather than pretending to compete with LAPACK, sparse Krylov libraries or other production-scale eigensolver stacks.

## Result model and diagnostics

A real eigenpair satisfies

`A * v = lambda * v`.

`Eigenpair` is now an immutable value object. It owns a defensive copy of its vector, rejects non-finite public values, exposes a read-only `Components` view, and keeps the array-returning `Eigenvector` property as a defensive-copy compatibility API.

All iterative eigensolvers return `IterativeResult<T>`. Applications should inspect `Converged`/`Status`, `Iterations` and `Residual`; they should not assume that the presence of a numeric value means convergence was achieved.

`EigenSolvers.EigenpairResidualNorm(matrix, pair)` computes the independent equation defect

`||A*v - lambda*v||2`.

A small residual means that the pair satisfies the eigen-equation well in an absolute sense. It is not a condition-number estimate and does not prove that a sensitive or clustered eigenvalue is accurately located.

## Power method

`EigenSolvers.PowerMethod` approximates an eigenpair associated with the eigenvalue of largest magnitude. It uses a deterministic all-ones start vector unless the caller supplies one.

The iteration normalizes vectors with a scaled norm calculation. This avoids the avoidable overflow that a naive `sqrt(sum(x_i^2))` can suffer for valid finite vectors such as `[1e308, 1e308]`.

Convergence requires both the Rayleigh-quotient change and the eigenpair residual to meet the requested tolerance. The initial vector must contain a component in the desired dominant eigendirection; an application that knows the default vector may be orthogonal to that direction should supply its own start vector.

## Inverse power method

`EigenSolvers.InversePowerMethod` is the unshifted inverse iteration. It targets the eigenvalue nearest zero in magnitude, provided that the matrix is nonsingular and the start vector excites the corresponding eigendirection.

The matrix is LU-factorized once and the factorization is reused during all iterations. The optional `pivotTolerance` is passed explicitly to that factorization so an application can define what it considers numerically singular.

A singular or numerically singular matrix is therefore an input/numerical-condition failure and raises `ArithmeticException` during setup rather than being reported as ordinary iteration non-convergence.

Shifted inverse iteration is intentionally not hidden in this V1 API. A future shifted solver should expose the shift explicitly because it changes both the numerical problem and singularity behavior.

## Wielandt deflation

`WielandtDeflation.Create` implements the classical rank-one transformation

`B = A - v*x^T`

from a known right eigenpair. The implementation selects the largest-magnitude eigenvector component for the construction so the required division does not use an unnecessarily small component.

Deflation is meaningful only if the supplied pair actually approximates an eigenpair of `A`. The factory therefore validates `||A*v-lambda*v||2` against a configurable relative `residualTolerance` and exposes the accepted `SourceResidual` for diagnostics. Unrelated or inaccurate claimed eigenpairs are rejected instead of silently producing a misleading transformed matrix.

`EigenSolvers.WielandtSecondEigenpair` combines a dominant power iteration, validated deflation, a second power iteration and restoration to the original matrix. It is primarily a historical/reference workflow. Repeated or nearly repeated dominant eigenvalues can make restoration ill-conditioned; such cases are reported as `NumericalBreakdown` rather than hidden behind an unstable division.

## Cyclic Jacobi method

`EigenSolvers.CyclicJacobi` computes the complete eigensystem of a **real symmetric** matrix. Each sweep applies plane rotations to all off-diagonal pairs and accumulates the same orthogonal rotations in the eigenvector matrix.

The implementation now scales the quantities used to construct each Jacobi rotation before forming the tangent. This reduces avoidable overflow for large finite matrix entries. Frobenius and Euclidean norms also use scaled sum-of-squares calculations rather than directly squaring the largest values.

The result is a `SymmetricEigendecomposition`:

- eigenvalues are sorted in descending **numerical** order, not by absolute magnitude;
- eigenvectors are stored as columns and are orthonormal up to floating-point error;
- `GetEigenvalue(index)` avoids allocating an entire eigenvalue array;
- `GetEigenvector(index)` returns an independent vector copy;
- `GetEigenpair(index)` returns an immutable eigenpair;
- `Eigenvalues` and `Eigenvectors` remain defensive-copy convenience properties.

`IterativeResult.Residual` for cyclic Jacobi is the off-diagonal Frobenius norm of the current transformed matrix. It is therefore a convergence diagnostic for the Jacobi diagonalization, not the largest final `||A*v-lambda*v||2`. Call `EigenpairResidualNorm` on individual returned pairs when that diagnostic is required.

Symmetry is validated with a separate relative `symmetryTolerance`. A matrix outside that tolerance is rejected because the real symmetric Jacobi algorithm does not define a general nonsymmetric eigensolver.

## Repeated and clustered eigenvalues

Eigenvectors are never unique in sign: `v` and `-v` represent the same one-dimensional eigendirection. For repeated eigenvalues, any orthonormal basis of the repeated eigenspace can be valid. Tests and applications should therefore prefer residuals, orthogonality and subspace properties over component-by-component comparison with one arbitrary reference vector.

Small spectral gaps can also slow power iteration and make individual eigenvectors sensitive to perturbations. A small residual remains useful evidence that the returned pair satisfies the matrix equation, but it does not remove the underlying conditioning of the mathematical problem.

## Current scope

V1 intentionally contains the historical/reference methods: power, unshifted inverse power, Wielandt deflation/second eigenpair and cyclic Jacobi for real symmetric matrices. It does not yet provide a general nonsymmetric QR/Schur solver, complex general eigenpairs, shifted inverse iteration, sparse Lanczos/Arnoldi methods or condition-number estimates.

Those are future SASD extensions. They should be added behind explicit APIs without making the dependency-free reference implementation difficult to audit.
