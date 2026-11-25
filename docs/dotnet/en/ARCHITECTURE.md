# Architecture

## 1. Purpose

SASD Math Toolkit is a long-lived mathematical foundation, not a one-off port. The architecture therefore separates mathematical behavior from language, presentation, storage and application-specific concerns.

## 2. Repository layers

### `spec/` — language-neutral contract

This layer defines the catalog, terminology, expected behavior, convergence/status semantics and reference test cases. It is the future synchronization point for C#, C++, Fortran, Java and JavaScript.

### `src/<language>/` — implementations

Each language receives its own idiomatic implementation. Implementations do not have to share source code; they must share behavior where the specification says they should.

### `tests/<language>/` — executable verification

Tests cover known solutions, invariants, edge cases, convergence behavior and numerical tolerances. Cross-language golden vectors will be added as the second implementation appears.

### `samples/<language>/` — usage, not library logic

Input/output and demonstrations stay outside the reusable algorithm assemblies.

## 3. .NET package structure

The first implementation uses assembly/package `Sasd.Math.Toolkit` and namespace root `Sasd.Numerics`. `Sasd.Numerics` avoids ambiguity with `System.Math`.

Functional namespaces are stable extension points:

- `Sasd.Numerics.Common`
- `Sasd.Numerics.RootFinding`
- `Sasd.Numerics.Interpolation`
- `Sasd.Numerics.Differentiation`
- `Sasd.Numerics.Integration`
- `Sasd.Numerics.LinearAlgebra`
- `Sasd.Numerics.DifferentialEquations`
- `Sasd.Numerics.Approximation`
- `Sasd.Numerics.Transforms`
- `Sasd.Numerics.Geometry`

Future domains such as statistics, optimization, probability, special functions, computational geometry or cryptography-support mathematics can be added as sibling namespaces instead of being forced into the V1 numerical-analysis modules.

## 4. Error, convergence and diagnostic model

The library distinguishes caller errors from expected numerical termination.

### 4.1 Input contracts

Programmer/input-contract violations use normal exceptions (`ArgumentException`, `ArgumentOutOfRangeException`). Public numerical inputs are expected to be finite unless an API explicitly documents otherwise.

### 4.2 Expected numerical termination

Expected iterative outcomes use result objects with status values such as `Converged`, `MaximumIterationsReached`, `NumericalBreakdown` or `NotBracketed`. Adaptive quadrature and adaptive ODE integration use their own status enums because depth exhaustion and minimum-step exhaustion are domain-specific conditions, not generic iteration-limit synonyms.

The architecture requires consistent semantics, not one oversized universal status enum.

### 4.3 Status, value and residual are separate

`IterativeResult<T>` keeps termination status, computed value and residual separate. `Converged` reports only whether the documented convergence criterion was satisfied. `HasFiniteResidual` reports whether the generic residual field currently contains a finite diagnostic; it does not imply convergence.

A residual is a problem-facing defect such as `|f(x)|`, `||A*x-b||∞` or `||A*v-lambda*v||2`. Those quantities have different scales and must not be compared merely because they share the word “residual”. A small residual also does not automatically imply a small forward error when the problem is ill-conditioned.

Detailed conventions are documented in [`NUMERICAL-DIAGNOSTICS.md`](NUMERICAL-DIAGNOSTICS.md).

## 5. Data structures

The first matrix type is a small dependency-free `DenseMatrix`. It exists to keep the educational/classical algorithms understandable. It is deliberately not presented as an HPC replacement for BLAS/LAPACK.

A later backend boundary may allow selected operations to delegate to optimized native providers while preserving the SASD public contract.

Result objects that expose arrays or matrices should prefer defensive ownership/copies where accidental caller mutation would invalidate a completed numerical result. Performance-sensitive alternatives can be introduced later behind explicit contracts rather than silently weakening result immutability.

## 6. Numerical policy

Tolerances are explicit. Algorithms must not hide convergence criteria in global mutable state. Tests should use problem-appropriate tolerances rather than exact floating-point equality.

`NumericConstants.DefaultTolerance` is a convenience default, not a global accuracy guarantee. `NumericConstants.NearlyZero` is an absolute numerical guard threshold, not machine epsilon and not a universal approximate-equality rule.

Implementations should prefer stable formulations (for example partial pivoting or scaled norm calculations) while compatibility variants may also be supplied when the historical catalog explicitly included a less stable method.

Readable reference implementations are preferred over premature SIMD, allocation elimination or cache-specific complexity. Optimized backends should remain optional and should preserve the public mathematical contracts.

## 7. Extension strategy

The Borland-compatible V1 is a milestone, not the final architecture. New mathematical functionality should be added according to mathematical domain and dependency direction, not according to the chapter numbers of the historical manual.

M3 may add reusable scaled tolerance/comparison abstractions, statistics primitives, geometry and simulation support. Such abstractions should generalize actual repeated needs instead of retroactively wrapping every simple V1 scalar tolerance in unnecessary framework code.
