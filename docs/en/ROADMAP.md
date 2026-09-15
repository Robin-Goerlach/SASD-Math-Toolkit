# Roadmap

## M0 — Repository foundation (done)

- Language-neutral repository layout
- .NET 10 class library, tests, sample application and CI
- English and German documentation roots
- Clean-room policy
- Common iteration status/result conventions

## M1 — Broad numerical foundation (historical catalog complete)

Implemented across the major domains: scalar and complex roots, polynomial/Horner/deflation support, interpolation/splines, differentiation, integration, dense linear algebra/eigenvalues, RK4/RKF45/Adams ODE integration, least squares, radix-2 FFT, convolution/correlation and the demonstration layer.

The historical Borland-inspired feature-transfer phase is complete. It remains a compatibility/reference baseline rather than the future scope boundary.

## M2 — Publication quality and release audit (release-candidate audit in progress)

The historical compatibility catalog and all eleven classical user-handbook chapters are complete. Publication-quality API/test audits, cross-cutting diagnostic documentation, and a pragmatic performance pass are also complete for the classical foundation.

The repository-wide audit now targets the real modernized `1.0.0-rc.1` candidate. Package metadata, package/symbol generation, package-content checks, sample smoke execution and current GitHub Actions runtime majors are part of the normal CI gate. The remaining release gates are LaTeX/PDF synchronization and visual inspection, followed by a final Windows acceptance run against the exact candidate commit. No public final 1.0 tag is created before those gates pass.

See [`RELEASE-AUDIT.md`](RELEASE-AUDIT.md) for the audit record.

## M3 — Modernization 2026 (in progress)

The project now evolves beyond the 1980s feature catalog. The detailed plan is in [`MODERNIZATION-2026.md`](MODERNIZATION-2026.md).

### M3.1 — Modern dense linear algebra and regression (foundation complete)

- Householder QR factorization: **implemented**
- General/polynomial least squares migrated from normal equations: **implemented**
- Cholesky factorization, reusable solves and log-determinant: **implemented**
- One-sided Jacobi SVD for arbitrary dense matrix shapes: **implemented**
- Numerical rank, pseudoinverse, minimum-norm least squares and 2-norm condition diagnostics: **implemented**
- Matrix 1-, infinity-, Frobenius- and spectral norms: **implemented**
- Combined rank, left/right nullity and conditioning report: **implemented**
- Optional mature BLAS/LAPACK-class backend: later, benchmark driven

Additional dense utilities remain possible, but the managed reference foundation is complete enough to support the next layers without pretending to replace vendor BLAS/LAPACK implementations.

### M3.2 — Sparse linear algebra (foundation complete)

- Canonical immutable CSR storage and validation: **implemented**
- Coordinate/dense/raw-CSR construction with explicit duplicate policy: **implemented**
- Sparse matvec with reusable output buffers: **implemented**
- Transpose, dense conversion and inexpensive sparse norms: **implemented**
- Shared sparse iterative options/result diagnostics: **implemented**
- Conjugate Gradient for SPD sparse systems with true-residual verification: **implemented**
- Solver-neutral `ISparsePreconditioner` abstraction: **implemented**
- Diagonal/Jacobi preconditioning: **implemented**
- Preconditioned Conjugate Gradient with SPD recurrence validation: **implemented**
- Restarted GMRES for general nonsymmetric systems: **implemented**
- Right-preconditioned GMRES with modified Gram-Schmidt/Arnoldi and Givens updates: **implemented**
- BiCGSTAB with right preconditioning, bounded vector memory and explicit recurrence-breakdown diagnostics: **implemented**
- Structured cross-solver release-reference cases: **implemented**
- Public independent true-residual diagnostic: **implemented**
- Release-facing sparse sample exercising GMRES/BiCGSTAB and residual verification: **implemented**
- Sparse public-API/XML/allocation consolidation: **completed**
- Dedicated CSC and stronger incomplete-factorization preconditioners: later when concrete workloads justify them

M3.2 is frozen at this release boundary except for audit fixes. The current work is release-candidate hardening, not additional sparse-solver scope.

### M3.3 — Optimization and nonlinear systems

Multidimensional roots, Nelder-Mead, BFGS/L-BFGS, line search and explicit diagnostics.

### M3.4 — Modern ODE capabilities

Dormand-Prince, dense output, event detection, lower-allocation vector-state APIs and later stiff solvers when justified.

### M3.5 — Statistics, random and special functions

Stable descriptive statistics, distributions, quantiles, regression diagnostics, reproducible random streams, special functions and PCA on the SVD foundation.

### M3.6 — Geometry, transforms and simulation

Vector3/4, transforms, quaternions, curves/intersections, broader FFT support and simulation foundations.

### M3.7 — Performance backends and release engineering

Benchmarks, optional BLAS/LAPACK adapters, measured SIMD, package/API compatibility checks, release automation, and regeneration of the German/English LaTeX PDF handbooks at release-candidate boundaries.

## M4 — Multi-language implementations

Equivalent implementations may proceed in parallel when useful. Shared behavior should increasingly move into machine-readable `spec/` reference vectors and cross-language conformance tests.

## M5 — Optional high-performance backends

Evaluate adapters for mature libraries behind stable SASD interfaces while retaining the dependency-free managed reference implementation for portability, diagnostics and education.
