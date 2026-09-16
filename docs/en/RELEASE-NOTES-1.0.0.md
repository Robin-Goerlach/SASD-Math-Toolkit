# SASD Math Toolkit 1.0.0

SASD Math Toolkit 1.0.0 is the first stable release of the reusable .NET 10 mathematical foundation for SASD projects.

## Highlights

- Complete independently implemented classical numerical-methods foundation covering roots, interpolation, differentiation, integration, linear algebra/eigenvalues, ODE/BVP methods, least squares and FFT applications.
- Modern dense linear algebra with Householder QR, Cholesky, one-sided Jacobi SVD, pseudoinverse, stable norms and rank/nullity/conditioning diagnostics.
- Sparse linear algebra with immutable canonical CSR, CG/PCG, solver-neutral preconditioning, Jacobi, restarted GMRES and BiCGSTAB with true-residual verification.
- Deterministic HTML/SVG numerical demo and a release-facing sparse GMRES/BiCGSTAB smoke case.
- German and English Markdown handbooks as the editorial source of truth, plus final V1.0 PDF editions generated through the LaTeX publication layer.
- NuGet-oriented package metadata, symbol package, package-content validation and retained CI release artifacts.

## Validation

The stable promotion preserves the accepted RC1 numerical implementation. Before promotion, the candidate passed an independent Windows x64 acceptance run with .NET SDK 10.0.303/runtime 10.0.11: 178/178 tests passed, the deterministic demo and sparse smoke solve succeeded, both package files were produced, and the working tree remained clean.

The final `1.0.0` promotion was then validated on Ubuntu 24.04 with .NET SDK 10.0.401/runtime 10.0.12: Release build completed with 0 warnings and 0 errors, 178/178 tests passed, `Sasd.Math.Toolkit.1.0.0.nupkg` and `.snupkg` were produced and inspected, and the sample smoke path remained green.

The final German handbook contains 79 pages and the English handbook 81 pages. Both final editions passed structural validation and full rendered-page visual inspection.

## Release scope

Version 1.0.0 includes the classical foundation, M3.1 modern dense linear algebra and M3.2 sparse linear algebra. Later modernization areas such as additional preconditioners/Krylov methods, broader statistics/special-functions work and optional BLAS/LAPACK backends remain post-1.0 roadmap items.

## Provenance and license

SASD Math Toolkit is an independent clean-room implementation. Historical toolbox material is used only as a high-level functional/reference catalog; historical source code and substantial manual text are not copied.

License: MIT.
