# LU factorization

## Purpose

`LuFactorization` provides the reusable direct-factorization building block required by the V1 Numerical Methods compatibility catalog. It is also a foundation for later numerical algorithms that repeatedly solve systems with the same coefficient matrix.

The implementation is independent new code and uses the classical LU decomposition with partial row pivoting.

## Mathematical convention

The factorization uses

```text
P * A = L * U
```

where:

- `A` is the original square matrix,
- `P` is a row-permutation matrix,
- `L` is unit-lower-triangular,
- `U` is upper-triangular.

`LuFactorization.Permutation[i]` identifies which original row appears as row `i` in `P * A`.

## API

Create a factorization with either:

```csharp
var lu = LuFactorization.Decompose(matrix);
```

or the convenience entry point:

```csharp
var lu = LinearSystemSolvers.FactorizeLu(matrix);
```

The factorization can then be reused:

```csharp
var x1 = lu.Solve(b1);
var x2 = lu.Solve(b2);
var determinant = lu.Determinant();
var inverse = lu.Inverse();
```

Multiple right-hand sides can also be supplied as columns of a `DenseMatrix` through `Solve(DenseMatrix)`.

## Design decisions

### Reusable object instead of a one-shot procedure

Historically, a direct factor/solve routine could be exposed as a single procedure. SASD separates factorization from solving because many important algorithms need several solves with the same matrix. Examples include inverse iteration, inverse construction and later optimization/statistics routines.

### Compact internal storage

`L` and `U` are stored together internally. Values below the diagonal belong to `L`, while the diagonal and upper triangle belong to `U`; the diagonal of `L` is implicitly one. `LowerTriangular` and `UpperTriangular` return independent matrices for inspection and testing.

### Partial pivoting

At every elimination step, the implementation chooses the available entry with the largest absolute value in the current column. This is the classical partial-pivoting strategy and is substantially safer than unpivoted elimination while keeping the implementation understandable.

### Singular matrices

`Decompose` throws `ArithmeticException` when no acceptable pivot remains. The threshold is configurable with `pivotTolerance`. This is an input/numerical-condition failure, not a normal iterative termination condition.

## Integration with existing code

`LinearSystemSolvers.Inverse` now uses one LU factorization and solves for all columns of the identity matrix. `EigenSolvers.InversePowerMethod` also factors the matrix once and reuses it during all inverse-iteration steps.

This is not intended as premature optimization. Reuse is part of the mathematical abstraction: factorization is the expensive setup phase and triangular solves are the repeated operation.

## Current scope and future work

The implementation intentionally remains a small dense reference implementation. It does not yet include scaled pivoting, condition estimation, sparse storage or BLAS/LAPACK acceleration. Those capabilities can be added later behind compatible abstractions when the SASD projects actually need them.
