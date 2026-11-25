# Sparse CSR foundation

## Purpose

`CsrMatrix` is the first data-structure milestone in the 2026 sparse-linear-algebra layer. It is intentionally implemented before Conjugate Gradient, GMRES or preconditioners so solver code can depend on one explicit, tested storage contract rather than inventing private sparse formats.

The implementation lives in `Sasd.Numerics.LinearAlgebra.Sparse` and is immutable after construction.

## Canonical storage contract

A matrix is stored with the usual three CSR arrays:

- `rowPointers`, length `Rows + 1`;
- `columnIndices`, length `NonZeroCount`;
- `values`, length `NonZeroCount`.

Canonical SASD CSR has these additional invariants:

1. row pointers start at zero, end at `NonZeroCount`, and are non-decreasing;
2. column indices are strictly increasing inside each row;
3. explicit zero values are not stored;
4. every stored value is finite;
5. public factories copy caller-owned arrays.

These rules remove duplicate structural representations of the same matrix and allow later solvers to traverse rows without repeated structural checks.

## Construction

### Coordinate assembly

`FromEntries(rows, columns, entries)` accepts arbitrary coordinate order. Duplicate coordinates are summed in enumeration order and exact cancellation removes the coordinate. The final compressed layout is row-major and sorted by column within each row.

This path is convenient for finite-element-style or local-contribution assembly. It is not intended to be the final high-throughput builder API for matrices with hundreds of millions of inserts; such a builder can be added when a real consumer needs it.

### Dense conversion

`FromDense` uses an explicit **absolute** zero tolerance. The default `0.0` preserves every non-zero dense entry. Passing a positive threshold is an intentional lossy sparsification operation and should therefore be selected from problem scale, not treated as a universal epsilon.

### Existing CSR interoperability

`FromCompressedRows` accepts already compressed arrays but validates the complete canonical contract before copying them. This is the preferred boundary for future file-format, Fortran/C++ or native-library adapters.

## Sparse matrix-vector multiplication

`Multiply(IReadOnlyList<double>)` is the convenient allocating API. An additional span-based overload writes into caller-provided storage so iterative solvers can reuse work vectors:

```csharp
var y = new double[a.Rows];
a.Multiply(x, y);
```

Input/output overlap is supported by snapshotting the input before destination writes. Non-finite products or accumulation overflow are reported rather than silently poisoning an iterative method with NaN/Infinity.

## Transpose and conversion

`Transpose()` produces another canonical CSR matrix without building a dense intermediate. `ToDense()` is primarily an interoperability, testing and diagnostic helper; sparse algorithms should not depend on it internally.

`CopyRowPointers`, `CopyColumnIndices` and `CopyValues` expose defensive copies for serialization or adapters without weakening immutability.

## Norms

`SparseMatrixNorms` currently provides max-absolute-entry, 1-norm, infinity-norm and Frobenius norm without dense materialization. The Frobenius norm uses scaled sum-of-squares accumulation. The 1-norm keeps workspace proportional to non-empty columns rather than the declared column count.

A sparse spectral norm is deliberately deferred. Computing it efficiently belongs with iterative sparse eigensolver/solver infrastructure rather than with the storage primitive.

## Next solver step

The next planned layer is Conjugate Gradient for symmetric positive-definite sparse systems. It should:

- reuse the span-based CSR matvec;
- reuse existing `IterationStatus` and residual conventions;
- expose relative/absolute stopping criteria explicitly;
- report numerical breakdown rather than hiding it;
- establish the preconditioner abstraction only when the solver genuinely needs it.

Dedicated CSC storage remains a later option when a column-oriented consumer justifies the additional representation. `Transpose()` already covers many occasional column-access workflows without doubling the core sparse API prematurely.
