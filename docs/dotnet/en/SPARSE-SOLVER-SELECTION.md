# Sparse solver selection and release-reference cases

The 2026 sparse layer contains several Krylov methods with deliberately different mathematical contracts. This note records how they fit together, the release-reference cases used by the managed implementation, and the architectural decisions made during the M3.2 consolidation pass.

## Start with matrix structure

Solver choice should begin with what is known about the matrix, not with whichever algorithm name is most familiar.

| Matrix / workload | First SASD choice | Why |
|---|---|---|
| Symmetric positive definite (SPD) | PCG, usually with Jacobi as a baseline | Short recurrence, low memory, exploits SPD structure |
| SPD, already well scaled or very small iteration count expected | CG | Avoids preconditioner setup/application cost |
| General nonsymmetric, residual robustness is the priority | Restarted GMRES | Explicit residual minimization in the current Krylov space |
| General nonsymmetric, Krylov-basis memory is a concern | BiCGSTAB | Fixed number of O(n) work vectors |

These are starting rules, not universal performance rankings. Matrix spectrum, scaling, restart length, preconditioner quality and matrix-vector-product cost can change the practical result.

## Shared convergence contract

All sparse iterative solvers use the same public stopping rule:

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

The result type reports the initial residual norm, final residual norm, right-hand-side norm, effective threshold, iteration count and termination status. A solver must not return `Converged` solely because an inexpensive recursive/projected residual became small. The physical residual is explicitly recomputed before success is reported.

`SparseMatrixDiagnostics.ResidualEuclideanNorm` exposes the same physical quantity independently of any solver. Applications and release tests can therefore verify a returned solution by recomputing `||b-A*x||2` directly. The diagnostic also supports rectangular matrices and uses scaled accumulation so large finite residual components are not lost to avoidable intermediate overflow.

This common public contract is more important than forcing every solver to share the same internal recurrence. CG/PCG, Arnoldi/Givens GMRES and BiCGSTAB have genuinely different numerical state and remain readable as separate algorithms.

## Preconditioning contract

`ISparsePreconditioner` represents an approximate inverse operation rather than an explicitly materialized inverse matrix.

- PCG requires a preconditioner compatible with the SPD recurrence.
- GMRES and BiCGSTAB use right preconditioning and accept a broader fixed linear approximate-inverse operation.
- `JacobiPreconditioner` is the first inexpensive baseline and is not claimed to be optimal for every problem.

Stronger incomplete-factorization preconditioners remain deferred until real SASD workloads justify their complexity.

## Structured release-reference cases

Small unit tests remain useful for exact edge cases, but a release candidate also needs deterministic systems large enough to exercise repeated Krylov steps and restart/preconditioning paths. `SparseSolverReferenceTests` therefore includes three cross-solver regression scenarios:

1. A 64x64 symmetric positive-definite tridiagonal system with a known nontrivial solution. CG, PCG, restarted GMRES and BiCGSTAB must all recover the same solution and satisfy the same true-residual contract.
2. A 48x48 diagonally dominant nonsymmetric tridiagonal system. Right-preconditioned GMRES and BiCGSTAB start from deliberately different initial guesses and must converge to the same known solution.
3. A separate 40x40 nonsymmetric system independently recomputes `b-A*x` after GMRES and BiCGSTAB, verifying that `SparseLinearSolveResult.ResidualNorm` represents the physical residual rather than only an internal recurrence estimate.

`SparseDiagnosticsTests` separately verifies rectangular residuals, large finite residual norms and validation/arithmetic boundaries for the public residual diagnostic.

These are regression/reference cases, **not benchmarks**. They make no speed claim and intentionally avoid asserting that one Krylov method should take fewer iterations than another.

## Release-facing sample

`SparseLinearAlgebraExample` in the .NET sample project adds a compact release smoke case. It solves one deterministic 24x24 nonsymmetric tridiagonal system with both right-preconditioned restarted GMRES and BiCGSTAB, compares both results with the known solution and recomputes the physical residual independently.

The normal sample executable runs this case after generating the HTML/SVG report and prints iteration/residual diagnostics. This ensures a routine sample run exercises the modern sparse API as well as the classical graphical demonstrations.

## Memory and allocation model

The managed reference implementation keeps sparse storage in canonical CSR and does not materialize dense matrices inside iterative solves.

- CG/PCG use a fixed number of O(n) vectors.
- BiCGSTAB also uses a fixed number of O(n) vectors.
- Restarted GMRES stores an Arnoldi basis whose memory grows with `restartLength`; restarting places an explicit bound on that growth.
- `CsrMatrix.Multiply(ReadOnlySpan<double>, Span<double>)` lets solver work buffers be reused instead of allocating a result vector for every matrix-vector product.
- Public post-solve diagnostics may allocate a temporary vector for clarity; they are not part of the solver hot loop.

No throughput or allocation percentage is claimed here. Dedicated benchmark work remains part of the later performance-backend milestone.

## Consolidation decision on shared helper code

The consolidation audit found repeated low-level operations in the Krylov implementations, such as scaled vector norms, compensated dot products, preconditioner application and true-residual recomputation. They are intentionally **not** all collapsed into one large internal utility at this release boundary.

The reason is architectural rather than accidental duplication: each solver attaches different breakdown semantics and diagnostic context to those operations, and keeping those checks close to the mathematical recurrence currently makes the algorithms easier to audit. The shared behavior that matters to callers is centralized in public contracts (`SparseIterativeSolverOptions`, `SparseLinearSolveResult`, `ISparsePreconditioner` and the residual diagnostic). A smaller internal numerical kernel can still be extracted later if profiling or maintenance shows a concrete benefit without obscuring solver-specific failure handling.

## M3.2 release boundary — foundation complete

The M3.2 managed reference foundation now consists of:

- canonical immutable CSR;
- sparse matvec, transpose and inexpensive norms;
- shared convergence/result semantics;
- solver-neutral preconditioner application;
- Jacobi preconditioning;
- CG and PCG for SPD systems;
- restarted right-preconditioned GMRES;
- right-preconditioned BiCGSTAB;
- public independent true-residual diagnostics;
- structured cross-solver regression cases;
- a release-facing sparse sample.

The public naming/XML surface and allocation model have been reviewed for this boundary. Dedicated CSC, ILU/incomplete Cholesky, further Krylov methods and benchmark-driven tuning are explicitly deferred until concrete workloads justify them.

The next repository step is the **repository-wide release-candidate audit**, not another sparse solver.
