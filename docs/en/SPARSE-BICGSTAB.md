# Sparse BiCGSTAB solver

## Purpose

`BiCgStabSolver` adds a second general nonsymmetric Krylov path beside restarted GMRES. It solves real square sparse systems

```text
A * x = b
```

without requiring symmetry or positive definiteness. The method uses a short recurrence and therefore keeps a fixed number of work vectors instead of storing a growing Arnoldi basis.

BiCGSTAB is useful when GMRES basis memory would be undesirable and the BiCGSTAB recurrence behaves well. It is not a universal replacement for GMRES: its residual history can be less smooth and it has more algebraic breakdown points.

## Shared convergence contract

BiCGSTAB reuses `SparseIterativeSolverOptions` and `SparseLinearSolveResult`. Convergence is always defined by the true Euclidean residual of the original system:

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

The recurrence maintains a cheap residual vector, but `Converged` is returned only after `b-A*x` has been recomputed explicitly. If the recursive residual reaches tolerance but the true residual does not, the solver restarts the short recurrence from the refreshed physical residual.

## Right preconditioning

`SolvePreconditioned` reuses the solver-neutral `ISparsePreconditioner` operation. Search directions are transformed with `M^-1` before matrix multiplication, while the reported residual remains the residual of the original equation.

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = BiCgStabSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

Unlike PCG, BiCGSTAB does not require an SPD preconditioner. The operation should nevertheless be fixed, deterministic and numerically finite during one solve.

## Breakdown diagnostics

The short recurrence contains scalar divisions that can become undefined even for a finite matrix and right-hand side. The implementation reports `IterationStatus.NumericalBreakdown` rather than manufacturing a result when, for example:

- the shadow-residual product becomes zero or non-finite;
- the alpha denominator becomes zero or non-finite;
- the stabilization denominator vanishes before convergence;
- the stabilization coefficient `omega` becomes zero or non-finite;
- a preconditioner or sparse matrix-vector product produces a non-finite value.

This is an important difference from ordinary non-convergence. `MaximumIterationsReached` means the recurrence remained numerically meaningful but did not meet the requested tolerance within the budget. `NumericalBreakdown` means the BiCGSTAB recurrence itself could not continue safely.

## Memory and algorithm choice

BiCGSTAB keeps O(n) vector storage independent of iteration count. Restarted GMRES stores O(m*n) basis data for restart length `m`. That makes BiCGSTAB attractive for memory-sensitive large systems, but GMRES is generally easier to reason about because it directly minimizes a projected residual and tends to have smoother convergence behavior.

A practical first choice is therefore:

- SPD system: CG/PCG;
- general nonsymmetric system where robustness is preferred: restarted GMRES;
- general nonsymmetric system where bounded vector memory is especially important: BiCGSTAB, while inspecting residual diagnostics.

## Performance policy

The current implementation is a dependency-free managed reference solver. Work arrays are allocated once per solve and reused across iterations; sparse matrices are never materialized densely. SIMD, parallel reductions, ILU-class preconditioners and native sparse backends remain benchmark-driven later work.
