# Roadmap

## M0 — Repository foundation (done)

- Language-neutral repository layout
- .NET 10 class library, tests, sample application and CI
- English and German documentation roots
- Clean-room policy
- Common iteration status/result conventions

## M1 — Broad numerical foundation (done for the historical V1 algorithm catalog)

The historical numerical method catalog is covered across scalar and complex roots, interpolation/splines, differentiation, integration, dense linear algebra, eigenvalues, RK4/RKF45/Adams ODE integration, boundary-value shooting, least-squares approximation, FFT, convolution and correlation.

The implementation deliberately shares foundations instead of cloning formulas across convenience APIs: LU factorization is reusable, higher-order RK4 variants reduce to the common system integrator, transformed least-squares models reuse the general basis solver, and convolution/correlation build on the shared complex FFT core.

A C#/.NET user handbook under `docs/user-guide/` grows alongside stable implementation milestones rather than reusing the historical Pascal documentation.

## M2 — Complete V1 compatibility catalog (functional catalog done; release hardening in progress)

The historical demonstration/graphics role is now covered by the cross-platform `Sasd.Math.Toolkit.Sample` application, which generates a deterministic self-contained HTML/SVG numerical report without introducing UI dependencies into the reusable library.

All rows in `BORLAND-V1-COMPATIBILITY.md` are therefore functionally covered. Remaining V1 work is release hardening rather than additional historical algorithms:

- complete the remaining user-handbook chapters for stable V1 areas;
- perform an API consistency and public-surface audit;
- verify XML documentation and error semantics across all public algorithms;
- expand executable examples where a handbook chapter benefits from them;
- perform the final compatibility/test audit and prepare release notes/versioning.

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
