# Sparse Conjugate Gradient solver

## Scope

`ConjugateGradientSolver` is the first iterative solver on top of the SASD canonical CSR foundation. It solves real sparse systems

```text
A * x = b
```

when `A` is **symmetric positive definite (SPD)**. It never converts the matrix to dense storage and reuses fixed work vectors for all repeated sparse matrix-vector products.

## Convergence rule

Sparse iterative solvers share `SparseIterativeSolverOptions`. CG accepts convergence when the Euclidean true residual satisfies

```text
||b - A*x||2 <= max(absoluteTolerance, relativeTolerance * ||b||2)
```

The result exposes the effective threshold, the initial/final residual norm and the residual relative to `||b||2` so applications can log the actual numerical stopping condition instead of only a Boolean flag.

## SPD contract

CG is not a generic sparse solver. Symmetry is checked by default using `SparseMatrixDiagnostics.IsSymmetric`. The solver also rejects a non-positive diagonal before iteration because positive diagonal entries are necessary for an SPD matrix.

These checks do **not** prove positive definiteness. A full proof would be much more expensive and would defeat the purpose of a lightweight iterative sparse solver. During iteration CG therefore checks the search-direction curvature

```text
p^T * A * p
```

and returns `IterationStatus.NumericalBreakdown` if it becomes non-positive or non-finite. Such a breakdown is a strong indication that the SPD contract was violated or that floating-point roundoff destroyed the recurrence.

## True residual verification

CG updates the residual recursively because recomputing `A*x` on every iteration would double the dominant sparse matrix-vector cost. Recursive residuals can, however, drift from the mathematically true residual through floating-point roundoff.

The SASD implementation therefore recomputes `b-A*x` whenever the recursive residual first claims convergence. Only the verified true residual can produce `IterationStatus.Converged`. If the verification fails the requested threshold, the algorithm restarts from the refreshed residual instead of reporting false convergence.

## Result model

`SparseLinearSolveResult` reuses the common `IterationStatus` vocabulary and provides:

- best available solution;
- completed iteration count;
- final and initial Euclidean residual norms;
- right-hand-side norm;
- effective convergence threshold;
- relative residual norm;
- optional numerical-breakdown / iteration-limit message.

The stored solution is protected from external mutation. `Solution` returns a defensive copy, while `GetSolutionValue` and `CopySolutionTo` avoid unnecessary complete-result copies when callers need finer control.

## Example

```csharp
using Sasd.Numerics.LinearAlgebra.Sparse;

var a = CsrMatrix.FromEntries(
    3,
    3,
    [
        new SparseMatrixEntry(0, 0, 4.0),
        new SparseMatrixEntry(0, 1, -1.0),
        new SparseMatrixEntry(1, 0, -1.0),
        new SparseMatrixEntry(1, 1, 4.0),
        new SparseMatrixEntry(1, 2, -1.0),
        new SparseMatrixEntry(2, 1, -1.0),
        new SparseMatrixEntry(2, 2, 3.0)
    ]);

var result = ConjugateGradientSolver.Solve(
    a,
    [2.0, 4.0, 7.0],
    options: new SparseIterativeSolverOptions
    {
        RelativeTolerance = 1e-10,
        AbsoluteTolerance = 1e-12,
        MaximumIterations = 500
    });

if (result.Converged)
{
    var x = result.Solution;
}
```

## Performance position

The current implementation prioritizes a transparent managed reference solver. It reuses work buffers and does not materialize dense matrices, but it deliberately avoids speculative SIMD, parallel reductions or native sparse backends. Those optimizations should follow benchmarks and stable solver contracts.

The next natural extensions are preconditioning for CG and general nonsymmetric Krylov solvers such as GMRES and BiCGSTAB, all reusing the same options/result semantics.
