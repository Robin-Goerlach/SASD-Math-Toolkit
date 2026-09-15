# Modernization 2026

The historical Numerical Methods compatibility catalog is now a completed foundation, not the future product boundary. The guiding rule is: **keep the classical algorithms where they remain useful, but stop letting a 1980s catalog decide what mathematics the library should contain.**

## M3.1 — Modern dense linear algebra and regression

- **Householder QR factorization — implemented**
- **Least-squares default path migrated from normal equations to QR — implemented**
- **Cholesky factorization for symmetric positive-definite matrices — implemented**
- reusable vector/multi-RHS Cholesky solves and determinant/log-determinant diagnostics — implemented
- SVD with singular values, rank diagnostics, pseudoinverse, and robust least squares — **next major milestone**
- condition-number estimates and explicit rank diagnostics
- matrix norms and additional decomposition utilities

QR is now the baseline robust dense least-squares solver. Cholesky provides the structured path for SPD systems. SVD will handle rank-deficient and underdetermined cases and later support PCA.

## M3.2 — Sparse linear algebra

CSR/CSC storage, sparse matrix-vector multiplication, Conjugate Gradient, GMRES, BiCGSTAB and preconditioners.

## M3.3 — Optimization and nonlinear systems

Brent-style methods, multidimensional nonlinear systems, Nelder-Mead, BFGS/L-BFGS and line-search infrastructure.

## M3.4 — Modern ODE capabilities

Dormand-Prince, dense output, event detection, reusable vector-state APIs and later stiff solvers.

## M3.5 — Statistics, random, and special functions

Stable descriptive statistics, distributions/quantiles, regression diagnostics, hypothesis-test building blocks, reproducible random streams, special functions and PCA after SVD.

## M3.6 — Geometry, transforms, and simulation

Vector3/Vector4, transforms, quaternions, curves/intersections, geometric predicates and broader FFT support as consumers require it.

## M3.7 — Performance backends, documentation, and packaging

BenchmarkDotNet, optional BLAS/LAPACK backends, measured SIMD, release automation, and regeneration of the German/English LaTeX PDF handbooks at release-candidate boundaries.
