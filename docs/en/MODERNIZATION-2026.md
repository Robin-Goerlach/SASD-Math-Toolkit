# Modernization 2026

## Purpose

The historical Numerical Methods compatibility catalog is now a completed foundation, not the future product boundary. The next phase evolves SASD Math Toolkit into a modern reusable numerical platform for current SASD applications while preserving the readable dependency-free reference implementation.

The guiding rule is: **keep the classical algorithms where they remain useful, but stop letting a 1980s catalog decide what mathematics the library should contain.**

## Architecture principles

1. Prefer numerically stable formulations over historically simple ones when the public contract can remain compatible.
2. Separate correctness/diagnostics from raw scalar return values. Rank, residual, convergence, conditioning, and failure mode are different concepts.
3. Keep managed reference implementations deterministic and testable. Optional native or accelerated backends may be added later behind stable abstractions.
4. Optimize measured hot paths and obvious allocation/locality problems, but do not introduce unsafe/SIMD/parallel complexity without representative benchmarks.
5. Preserve caller ownership by default. Performance-oriented buffer APIs may be added explicitly rather than weakening existing APIs silently.
6. Treat cross-language behavior as a specification problem. C#, Fortran, and future implementations should converge on shared reference vectors and documented conventions rather than line-by-line ports.

## Modernization sequence

### M3.1 — Modern dense linear algebra and regression

This is the first priority because many later statistical, optimization, and scientific features depend on it.

- **Householder QR factorization — implemented**
- **Least-squares default path migrated from normal equations to QR — implemented**
- Cholesky/LDL-style factorizations for symmetric positive-definite / symmetric problems
- SVD with singular values, rank diagnostics, pseudoinverse, and robust least squares
- condition-number estimates and explicit rank diagnostics
- matrix norms and additional decomposition-oriented utilities

Normal equations are no longer the default general linear least-squares engine because forming `A^T*A` squares the condition number. QR is now the baseline robust dense solver; SVD will handle rank-deficient and underdetermined cases later.

### M3.2 — Sparse linear algebra

- CSR/CSC storage
- sparse matrix-vector multiplication
- conjugate gradient for SPD systems
- GMRES and BiCGSTAB for general sparse systems
- preconditioner abstractions
- residual and stopping diagnostics consistent with the dense solver contracts

### M3.3 — Optimization and nonlinear systems

- Brent-style scalar minimization/root bracketing where appropriate
- multidimensional nonlinear root systems
- Nelder-Mead for derivative-free optimization
- BFGS/L-BFGS for smooth unconstrained optimization
- line-search infrastructure and explicit termination diagnostics
- later constrained optimization when a concrete SASD consumer requires it

### M3.4 — Modern ODE capabilities

- Dormand-Prince-style adaptive Runge-Kutta
- dense output / interpolation between accepted steps
- event detection and terminal events
- reusable vector-state APIs with reduced allocation pressure
- later stiff solvers when real workloads justify them

### M3.5 — Statistics, random, and special functions

Driven primarily by SASD Statistical Workbench:

- numerically stable descriptive statistics and online moments
- probability distributions and quantiles
- regression diagnostics
- hypothesis-test building blocks
- deterministic random-number abstractions and reproducible streams
- Gamma/Beta/error-function foundations and other special functions required by distributions
- PCA once SVD is available

### M3.6 — Geometry, transforms, and simulation foundations

Driven primarily by Game Toolkit / graphics / simulation:

- Vector3/Vector4 and matrix transforms
- quaternions
- curves, intersections, bounding volumes, and geometric predicates
- FFT support beyond power-of-two lengths where needed
- multidimensional transforms when justified by consumers

### M3.7 — Performance backends and packaging

- BenchmarkDotNet benchmark suite for representative workloads
- optional BLAS/LAPACK backend adapters
- selective SIMD only after measurement
- package metadata, API compatibility checks, and release automation

## Consumer priorities

**SASD Statistical Workbench** benefits first from QR, SVD, statistics, distributions, regression diagnostics, PCA, and optimization.

**SASD Game Toolkit** benefits first from vectors, transforms, quaternions, geometry, deterministic random/simulation support, and selected fast linear algebra.

**Fully Encrypted** may use generic number-theory/support mathematics, but SASD Math Toolkit must not become a home-grown replacement for audited cryptographic primitives.

## Release strategy

The classical compatibility implementation remains a documented baseline. Modernization may continue before the public 1.0 tag, but the final repository-wide release audit must be executed against the actual release candidate so it covers the modern APIs too.

The project should not wait until every possible modern numerical feature exists. Each modernization slice must be independently useful, tested, documented, and releasable.
