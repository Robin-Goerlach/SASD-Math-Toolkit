# Changelog

All notable release-facing changes to SASD Math Toolkit are recorded here.

The project uses semantic versioning for package/release identifiers.

## Unreleased

### C++ 0.4.0 / CPP-M3

- Added finite-input linear, Lagrange and Newton divided-difference interpolation.
- Added an immutable reusable Newton interpolant with caller-controlled sequential, automatic and parallel batch evaluation.
- Added immutable natural and clamped cubic splines with value, first-derivative and second-derivative evaluation on their closed knot domain.
- Added deterministic spline batch evaluation without claiming a universal parallel crossover size.
- Added a language-neutral interpolation contract, shared analytic conformance cases, 64 focused C++ checks and independent German/English documentation.

### C++ 0.3.0 / CPP-M2

- Added immutable complex-capable polynomials with Horner evaluation, combined derivative evaluation, formal derivatives and synthetic deflation.
- Added status-bearing bisection, Newton-Raphson, secant, complex Muller, Newton-Horner and Laguerre root solvers.
- Added deterministic all-roots Laguerre search with fallback starts, deflation and original-polynomial residual polishing.
- Added a language-neutral root-finding contract, shared scalar conformance cases and 63 focused C++ checks.
- Added independent German/English technical and handbook chapters and advanced the C++ handbook to version 0.3.

### C++ 0.2.0 / CPP-M1

- Added finite-input descriptive statistics: compensated scaled mean, overflow-safe median and scaled population/sample variance.
- Added deterministic automatic/sequential/parallel reductions for mean and variance, with caller-controlled CPU limits.
- Added numerical boundary, validation, parallel consistency and installed-package tests.
- Separated .NET and C++ technical documentation, handbook sources and LaTeX publication projects into explicit language trees.
- Recorded the semantic migration plan for relevant `sasd-toolbox-dotnet` functionality without duplicating the existing root-finding implementation.

### C++ 0.1.0 foundation

- Added the independently versioned C++20 project under `src/cpp/`, with CMake build and install-package support.
- Added the initial `sasd::numerics::common` diagnostic vocabulary, result envelope and numerical defaults.
- Added an explicit automatic/sequential/parallel CPU execution policy and a portable, exception-safe internal C++20 worker primitive for suitable workloads.
- Added isolated C++ tests, an executable sample, an installed-package consumer smoke test and GCC/Clang CI.
- Added separate German and English C++ technical documentation and Markdown/LaTeX handbook sources.
- Established `conformance/` as the language-neutral home for future shared test vectors without moving the stable C#/.NET 1.0 documentation.

## 1.0.0 — 2026-09-16

### First stable release

- Promoted the audited `1.0.0-rc.1` code line to the first stable `1.0.0` package after successful Linux CI and independent Windows acceptance.
- Preserved the completed classical numerical-methods foundation together with the M3.1 modern dense and M3.2 sparse foundations.
- Finalized NuGet package identity as `Sasd.Math.Toolkit` version `1.0.0` and retained symbol-package generation, package-content verification and executable smoke testing.
- Finalized the German and English V1.0 handbook front matter and regenerated both release PDF snapshots from the maintained Markdown sources.
- No algorithmic behavior was changed as part of the RC1-to-1.0.0 promotion; the promotion records the completed audit and release decision.

## 1.0.0-rc.1 — 2026-09-15

### Classical numerical foundation

- Completed the independently implemented classical numerical-methods compatibility catalog across roots, interpolation, differentiation, integration, linear algebra/eigenvalues, ODE/BVP methods, least squares and FFT applications.
- Added status/residual/error diagnostics, input validation, deterministic examples and regression coverage around the historical problem classes.

### 2026 dense modernization

- Added Householder QR and migrated general full-column-rank least squares away from normal equations.
- Added Cholesky factorization with reusable solves and log-determinant support.
- Added one-sided Jacobi SVD with numerical rank, minimum-norm solves, pseudoinverse and condition diagnostics.
- Added stable matrix norms and shared rank/nullity/conditioning reporting.

### 2026 sparse modernization

- Added immutable canonical CSR storage, coordinate assembly, transpose, sparse matvec and sparse norms.
- Added Conjugate Gradient, Preconditioned Conjugate Gradient and a solver-neutral preconditioner contract with Jacobi preconditioning.
- Added restarted GMRES and BiCGSTAB for general square nonsymmetric systems with right preconditioning.
- Added true-residual verification, explicit breakdown diagnostics, cross-solver release-reference cases and a release-facing sparse smoke example.

### Documentation and release engineering

- Maintained German and English Markdown user-handbook chapters as the editorial source of truth.
- Synchronized the LaTeX/PDF publication layer with modernization chapters 12 and 13 and updated RC1 front/back matter.
- Regenerated both language PDFs, added structural `pdfinfo` validation plus retained CI artifacts, and completed page-by-page visual release-candidate inspection.
- Added NuGet-oriented package metadata, package README, symbols-package generation and CI verification of package contents.
- Extended CI to run the deterministic sample/sparse smoke path in addition to Release build and tests.

### Provenance

SASD Math Toolkit is a clean-room implementation. Historical toolbox material is used as a functional/reference catalog only; historical source code and substantial manual text are not copied.
