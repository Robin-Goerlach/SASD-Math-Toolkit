# Restarted GMRES

## Scope

`GmresSolver` solves real square sparse systems

```text
A * x = b
```

without requiring symmetry or positive definiteness. It is the general nonsymmetric Krylov solver that complements CG/PCG in the SASD sparse layer.

The implementation is deliberately a readable managed reference implementation. It never materializes the sparse matrix as dense storage and builds its Krylov basis only through repeated CSR matrix-vector products.

## Arnoldi process and least-squares problem

GMRES constructs an orthonormal basis of a Krylov subspace with the Arnoldi process. The SASD implementation uses modified Gram-Schmidt and performs a second orthogonalization pass. This costs extra dot products but reduces avoidable loss of basis orthogonality in the reference implementation.

The resulting small upper-Hessenberg least-squares problem is updated incrementally with Givens rotations. The residual estimate is therefore available without solving a new dense least-squares problem from scratch on every inner iteration.

## Restarting

A full GMRES basis grows by one vector per iteration. That can become expensive for large sparse systems. The public `restartLength` parameter limits the basis size:

```csharp
var result = GmresSolver.Solve(
    matrix,
    rightHandSide,
    restartLength: 30,
    options: new SparseIterativeSolverOptions
    {
        RelativeTolerance = 1e-10,
        AbsoluteTolerance = 1e-12,
        MaximumIterations = 1000
    });
```

At the end of a restart cycle the current least-squares correction is applied, the true residual is recomputed, and a new Krylov basis begins from that residual.

A smaller restart length reduces memory usage but can slow or stall convergence. A larger value retains more Krylov information but stores more basis vectors. The toolkit therefore keeps this choice explicit.

## Right preconditioning

`SolvePreconditioned` uses **right preconditioning**:

```text
A * M^-1 * y = b
x = M^-1 * y
```

Conceptually, the Arnoldi basis is built for `A*M^-1`. In implementation terms each basis vector `v_j` is first mapped to

```text
z_j = M^-1 * v_j
```

and the matrix-vector product uses `A*z_j`. The final physical solution correction is assembled from the stored `z_j` vectors.

This choice is important because the minimized residual remains the original residual `b-A*x`. The shared SASD convergence rule therefore stays unchanged:

```text
||b-A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Unlike PCG, GMRES does not require an SPD preconditioner. The supplied `ISparsePreconditioner` is expected to behave as a fixed deterministic linear approximate inverse during one solve.

Example:

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = GmresSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi,
    restartLength: 30);
```

## Residual verification and breakdown

The Givens-updated projected residual is inexpensive and is used to decide when a candidate solution should be inspected. `Converged` is returned only after the implementation explicitly recomputes the true residual `b-A*x`.

The solver reports `NumericalBreakdown` rather than fabricating progress when, for example:

- a matrix-vector product becomes non-finite;
- Arnoldi orthogonalization becomes non-finite;
- the projected triangular system becomes singular/non-finite;
- the Krylov space terminates (`happy breakdown`) but the true residual still exceeds the requested tolerance;
- a preconditioner produces a non-finite result.

A `MaximumIterationsReached` result still carries the best candidate solution and true residual diagnostics.

## Numerical design choices

The managed reference implementation intentionally favors clarity and diagnostics over aggressive tuning:

- CSR matrix-vector products remain sparse;
- basis orthogonalization uses two modified Gram-Schmidt passes;
- inner products use compensated summation;
- Euclidean norms use scaled accumulation to reduce avoidable overflow/underflow;
- Givens rotations use scale-aware magnitude formation;
- no speculative SIMD, parallel reductions or native sparse backend is required.

These choices establish stable semantics first. Benchmark-driven optimization can follow later without changing the public convergence/result contract.

## Position in M3.2

With restarted GMRES, the sparse layer now covers both major structural cases:

- CG/PCG for SPD systems;
- GMRES for general square systems.

The next planned Krylov slice is BiCGSTAB, followed by a sparse-architecture consolidation and release-readiness round.
