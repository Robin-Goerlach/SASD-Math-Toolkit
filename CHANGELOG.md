# Changelog

All notable release-facing changes to SASD Math Toolkit are recorded here.

The project uses semantic versioning for package/release identifiers. The current `1.0.0-rc.1` line is a release candidate and is not a declaration that the final `1.0.0` release has been published.

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
