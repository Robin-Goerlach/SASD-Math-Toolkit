# Modernization 2026

The historical Numerical Methods compatibility catalog is now a completed foundation, not the future product boundary. The guiding rule is: **keep the classical algorithms where they remain useful, but stop letting a 1980s catalog decide what mathematics the library should contain.**

## M3.1 — Modern dense linear algebra and regression — foundation complete

- **Householder QR factorization — implemented**
- **Least-squares default path migrated from normal equations to QR — implemented**
- **Cholesky factorization for symmetric positive-definite matrices — implemented**
- **reusable vector/multi-RHS Cholesky solves and determinant/log-determinant diagnostics — implemented**
- **one-sided Jacobi SVD for tall, square and wide dense matrices — implemented**
- **singular values, numerical rank, pseudoinverse, 2-norm condition diagnostics and minimum-norm least squares — implemented**
- **matrix 1-, infinity-, Frobenius- and spectral norms — implemented**
- **combined numerical-rank, nullity and 2-norm conditioning diagnostics — implemented**
- later: optional LAPACK-class backend behind stable SASD decomposition concepts

QR remains the preferred full-column-rank dense least-squares path because it is cheaper than a complete SVD. Cholesky provides the structured path for SPD systems. SVD is the rank-aware fallback for rank-deficient and underdetermined problems and establishes the mathematical base required by later PCA and advanced regression diagnostics.

The current managed SVD is intentionally a readable one-sided Jacobi reference implementation. It avoids `A^T*A`, uses scale-aware column orthogonalization, and keeps rank truncation explicit. The matrix-diagnostic layer reuses that SVD for spectral norm, rank, nullity and condition reporting instead of repeating decompositions.

The dense reference foundation is now broad enough to support the next modernization layer without pretending to be a vendor BLAS/LAPACK replacement. Additional dense utilities can be added when concrete consumers require them.

## M3.2 — Sparse linear algebra — in progress

The storage/arithmetic foundation is now implemented:

- **canonical immutable CSR storage — implemented**;
- **coordinate assembly with explicit duplicate aggregation — implemented**;
- **validated raw-CSR and dense conversion boundaries — implemented**;
- **sparse matrix-vector multiplication with reusable span buffers — implemented**;
- **transpose without dense materialization — implemented**;
- **sparse max-entry, 1-, infinity- and Frobenius norms — implemented**;
- Conjugate Gradient for symmetric positive-definite sparse systems — **next**;
- CSC support when sustained column-oriented consumers justify a dedicated representation;
- later GMRES and BiCGSTAB for more general systems;
- preconditioner abstractions only when the first solver needs them.

Sparse solver diagnostics will reuse the existing `IterationStatus`, residual and tolerance conventions rather than introducing a second convergence model. The CSR API already exposes an allocation-free matrix-vector destination overload specifically so iterative solvers can reuse work vectors.

## M3.3 — Optimization and nonlinear systems

Brent-style methods, multidimensional nonlinear systems, Nelder-Mead, BFGS/L-BFGS and line-search infrastructure.

## M3.4 — Modern ODE capabilities

Dormand-Prince, dense output, event detection, reusable vector-state APIs and later stiff solvers.

## M3.5 — Statistics, random, and special functions

Stable descriptive statistics, distributions/quantiles, regression diagnostics, hypothesis-test building blocks, reproducible random streams, special functions and **PCA on the SVD foundation**.

## M3.6 — Geometry, transforms, and simulation

Vector3/Vector4, transforms, quaternions, curves/intersections, geometric predicates and broader FFT support as consumers require it.

## M3.7 — Performance backends, documentation, and packaging

BenchmarkDotNet, optional BLAS/LAPACK backends, measured SIMD, release automation, and regeneration of the German/English LaTeX PDF handbooks at release-candidate boundaries. Markdown remains the continuously maintained editorial source; LaTeX is synchronized as the publication layer rather than maintained as a second independent copy of the prose.
