# Roadmap

## M0 — Repository foundation (done)

- Language-neutral repository layout
- .NET 10 class library, tests, sample application and CI
- English and German documentation roots
- Clean-room policy
- Common iteration status/result conventions

## M1 — Broad numerical foundation (done for the historical numerical catalog)

Implemented across the major domains: scalar and complex roots, reusable polynomial/Horner/deflation support, polynomial/spline interpolation, the complete historical V1 differentiation catalog, integration, dense linear algebra with reusable pivoted LU factorization, the complete historical V1 eigenvalue catalog, RK4/RKF45/Adams ODE integration, least squares and radix-2 FFT.

The RK4 ODE convenience family covers scalar first-order, scalar second-order, scalar nth-order, coupled first-order and coupled second-order systems on the same shared system RK4 core. Adaptive scalar RKF45 and fourth-order Adams-Bashforth/Adams-Moulton predictor-corrector integration are also available. The historical boundary-value slice covers both linear and nonlinear RK4-backed shooting. The least-squares model set includes general linear-basis and polynomial fitting plus named power-law, exponential, logarithmic and five-term Fourier models. The transform slice includes complex and real FFT, compact real spectra, and real/complex convolution and cross-correlation.

## M2 — V1 publication quality (in progress)

The historical compatibility catalog, including the graphical demonstration layer, is now implemented. Work therefore focuses on the quality required for a release rather than on adding more historical algorithms.

The independent C#/.NET user handbook under `docs/user-guide/` is being completed domain by domain. The root-finding chapter is complete and covers bisection, Newton-Raphson, secant, Newton-Horner, Muller, Laguerre and all-roots deflation workflows. The next handbook areas are interpolation, integration, matrices/linear systems and eigenvalues before a cross-cutting diagnostics chapter and final V1 audit.

Quality gate for V1:

- All catalog items implemented or intentionally documented as superseded by an equivalent API.
- Unit/regression tests for every public algorithm and important failure status.
- Numerical examples compared against independent analytical/reference results.
- API and usage documentation in English; German user/developer overview synchronized.
- User-handbook chapters for the stable V1 areas, with C# examples and numerical interpretation guidance.
- No `NotImplementedException` in public numerical APIs.

## M3 — SASD core mathematics

Expand beyond the historical toolbox where modern SASD projects need shared foundations:

- vectors, matrices and transformations for Game Toolkit/graphics
- geometry, curves and intersections
- robust tolerances and comparison helpers
- random-number abstractions and deterministic simulation support
- statistics primitives required by SASD Statistical Workbench
- number-theory/support algorithms useful to cryptographic applications without pretending to replace audited cryptographic libraries

## M4 — Multi-language implementations

Add implementations in this order unless a concrete project changes priorities:

1. C++
2. Java
3. JavaScript/TypeScript
4. Fortran

At this stage, move reference vectors and behavioral contracts into machine-readable `spec/` artifacts and run cross-language conformance tests.

## M5 — Optional high-performance backends

Evaluate adapters for mature libraries (BLAS/LAPACK and equivalents) behind stable SASD interfaces. Keep the dependency-free reference implementation for education, determinism tests and portability.
