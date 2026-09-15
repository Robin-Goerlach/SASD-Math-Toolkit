# Roadmap

## M0 — Repository foundation (done)

- Language-neutral repository layout
- .NET 10 class library, tests, sample application and CI
- English and German documentation roots
- Clean-room policy
- Common iteration status/result conventions

## M1 — Broad numerical foundation (done for the historical numerical catalog)

Implemented across the major domains: scalar and complex roots, polynomial/Horner/deflation support, interpolation/splines, differentiation, integration, dense linear algebra/eigenvalues, RK4/RKF45/Adams ODE integration, least squares, radix-2 FFT, convolution/correlation and the demonstration layer.

## M2 — V1 publication quality (final audit remaining)

The historical compatibility catalog and all eleven planned user-handbook chapters are complete. Publication-quality API/test audits and cross-cutting diagnostic documentation are also complete.

Before the final audit, a pragmatic performance pass was completed. It removes repeated checked matrix indexing from dense hot loops, batches multi-RHS LU substitution, caches least-squares design values, specializes polynomial basis generation, and removes redundant full-length FFT/convolution copies while preserving public defensive-copy semantics. See [`PERFORMANCE.md`](PERFORMANCE.md). The round intentionally stops short of unsafe/SIMD/parallel/native-backend tuning until representative benchmarks exist.

The remaining V1 milestone is the **final repository-wide release audit**. It should verify public API consistency, XML comments, examples, handbook links, technical notes, compatibility claims, absence of public placeholders, package/release metadata, deterministic sample behavior and CI coverage before assigning a V1 release tag.

Quality gate for V1:

- All catalog items implemented or intentionally documented as superseded by an equivalent API.
- Unit/regression tests for every public algorithm and important failure status.
- Numerical examples compared against independent analytical/reference results.
- API and usage documentation in English; German user/developer overview synchronized.
- User-handbook chapters for the stable V1 areas, with C# examples and numerical interpretation guidance.
- No `NotImplementedException` in public numerical APIs.
- Cross-cutting diagnostic conventions documented and reflected in result APIs.
- Pragmatic pre-release performance pass completed without weakening validation or ownership contracts.
- Final release audit completed before the V1 tag.

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
