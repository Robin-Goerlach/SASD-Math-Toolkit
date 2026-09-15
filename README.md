# SASD Math Toolkit

**SASD Math Toolkit** is the reusable mathematical foundation for the SASD Toolbox ecosystem. The repository began with a modern, independently implemented C# version of the classical Numerical Methods compatibility set and is now evolving into a broader 2026 numerical platform. Equivalent implementations can live alongside .NET without redesigning the repository.

The library is intended to become a common base for projects such as the **SASD Game Toolkit**, **Fully Encrypted**, the **SASD Statistical Workbench**, graphics/simulation software and future SASD scientific applications.

## Classical baseline and 2026 modernization

The historical Borland-inspired feature-transfer phase is functionally complete. It remains a compatibility/reference baseline, **not** a source-code port and no longer the boundary of future development. Historical source code is neither required nor copied.

Modernization is now in progress. The modern dense numerical foundation includes **Householder QR**, **Cholesky**, a **one-sided Jacobi singular value decomposition**, stable matrix norms and shared rank/nullity/conditioning diagnostics. General/polynomial least squares no longer rely on normal equations; the general basis API can use SVD for rank-deficient or underdetermined minimum-norm problems. Sparse linear algebra now includes immutable canonical **CSR**, sparse arithmetic/norms, **Conjugate Gradient**, a solver-neutral preconditioner abstraction, **Jacobi preconditioning** and **Preconditioned Conjugate Gradient (PCG)** with true-residual and SPD-recurrence validation. The next sparse solver target is restarted GMRES. The broader plan is documented in [`docs/en/MODERNIZATION-2026.md`](docs/en/MODERNIZATION-2026.md).

A final repository-wide release audit is still required before a public 1.0 tag; it will be run against the actual release candidate so modern APIs are included too.

See [`docs/en/BORLAND-V1-COMPATIBILITY.md`](docs/en/BORLAND-V1-COMPATIBILITY.md) for the classical implementation matrix.

## Repository architecture

```text
SASD-Math-Toolkit/
├── src/
│   └── dotnet/                 # C#/.NET implementation (first language)
├── tests/
│   └── dotnet/                 # automated numerical regression tests
├── samples/
│   └── dotnet/                 # executable and graphical examples
├── spec/                       # language-neutral contracts and algorithm catalog
├── docs/
│   ├── en/                     # primary technical documentation
│   ├── de/                     # German technical documentation
│   └── user-guide/             # Markdown handbook + LaTeX/PDF editions
└── .github/workflows/          # build and test automation
```

Future language trees are peers of `dotnet`. Language-neutral behavior belongs in `spec/`, not in one language implementation.

## Current C# foundation

Implemented now:

- Root finding: bisection, Newton-Raphson, secant, Newton-Horner, complex Muller and Laguerre with polynomial deflation/all-roots support
- Polynomial foundation: immutable complex-capable coefficients, Horner evaluation, derivative evaluation and synthetic deflation
- Interpolation: Lagrange, Newton divided differences, natural and clamped cubic splines
- Numerical differentiation: function, tabular and spline differentiation
- Numerical integration: composite/adaptive rules, Romberg and Gauss-Legendre with diagnostics
- Dense linear algebra: row-major dense matrices, determinant, Gaussian elimination, reusable pivoted LU, **Householder QR**, **Cholesky**, **SVD with numerical rank, 2-norm condition diagnostics and pseudoinverse**, stable 1-/infinity-/Frobenius-/spectral norms, shared nullity/conditioning reports, inverse, residual diagnostics and Gauss-Seidel
- Sparse linear algebra: immutable canonical **CSR** storage, coordinate/dense/raw-CSR construction, duplicate aggregation, sparse matrix-vector multiplication with reusable span buffers, transpose/dense conversion, sparse 1-/infinity-/Frobenius norms, structural symmetry diagnostics, **CG**, solver-neutral `ISparsePreconditioner`, **Jacobi preconditioning** and **PCG** with explicit iteration/residual diagnostics
- Eigenvalues: immutable eigenpairs, residual diagnostics, power/inverse-power, Wielandt deflation and cyclic Jacobi
- Differential equations: RK4 convenience family, adaptive RKF45, Adams predictor-corrector and linear/nonlinear shooting
- Least squares: polynomial/general-basis fits prefer **Householder QR**; general-basis fitting falls back to SVD for rank-deficient/underdetermined minimum-norm problems; named power-law, exponential, logarithmic and five-term Fourier models retain their domain/identifiability contracts
- FFT/transforms: radix-2 complex/real FFT, compact real spectra, real/complex convolution and cross-correlation
- Graphical demo: deterministic self-contained HTML/SVG report
- Geometry seed: immutable `Vector2D`

## Build

```bash
dotnet restore Sasd.Math.Toolkit.slnx
dotnet build Sasd.Math.Toolkit.slnx --configuration Release
dotnet test Sasd.Math.Toolkit.slnx --configuration Release
```

Target framework: **.NET 10**.

## Design rules

- Numerical algorithms are deterministic and independent from UI, files and console I/O.
- Input validation is explicit; malformed calls use normal .NET argument exceptions.
- Expected iterative/adaptive non-convergence is reported through status-bearing result types where that is part of the public workflow.
- Status, value, residual, rank, conditioning and error estimate are separate concepts.
- Tolerances are explicit and problem-specific; no mutable global convergence tolerance exists.
- Numerically more stable formulations replace historical internals when public compatibility can be preserved.
- Public APIs use ordinary .NET types and `System.Numerics.Complex` where appropriate.
- Readability remains important, but obvious hot-loop, memory-locality and allocation costs are optimized when this can be done without weakening the public contract.
- Sparse representations are canonical and immutable after construction; iterative sparse algorithms reuse fixed work buffers instead of converting to dense storage.
- Sparse iterative solvers share one absolute/relative residual stopping rule and the toolkit-wide `IterationStatus` vocabulary.
- Preconditioners expose an approximate-inverse operation rather than an explicit inverse matrix; solver-specific mathematical requirements stay at the solver boundary.
- Aggressive unsafe/SIMD/parallel/native tuning is benchmark-driven rather than speculative.
- Large-scale workloads may later use optional BLAS/LAPACK or other mature backends behind SASD abstractions.
- Historical algorithm names are mathematical references, not dependencies on historical source code.

## Documentation

- 2026 modernization plan: [`docs/en/MODERNIZATION-2026.md`](docs/en/MODERNIZATION-2026.md)
- QR design note: [`docs/en/QR-FACTORIZATION.md`](docs/en/QR-FACTORIZATION.md)
- Cholesky design note: [`docs/en/CHOLESKY-FACTORIZATION.md`](docs/en/CHOLESKY-FACTORIZATION.md)
- SVD design note: [`docs/en/SINGULAR-VALUE-DECOMPOSITION.md`](docs/en/SINGULAR-VALUE-DECOMPOSITION.md)
- Matrix norms and conditioning: [`docs/en/MATRIX-NORMS-CONDITIONING.md`](docs/en/MATRIX-NORMS-CONDITIONING.md)
- Sparse CSR foundation: [`docs/en/SPARSE-CSR.md`](docs/en/SPARSE-CSR.md)
- Sparse CG/PCG: [`docs/en/SPARSE-CONJUGATE-GRADIENT.md`](docs/en/SPARSE-CONJUGATE-GRADIENT.md)
- Sparse preconditioning: [`docs/en/SPARSE-PRECONDITIONING.md`](docs/en/SPARSE-PRECONDITIONING.md)
- Performance policy: [`docs/en/PERFORMANCE.md`](docs/en/PERFORMANCE.md)
- Diagnostic conventions: [`docs/en/NUMERICAL-DIAGNOSTICS.md`](docs/en/NUMERICAL-DIAGNOSTICS.md)
- Clean-room policy: [`docs/en/CLEAN-ROOM.md`](docs/en/CLEAN-ROOM.md)
- English user handbook: [`docs/user-guide/en/README.md`](docs/user-guide/en/README.md)
- Deutsches Benutzerhandbuch: [`docs/user-guide/de/README.md`](docs/user-guide/de/README.md)

The **Markdown handbook is the editorial source of truth and is updated continuously with each modernization milestone**. The LaTeX directory is the typesetting/build layer. The committed V1.0 PDFs remain a stable classical edition while modernization continues; LaTeX chapter ordering/metadata and both PDFs are synchronized from the current Markdown at release-candidate boundaries.

## License

MIT. See [`LICENSE`](LICENSE).
