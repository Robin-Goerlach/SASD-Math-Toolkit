# Roadmap

## M0 — Repository foundation (done)

- Language-neutral repository layout
- .NET 10 class library, tests, sample application and CI
- English and German documentation roots
- Clean-room policy
- Common iteration status/result conventions

## M1 — Broad numerical foundation (in progress; vertical slice expanded)

Implemented across the major domains: scalar and complex roots, reusable polynomial/Horner/deflation support, polynomial/spline interpolation, the complete historical V1 differentiation catalog, integration, dense linear algebra with reusable pivoted LU factorization, the complete historical V1 eigenvalue catalog, RK4/RKF45/Adams ODE integration, least squares and radix-2 FFT.

The root-finding slice includes the complete historical V1 root catalog. Direct LU factorization is complete through `LuFactorization`. The eigenvalue slice is complete through power iteration, inverse power iteration, Wielandt deflation and cyclic Jacobi. Differentiation is complete for V1. The scalar first-order ODE slice now contains fixed-step RK4, adaptive RKF45 and fourth-order Adams-Bashforth/Adams-Moulton predictor-corrector integration.

A C#/.NET user handbook under `docs/user-guide/` grows alongside stable implementation milestones rather than reusing the historical Pascal documentation.

The intent remains to validate architecture across the whole product before filling every historical routine.

## M2 — Complete V1 compatibility catalog

Close every item marked `planned` in `BORLAND-V1-COMPATIBILITY.md`, especially linear/nonlinear shooting methods and the remaining FFT helpers. Complete or explicitly supersede the convenience APIs currently marked `partial`, including higher-order ODE wrappers and named least-squares helpers.

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
