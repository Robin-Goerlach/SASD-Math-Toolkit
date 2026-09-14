# SASD Math Toolkit

**SASD Math Toolkit** is the reusable mathematical foundation for the SASD Toolbox ecosystem. The repository starts with a modern, independently implemented C# numerical-methods library and is intentionally structured so equivalent implementations can later be added for C++, Fortran, Java and JavaScript without redesigning the repository.

The library is intended to become a common base for projects such as the **SASD Game Toolkit**, **Fully Encrypted**, the **SASD Statistical Workbench**, graphics/simulation software and future SASD scientific applications.

## V1 target: Numerical Methods compatibility set

Version 1 targets the functional scope of Borland's historical *Turbo Pascal Numerical Methods Toolbox* as a compatibility/reference milestone, **not** as a source-code port. The implementation in this repository is new code with modern APIs, tests and documentation. Historical source code is neither required nor copied.

The V1 catalog contains these areas:

1. Roots of equations in one variable
2. Interpolation
3. Numerical differentiation
4. Numerical integration
5. Matrix routines
6. Eigenvalues and eigenvectors
7. Initial-value and boundary-value methods for ODEs
8. Least-squares approximation
9. Fast Fourier transform routines, convolution and correlation
10. Demonstration/sample applications

See [`docs/en/BORLAND-V1-COMPATIBILITY.md`](docs/en/BORLAND-V1-COMPATIBILITY.md) for the implementation matrix.

## Repository architecture

```text
SASD-Math-Toolkit/
├── src/
│   └── dotnet/                 # C#/.NET implementation (first language)
├── tests/
│   └── dotnet/                 # automated numerical regression tests
├── samples/
│   └── dotnet/                 # small executable examples
├── spec/                       # language-neutral contracts and algorithm catalog
├── docs/
│   ├── en/                     # primary technical documentation
│   ├── de/                     # German technical documentation
│   └── user-guide/             # C#/.NET user handbook (English and German)
└── .github/workflows/          # build and test automation
```

Future language trees will be peers of `dotnet`, for example `src/cpp`, `src/fortran`, `src/java` and `src/javascript`. Language-neutral behavior belongs in `spec/`, not in one language implementation.

## Current C# foundation (0.1.0 development line)

Implemented now:

- Root finding: bisection, Newton-Raphson, secant, Newton-Horner, complex Muller and Laguerre with polynomial deflation/all-roots support
- Polynomial foundation: immutable complex-capable coefficients, Horner evaluation, first/second derivative evaluation, formal derivative and synthetic deflation
- Interpolation: Lagrange, Newton divided differences, natural cubic spline and clamped cubic spline
- Numerical differentiation: function derivatives with Richardson/five-point formulas, tabular two/three/five-point methods on uniform or non-uniform grids, and first/second derivatives of cubic splines
- Numerical integration: composite trapezoid, composite Simpson, adaptive Simpson, Romberg, 5-point Gauss-Legendre and adaptive Gauss-Legendre
- Linear algebra: determinant, Gaussian elimination, partial pivoting, reusable LU factorization/solve, inverse and Gauss-Seidel iteration
- Eigenvalues: power method, LU-backed inverse power method, Wielandt deflation/second eigenpair and cyclic Jacobi complete symmetric eigensystems
- Differential equations: fixed-step RK4, adaptive RKF45, fourth-order Adams-Bashforth/Adams-Moulton predictor-corrector, and RK4 for first-order systems
- Least squares: polynomial and arbitrary linear-basis fitting
- FFT: radix-2 complex/real FFT, inverse FFT, real convolution and real cross-correlation
- Geometry seed: immutable `Vector2D`

This is deliberately a useful vertical slice rather than a collection of placeholders. Missing V1 algorithms remain explicit roadmap items instead of methods that only throw `NotImplementedException`.

## Build

```bash
dotnet restore Sasd.Math.Toolkit.slnx
dotnet build Sasd.Math.Toolkit.slnx --configuration Release
dotnet test Sasd.Math.Toolkit.slnx --configuration Release
```

Target framework: **.NET 10**.

## Design rules

- Numerical algorithms are deterministic and independent from UI, files and console I/O.
- Input validation is explicit.
- Iterative algorithms return termination status, iteration count and residual information where useful.
- Public APIs use ordinary .NET data types and `System.Numerics.Complex` where appropriate.
- Algorithms stay understandable and well documented before micro-optimization begins.
- Large-scale/high-performance workloads may later use optional BLAS/LAPACK or other mature backends behind SASD abstractions.
- A historical algorithm name is a mathematical reference, not a dependency on historical source code.

## Documentation

Technical documentation:

- English: [`docs/en/ARCHITECTURE.md`](docs/en/ARCHITECTURE.md), [`docs/en/ROADMAP.md`](docs/en/ROADMAP.md), [`docs/en/DIFFERENTIATION.md`](docs/en/DIFFERENTIATION.md), [`docs/en/POLYNOMIAL-ROOTS.md`](docs/en/POLYNOMIAL-ROOTS.md), [`docs/en/LU-FACTORIZATION.md`](docs/en/LU-FACTORIZATION.md), [`docs/en/EIGENVALUES.md`](docs/en/EIGENVALUES.md), [`docs/en/RUNGE-KUTTA-FEHLBERG.md`](docs/en/RUNGE-KUTTA-FEHLBERG.md), [`docs/en/ADAMS-PREDICTOR-CORRECTOR.md`](docs/en/ADAMS-PREDICTOR-CORRECTOR.md)
- Deutsch: [`docs/de/ARCHITECTURE.md`](docs/de/ARCHITECTURE.md), [`docs/de/ROADMAP.md`](docs/de/ROADMAP.md), [`docs/de/DIFFERENTIATION.md`](docs/de/DIFFERENTIATION.md), [`docs/de/POLYNOM-NULLSTELLEN.md`](docs/de/POLYNOM-NULLSTELLEN.md), [`docs/de/LU-FAKTORISIERUNG.md`](docs/de/LU-FAKTORISIERUNG.md), [`docs/de/EIGENWERTE.md`](docs/de/EIGENWERTE.md), [`docs/de/RUNGE-KUTTA-FEHLBERG.md`](docs/de/RUNGE-KUTTA-FEHLBERG.md), [`docs/de/ADAMS-PREDICTOR-CORRECTOR.md`](docs/de/ADAMS-PREDICTOR-CORRECTOR.md)
- Clean-room policy: [`docs/en/CLEAN-ROOM.md`](docs/en/CLEAN-ROOM.md)

User handbook:

- English: [`docs/user-guide/en/README.md`](docs/user-guide/en/README.md)
- Deutsch: [`docs/user-guide/de/README.md`](docs/user-guide/de/README.md)

The handbook is written for the SASD C#/.NET API and grows with stable milestones; it is not a redistribution of the historical Pascal handbook.

## License

MIT. See [`LICENSE`](LICENSE).
