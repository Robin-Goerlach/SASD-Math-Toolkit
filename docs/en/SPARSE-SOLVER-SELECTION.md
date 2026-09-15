# Sparse solver selection and reference cases

The 2026 sparse layer now contains several Krylov methods with deliberately different mathematical contracts. This note records how they fit together and defines the structured regression cases used before the next release candidate.

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

This common contract is more important for application code than forcing every solver to share the same internal recurrence. CG/PCG, Arnoldi/Givens GMRES and BiCGSTAB have genuinely different numerical state and should remain readable as separate algorithms.

## Preconditioning contract

`ISparsePreconditioner` represents an approximate inverse operation rather than an explicitly materialized inverse matrix.

- PCG requires a preconditioner compatible with the SPD recurrence.
- GMRES and BiCGSTAB use right preconditioning and accept a broader fixed linear approximate-inverse operation.
- `JacobiPreconditioner` is the first inexpensive baseline and is not claimed to be optimal for every problem.

Stronger incomplete-factorization preconditioners remain deferred until real SASD workloads justify their complexity.

## Structured release-reference cases

Small unit tests remain useful for exact edge cases, but a release candidate also needs deterministic systems large enough to exercise repeated Krylov steps and restart/preconditioning paths. `SparseSolverReferenceTests` therefore adds three cross-solver regression scenarios:

1. A 64x64 symmetric positive-definite tridiagonal system with a known nontrivial solution. CG, PCG, restarted GMRES and BiCGSTAB must all recover the same solution and satisfy the same true-residual contract.
2. A 48x48 diagonally dominant nonsymmetric tridiagonal system. Right-preconditioned GMRES and BiCGSTAB start from deliberately different initial guesses and must converge to the same known solution.
3. A separate 40x40 nonsymmetric system independently recomputes `b-A*x` after GMRES and BiCGSTAB, verifying that `SparseLinearSolveResult.ResidualNorm` represents the physical residual rather than only an internal recurrence estimate.

These are regression/reference cases, **not benchmarks**. They make no speed claim and intentionally avoid asserting that one Krylov method should take fewer iterations than another.

## Memory model

The managed reference implementation keeps sparse storage in canonical CSR and does not materialize dense matrices inside iterative solves.

- CG/PCG use a fixed number of O(n) vectors.
- BiCGSTAB also uses a fixed number of O(n) vectors.
- Restarted GMRES stores an Arnoldi basis whose memory grows with `restartLength`; restarting places an explicit bound on that growth.

This is why GMRES exposes restart length as a public parameter rather than hiding it as an implementation constant.

## M3.2 release boundary

The current M3.2 foundation consists of:

- canonical immutable CSR;
- sparse matvec, transpose and inexpensive norms;
- shared convergence/result semantics;
- solver-neutral preconditioner application;
- Jacobi preconditioning;
- CG and PCG for SPD systems;
- restarted right-preconditioned GMRES;
- right-preconditioned BiCGSTAB;
- structured cross-solver regression cases.

Before the release candidate, one final sparse consolidation pass should review public naming/XML documentation, duplicated internal numerical helpers, allocation behavior and the release-facing examples. CSC, ILU/incomplete Cholesky and additional Krylov methods are explicitly outside this release boundary unless that audit uncovers a concrete blocker.
