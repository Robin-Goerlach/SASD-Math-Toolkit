# SASD Math Toolkit

**SASD Math Toolkit** is the reusable mathematical foundation for the SASD Toolbox ecosystem. The repository starts with a modern, independently implemented C# numerical-methods library and is intentionally structured so equivalent implementations can later be added for C++, Fortran, Java and JavaScript without redesigning the repository.

The library is intended to become a common base for projects such as the **SASD Game Toolkit**, **Fully Encrypted**, the **SASD Statistical Workbench**, graphics/simulation software and future SASD scientific applications.

## V1 target: Numerical Methods compatibility set

Version 1 targets the functional scope of Borland's historical *Turbo Pascal Numerical Methods Toolbox* as a compatibility/reference milestone, **not** as a source-code port. The implementation is new code with modern APIs, tests and documentation. Historical source code is neither required nor copied.

The historical V1 compatibility catalog and all planned C#/.NET user-handbook chapters are implemented. A pragmatic performance pass has also been completed; the remaining V1 work is the final repository-wide release audit.

See [`docs/en/BORLAND-V1-COMPATIBILITY.md`](docs/en/BORLAND-V1-COMPATIBILITY.md) for the implementation matrix.

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
│   └── user-guide/             # C#/.NET user handbook (English and German)
└── .github/workflows/          # build and test automation
```

Future language trees will be peers of `dotnet`. Language-neutral behavior belongs in `spec/`, not in one language implementation.

## Current C# foundation

Implemented now:

- Root finding: bisection, Newton-Raphson, secant, Newton-Horner, complex Muller and Laguerre with polynomial deflation/all-roots support
- Polynomial foundation: immutable complex-capable coefficients, Horner evaluation, derivative evaluation and synthetic deflation
- Interpolation: Lagrange, Newton divided differences, natural and clamped cubic splines
- Numerical differentiation: function, tabular and spline differentiation
- Numerical integration: composite/adaptive rules, Romberg and Gauss-Legendre with diagnostics
- Linear algebra: row-major dense matrices, determinant, Gaussian elimination, reusable pivoted LU including batched RHS solves, inverse, residual diagnostics and Gauss-Seidel
- Eigenvalues: immutable eigenpairs, residual diagnostics, power/inverse-power, Wielandt deflation and cyclic Jacobi
- Differential equations: RK4 convenience family, adaptive RKF45, Adams predictor-corrector and linear/nonlinear shooting
- Least squares: polynomial/general-basis plus power-law, exponential, logarithmic and five-term Fourier models
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
- Expected iterative/adaptive non-convergence is reported through status-bearing result types.
- Status, value, residual and error estimate are separate concepts.
- Tolerances are explicit and problem-specific; no mutable global convergence tolerance exists.
- Public APIs use ordinary .NET types and `System.Numerics.Complex` where appropriate.
- Readability remains important, but obvious hot-loop, memory-locality and allocation costs are optimized when this can be done without weakening the public contract.
- Aggressive unsafe/SIMD/parallel/native tuning is benchmark-driven rather than speculative.
- Large-scale workloads may later use optional BLAS/LAPACK or other mature backends behind SASD abstractions.
- Historical algorithm names are mathematical references, not dependencies on historical source code.

## Documentation

- Performance policy: [`docs/en/PERFORMANCE.md`](docs/en/PERFORMANCE.md)
- Diagnostic conventions: [`docs/en/NUMERICAL-DIAGNOSTICS.md`](docs/en/NUMERICAL-DIAGNOSTICS.md)
- Clean-room policy: [`docs/en/CLEAN-ROOM.md`](docs/en/CLEAN-ROOM.md)
- English user handbook: [`docs/user-guide/en/README.md`](docs/user-guide/en/README.md)
- Deutsches Benutzerhandbuch: [`docs/user-guide/de/README.md`](docs/user-guide/de/README.md)

The handbook is written for the SASD C#/.NET API and is not a redistribution of the historical Pascal handbook.

## License

MIT. See [`LICENSE`](LICENSE).
