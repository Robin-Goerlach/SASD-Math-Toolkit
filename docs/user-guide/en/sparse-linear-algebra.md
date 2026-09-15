# Sparse linear algebra — CSR foundation

Dense matrices reserve storage for every matrix position. That is the right representation for many small and medium problems, but it becomes wasteful when a large matrix contains only a small number of non-zero coefficients per row. `CsrMatrix` is the first SASD sparse representation for such workloads.

## Creating a sparse matrix

For coordinate-style assembly:

```csharp
using Sasd.Numerics.LinearAlgebra.Sparse;

var a = CsrMatrix.FromEntries(
    4,
    4,
    [
        new SparseMatrixEntry(0, 0, 4.0),
        new SparseMatrixEntry(0, 1, -1.0),
        new SparseMatrixEntry(1, 0, -1.0),
        new SparseMatrixEntry(1, 1, 4.0),
        new SparseMatrixEntry(1, 2, -1.0),
        new SparseMatrixEntry(2, 1, -1.0),
        new SparseMatrixEntry(2, 2, 4.0),
        new SparseMatrixEntry(2, 3, -1.0),
        new SparseMatrixEntry(3, 2, -1.0),
        new SparseMatrixEntry(3, 3, 4.0)
    ]);
```

The matrix is immutable after construction. Entries are stored in canonical CSR form: sorted columns within each row, no duplicate coordinates and no explicit zero values.

Duplicate coordinates are added during `FromEntries`. This is useful when several local contributions target the same global matrix coefficient. If they cancel exactly, the coordinate disappears from compressed storage.

## Matrix-vector multiplication

The ordinary convenience call allocates a result vector:

```csharp
var y = a.Multiply(x);
```

Iterative algorithms should normally reuse work buffers instead:

```csharp
var y = new double[a.Rows];
a.Multiply(x, y);
```

The second form is why CSR was established before the first iterative sparse solver: Conjugate Gradient will be able to perform repeated matrix-vector products without allocating a new vector for every iteration.

## Dense conversion and sparsification

`CsrMatrix.FromDense(dense)` stores every non-zero dense entry. A positive `absoluteZeroTolerance` deliberately discards smaller coefficients:

```csharp
var sparse = CsrMatrix.FromDense(dense, absoluteZeroTolerance: 1e-10);
```

This threshold is absolute. Choosing it is part of the numerical model; do not assume one epsilon is suitable for matrices with very different physical scales.

`ToDense()` is useful for inspection, interoperability and tests, but sparse algorithms should not repeatedly convert back to dense storage.

## Transpose

`Transpose()` returns canonical CSR for the transposed matrix without first allocating a dense matrix. A dedicated CSC type may be added later when sustained column-oriented workloads justify it.

## Sparse norms

`SparseMatrixNorms` provides inexpensive norms directly from compressed storage:

```csharp
var one = SparseMatrixNorms.OneNorm(a);
var infinity = SparseMatrixNorms.InfinityNorm(a);
var frobenius = SparseMatrixNorms.FrobeniusNorm(a);
```

The Frobenius implementation is scale-aware, so large finite entries do not overflow merely because an intermediate implementation squared them naively.

## What CSR does not yet provide

This milestone establishes data representation and arithmetic, not a complete sparse package. There is not yet a Conjugate Gradient, GMRES, BiCGSTAB or preconditioner API. Those algorithms will be layered on this storage contract while reusing the toolkit's existing iteration-status and residual conventions.

The distinction matters: sparse storage is a data-layout decision, while solver choice depends on mathematical structure. A symmetric positive-definite sparse system should eventually use a different iterative method from a general nonsymmetric one.
