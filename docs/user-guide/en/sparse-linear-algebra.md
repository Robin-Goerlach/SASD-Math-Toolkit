# Sparse linear algebra — CSR and Conjugate Gradient

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

The second form is used by iterative solvers so repeated matrix-vector products do not allocate a new result vector on every iteration.

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

## Conjugate Gradient for SPD systems

`ConjugateGradientSolver` solves sparse systems whose matrix is **symmetric positive definite**. It is not a generic replacement for LU or a nonsymmetric Krylov solver.

```csharp
var result = ConjugateGradientSolver.Solve(
    a,
    rightHandSide,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });

if (result.Converged)
{
    var solution = result.Solution;
}
```

The shared sparse stopping rule is

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

so the same semantics can later be reused by GMRES and BiCGSTAB.

Symmetry is checked by default. A strictly positive diagonal is also required because it is necessary for positive definiteness. These inexpensive checks cannot prove that an arbitrary sparse matrix is SPD; CG therefore additionally reports `NumericalBreakdown` if the search-direction curvature `p^T*A*p` becomes non-positive or non-finite.

### True residual verification

CG updates its residual recursively for efficiency. Floating-point roundoff can make that recursive residual differ from the real `b-A*x`. Before returning `Converged`, the SASD implementation therefore recomputes the true residual. If it is still above the requested threshold, CG restarts from the refreshed residual instead of reporting a false convergence.

### Solver diagnostics

`SparseLinearSolveResult` exposes the best available solution, `IterationStatus`, completed iteration count, initial/final residual norms, the right-hand-side norm, the effective convergence threshold and the relative residual norm. A result with `MaximumIterationsReached` can therefore still be inspected quantitatively instead of being reduced to a success/failure Boolean.

## What sparse linear algebra does not yet provide

CSR and unpreconditioned Conjugate Gradient are now implemented. The next layers are preconditioning and general nonsymmetric Krylov methods such as GMRES and BiCGSTAB. A dedicated CSC type remains deferred until sustained column-oriented workloads justify maintaining a second sparse storage representation.

The distinction matters: sparse storage is a data-layout decision, while solver choice depends on mathematical structure. An SPD system is a good CG problem; a general nonsymmetric matrix is not.
