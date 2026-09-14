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

## 4. Error and convergence model

Programmer/input-contract violations use normal exceptions (`ArgumentException`, `ArgumentOutOfRangeException`). Expected iterative outcomes use result objects with a status such as:

- `Converged`
- `MaximumIterationsReached`
- `NumericalBreakdown`
- `NotBracketed`

This prevents normal non-convergence from being confused with programming errors.

## 5. Data structures

The first matrix type is a small dependency-free `DenseMatrix`. It exists to keep the educational/classical algorithms understandable. It is deliberately not presented as an HPC replacement for BLAS/LAPACK.

A later backend boundary may allow selected operations to delegate to optimized native providers while preserving the SASD public contract.

## 6. Numerical policy

Tolerances are explicit. Algorithms must not hide convergence criteria in global mutable state. Tests should use problem-appropriate tolerances rather than exact floating-point equality.

Implementations should prefer stable formulations (for example partial pivoting) while compatibility variants may also be supplied when the historical catalog explicitly included a less stable method.

## 7. Extension strategy

The Borland-compatible V1 is a milestone, not the final architecture. New mathematical functionality should be added according to mathematical domain and dependency direction, not according to the chapter numbers of the historical manual.
