# Sparse preconditioning

## Purpose

Preconditioning changes the numerical geometry of an iterative linear solve without changing the mathematical solution. Instead of asking a Krylov method to work directly with a difficult system, an approximate inverse operation is applied so the transformed problem is easier to iterate on.

The SASD sparse layer represents that operation through `ISparsePreconditioner`:

```csharp
public interface ISparsePreconditioner
{
    int Size { get; }
    void Apply(ReadOnlySpan<double> source, Span<double> destination);
}
```

The abstraction deliberately exposes an operation, not an explicit inverse matrix. Iterative solvers normally need `z = M^-1*r`; materializing `M^-1` would often be more expensive and less sparse than applying an approximate inverse directly.

## Jacobi preconditioner

`JacobiPreconditioner` is the first concrete implementation. It stores the reciprocal diagonal of a square CSR matrix and applies

```text
z_i = r_i / A_ii
```

Construction is O(n) plus row traversal, storage is O(n), and each application is O(n). This makes Jacobi a useful baseline for validating the common preconditioner architecture before adding more expensive methods such as incomplete Cholesky or ILU.

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = ConjugateGradientSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi);
```

The default diagonal threshold is zero: exact missing/zero diagonal entries are rejected. A positive `absoluteDiagonalTolerance` can deliberately reject very small diagonal values. The threshold is explicitly absolute because one global epsilon cannot represent every physical or numerical scale.

If a reciprocal diagonal would overflow the finite `double` range, construction fails rather than storing `Infinity` in a solver component.

## PCG contract

Preconditioned Conjugate Gradient (PCG) requires both the system matrix and the preconditioner to be symmetric positive definite. For an SPD system matrix, the Jacobi preconditioner is SPD because the diagonal entries are strictly positive.

A custom `ISparsePreconditioner` can violate that contract. The solver therefore checks the recurrence quantity

```text
r^T * M^-1 * r
```

and reports `IterationStatus.NumericalBreakdown` if it is non-positive or non-finite. It also retains the existing CG curvature check `p^T*A*p > 0`.

These checks do not prove all mathematical properties of an arbitrary custom preconditioner, but they prevent the most dangerous contract violations from silently contaminating the iteration.

## Why the interface is solver-neutral

The abstraction is intentionally not named `ICgPreconditioner`. Later GMRES and BiCGSTAB implementations can reuse the same application contract even though those solvers do not require an SPD preconditioner. Solver-specific mathematical constraints remain documented and checked at the solver boundary.

This keeps future implementations such as Jacobi, block-Jacobi, incomplete Cholesky or ILU reusable where their mathematical properties fit the consuming solver.

## Performance position

Preconditioner application is designed for repeated use with caller-provided spans. `JacobiPreconditioner` performs no per-application allocation and supports overlapping source/destination storage because every result component depends only on the corresponding input component.

More sophisticated preconditioners will only be added with explicit storage, breakdown and reuse contracts. The next solver milestone is restarted GMRES, followed by BiCGSTAB and a sparse architecture consolidation round.
