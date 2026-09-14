# Roadmap

## M0 — Repository foundation (done)

- Language-neutral repository layout
- .NET 10 class library, tests, sample application and CI
- English and German documentation roots
- Clean-room policy
- Common iteration status/result conventions

## M1 — Broad numerical foundation (in progress; vertical slice expanded)

Implemented across the major domains: scalar and complex roots, reusable polynomial/Horner/deflation support, polynomial/spline interpolation, the complete historical V1 differentiation catalog, integration, dense linear algebra with reusable pivoted LU factorization, the complete historical V1 eigenvalue catalog, fixed-step RK4 plus adaptive RKF45 ODE integration, least squares and radix-2 FFT.

The root-finding, differentiation and eigenvalue slices include their complete historical V1 catalogs. Direct LU factorization is complete through `LuFactorization`. The ODE slice now includes both classical RK4 and adaptive Runge-Kutta-Fehlberg 4(5), with explicit tolerances, rejected-step diagnostics and bounded adaptive step sizes.

A C#/.NET user handbook lives under `docs/user-guide/` and grows alongside stable implementation milestones rather than reusing the historical Pascal documentation. The ODE chapter now documents both RK4 and RKF45 usage and numerical interpretation.

The intent remains to validate architecture across the whole product before filling every historical routine.

## M2 — Complete V1 compatibility catalog

Close every item marked `planned` in `BORLAND-V1-COMPATIBILITY.md`, including Adams predictor-corrector methods, shooting methods and the remaining FFT variants. Several convenience APIs currently marked `partial` must also be completed or explicitly documented as superseded by a more general equivalent.

Quality gate for V1:

- All catalog items implemented or intentionally documented as superseded by an equivalent API.
- Unit/regression tests for every public algorithm.
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
