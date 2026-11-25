# Sparse linear algebra — CSR, CG/PCG, GMRES and BiCGSTAB

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
```

The shared sparse stopping rule is

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Symmetry is checked by default. A strictly positive diagonal is also required because it is necessary for positive definiteness. These inexpensive checks cannot prove that an arbitrary sparse matrix is SPD; CG therefore additionally reports `NumericalBreakdown` if the search-direction curvature `p^T*A*p` becomes non-positive or non-finite.

### True residual verification

CG updates its residual recursively for efficiency. Floating-point roundoff can make that recursive residual differ from the real `b-A*x`. Before returning `Converged`, the SASD implementation therefore recomputes the true residual. If it is still above the requested threshold, CG restarts from the refreshed residual instead of reporting false convergence.

## Preconditioned Conjugate Gradient

Poor scaling or an unfavorable spectrum can make CG require many iterations even when the matrix is SPD. A preconditioner applies an approximate inverse operation

```text
z = M^-1 * r
```

so the Krylov iteration sees a numerically easier problem.

The public `ISparsePreconditioner` interface is solver-neutral. The first implementation is `JacobiPreconditioner`, which uses the inverse matrix diagonal:

```csharp
var jacobi = JacobiPreconditioner.Create(a);
var result = ConjugateGradientSolver.SolvePreconditioned(
    a,
    rightHandSide,
    jacobi,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

Jacobi is cheap to build and apply, creates no per-application arrays, and is especially useful as a scaling baseline. It is not guaranteed to reduce the iteration count for every problem; preconditioning quality is problem dependent.

For PCG, the preconditioner must itself be SPD. `JacobiPreconditioner` has that property when built from an SPD matrix. A custom preconditioner that makes `r^T*M^-1*r` non-positive or non-finite causes `NumericalBreakdown` rather than silent continuation with an invalid recurrence.

## Restarted GMRES for general square systems

`GmresSolver` is the general nonsymmetric counterpart to CG/PCG. It does not require symmetry or positive definiteness and minimizes the residual over a Krylov subspace generated by the Arnoldi process.

```csharp
var result = GmresSolver.Solve(
    matrix,
    rightHandSide,
    restartLength: 30,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

Full GMRES stores one additional basis vector per iteration. `restartLength` bounds that memory growth. At a restart boundary the current least-squares correction is applied, the true residual is recomputed and the next Krylov cycle begins from that residual. A small restart length saves memory but can slow or stall convergence; a larger value preserves more Krylov information.

The Arnoldi basis uses two modified-Gram-Schmidt passes to reduce avoidable loss of orthogonality. Givens rotations update the small upper-Hessenberg least-squares system incrementally. The projected residual is used as an inexpensive trigger, but `Converged` is returned only after explicitly recomputing the true `b-A*x` residual.

### Right-preconditioned GMRES

The same solver-neutral preconditioner abstraction is reused through `SolvePreconditioned`. GMRES applies it on the right:

```text
A * M^-1 * y = b
```

This keeps the minimized and reported residual in the original system. Unlike PCG, GMRES does not require the preconditioner to be SPD.

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = GmresSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi,
    restartLength: 30);
```

A terminated Arnoldi expansion is not automatically called success. If the Krylov space cannot expand and the explicitly recomputed residual is still above tolerance, the result is `NumericalBreakdown` with the best available solution.

## BiCGSTAB for bounded-memory nonsymmetric solves

`BiCgStabSolver` is another general nonsymmetric Krylov solver, but unlike GMRES it uses a short recurrence and therefore keeps a fixed number of O(n) work vectors. This can be attractive when restart-basis memory matters.

```csharp
var result = BiCgStabSolver.Solve(
    matrix,
    rightHandSide,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

BiCGSTAB does not directly minimize a growing least-squares problem. Its residual may therefore be less smooth than GMRES, and its short recurrence contains scalar breakdown points. A zero/non-finite shadow-residual product, alpha denominator or stabilization coefficient is reported as `NumericalBreakdown` rather than being confused with an iteration limit.

### Right-preconditioned BiCGSTAB

BiCGSTAB reuses `ISparsePreconditioner` and applies `M^-1` to its search directions before multiplying by `A`:

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = BiCgStabSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi);
```

As with GMRES, the preconditioner need not be SPD. The solver still reports convergence using the true residual of the original equation. If a recursive residual first appears converged but explicit recomputation disagrees, BiCGSTAB restarts from the refreshed true residual.

### Choosing between the sparse solvers

A useful first decision rule is:

- **CG/PCG** for SPD systems;
- **restarted GMRES** for general nonsymmetric systems when robust residual minimization is the priority;
- **BiCGSTAB** for general nonsymmetric systems when bounded basis memory is especially important and its convergence behavior is acceptable for the problem.

Do not choose only by iteration count. Memory, matvec cost, preconditioner quality and the reported true residual all matter.

### Solver diagnostics

`SparseLinearSolveResult` is shared by CG, PCG, GMRES and BiCGSTAB. It exposes the best available solution, `IterationStatus`, completed iteration count, initial/final residual norms, the right-hand-side norm, the effective convergence threshold and the relative residual norm. `MaximumIterationsReached` therefore remains quantitatively inspectable rather than collapsing to a Boolean failure.

## What sparse linear algebra does not yet provide

CSR, CG/PCG, the common preconditioner abstraction, Jacobi preconditioning, restarted GMRES and BiCGSTAB are now implemented. The next step is an architecture/reference-case consolidation round before the release candidate. More powerful preconditioners such as incomplete Cholesky/ILU and a dedicated CSC type remain deferred until concrete workloads justify them.

The distinction matters: sparse storage is a data-layout decision, solver choice depends on mathematical structure, and preconditioner choice depends on both the solver contract and the matrix.
