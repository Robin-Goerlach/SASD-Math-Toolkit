# Matrices and linear systems

Many numerical methods eventually reduce a problem to a linear system

```text
A * x = b
```

where `A` is a coefficient matrix, `x` is the unknown vector and `b` is the right-hand side. SASD Math Toolkit provides a deliberately small dense-matrix foundation plus direct and iterative solvers suitable for the V1 reference/educational scope.

The implementation is intended to be understandable first. It is not a substitute for a large sparse solver or a tuned BLAS/LAPACK stack.

## 1. `DenseMatrix`

Create a matrix from a rectangular C# array:

```csharp
using Sasd.Numerics.LinearAlgebra;

var matrix = new DenseMatrix(new double[,]
{
    { 4.0, 1.0 },
    { 2.0, 3.0 }
});

Console.WriteLine(matrix.Rows);     // 2
Console.WriteLine(matrix.Columns);  // 2
Console.WriteLine(matrix.IsSquare); // true
```

Entries are mutable:

```csharp
matrix[0, 1] = 1.5;
```

but dimensions are fixed after construction. Matrix entries must be finite. `NaN` and infinity are rejected because they otherwise tend to contaminate every later numerical operation.

`Clone()` returns an independent matrix. Matrix-vector and matrix-matrix multiplication are also available:

```csharp
var y = matrix.Multiply(new[] { 1.0, 2.0 });
var product = matrix.Multiply(DenseMatrix.Identity(2));
```

The multiplication routines intentionally use straightforward loops. If arithmetic overflows, they throw instead of returning a matrix containing infinity.

## 2. Gaussian elimination

For a one-off dense system, `SolveGaussian` is the direct textbook solver:

```csharp
var a = new DenseMatrix(new double[,]
{
    { 2.0, 1.0 },
    { 5.0, 7.0 }
});

var b = new[] { 11.0, 13.0 };
var x = LinearSystemSolvers.SolveGaussian(a, b);
```

The method copies `A` and `b`; caller-owned inputs are not modified.

### Partial pivoting

Partial pivoting is enabled by default and should normally remain enabled. At each elimination step the solver chooses the largest available absolute entry in the pivot column.

For example, a nonsingular matrix can start with a zero diagonal element:

```text
0  2
1  3
```

Unpivoted Gaussian elimination cannot use the first diagonal entry. Partial pivoting swaps the rows and proceeds normally.

The API retains `partialPivoting: false` for historical and educational comparison, not because it is the recommended production setting.

## 3. Reusable LU factorization

If several systems use the same coefficient matrix, do not repeat Gaussian elimination. Factor the matrix once:

```csharp
var lu = LinearSystemSolvers.FactorizeLu(a);

var x1 = lu.Solve(new[] { 11.0, 13.0 });
var x2 = lu.Solve(new[] { 4.0, 9.0 });
```

The convention is

```text
P * A = L * U
```

where `P` is a row permutation, `L` is unit-lower-triangular and `U` is upper-triangular.

For inspection and diagnostics:

```csharp
var lower = lu.LowerTriangular;
var upper = lu.UpperTriangular;
var permutation = lu.Permutation;
var toleranceUsed = lu.PivotTolerance;
```

These exposed matrices/arrays are independent copies where mutation would otherwise compromise the factorization.

Several right-hand sides can be supplied as the columns of one `DenseMatrix`:

```csharp
var bMany = DenseMatrix.Identity(2);
var xMany = lu.Solve(bMany);
```

This is exactly the pattern used when constructing an inverse.

## 4. Determinants

A determinant can be computed directly:

```csharp
var determinant = LinearSystemSolvers.Determinant(a);
```

or from an existing LU factorization:

```csharp
var determinant = lu.Determinant();
```

The LU path is preferable when a factorization already exists because it reuses the diagonal of `U` and the row-permutation parity.

A determinant close to zero can indicate singularity, but determinant magnitude by itself is not a robust condition-number estimate. It also changes strongly with matrix scaling.

## 5. Matrix inverse

The toolkit exposes:

```csharp
var inverse = LinearSystemSolvers.Inverse(a);
```

Internally this creates one LU factorization and solves against all columns of the identity matrix.

However, if the actual task is to solve `A*x=b`, prefer `SolveGaussian` or `LuFactorization.Solve`. Explicitly forming `A^-1` usually performs more work and can amplify numerical error without adding useful information.

Use the inverse when an algorithm genuinely needs the inverse matrix itself, not merely because the symbolic formula says `x=A^-1*b`.

## 6. Pivot tolerance and numerical singularity

Direct solvers use a positive absolute `pivotTolerance`. A candidate pivot whose magnitude is at or below that threshold is treated as numerically zero.

```csharp
var lu = LuFactorization.Decompose(a, pivotTolerance: 1e-12);
```

This is a practical safety threshold, not a full conditioning analysis. A matrix can have pivots larger than the threshold and still be badly conditioned.

For differently scaled problem families, the same absolute tolerance may not be equally meaningful. V1 deliberately keeps the rule explicit and simple; later SASD work can add scaling and condition estimation without changing the basic solve abstraction.

## 7. Residuals

After solving, verify what equation the computed vector actually satisfies:

```csharp
var residual = LinearSystemSolvers.ResidualInfinityNorm(a, x, b);
```

This computes

```text
||A*x - b||∞ = max_i |(A*x-b)_i|
```

A small residual is useful evidence, but it does not by itself prove that `x` is close to the exact solution when `A` is badly conditioned.

This distinction is central:

- **residual** asks how well the computed vector satisfies the supplied equations;
- **forward error** asks how close the vector is to the exact mathematical solution;
- **conditioning** describes how strongly the solution can react to small changes in the input data.

V1 exposes the residual, not a condition estimator.

## 8. Gauss-Seidel iteration

Gauss-Seidel is an iterative alternative:

```csharp
var result = LinearSystemSolvers.GaussSeidel(
    new DenseMatrix(new double[,]
    {
        { 4.0, 1.0 },
        { 2.0, 3.0 }
    }),
    new[] { 1.0, 2.0 },
    tolerance: 1e-12,
    maximumIterations: 200);

if (result.Converged)
{
    Console.WriteLine(result.Value[0]);
    Console.WriteLine(result.Residual);
}
```

The implementation reports convergence only when both the largest component update and the residual infinity norm are within the requested tolerance.

Possible statuses include:

- `Converged` — the V1 convergence checks passed;
- `MaximumIterationsReached` — the iteration budget was exhausted;
- `NumericalBreakdown` — for example a zero/near-zero diagonal or numerical overflow was encountered.

Gauss-Seidel is **not guaranteed to converge for every nonsingular matrix**. Strict diagonal dominance is a common sufficient condition, though not a necessary one. If you simply need a solution to a small arbitrary dense system, pivoted LU is the safer default.

## 9. Exceptions versus iterative status

The linear-algebra API follows the same general SASD convention as the rest of the toolkit.

Invalid contracts such as wrong dimensions, non-finite input values or a non-positive tolerance produce exceptions. A direct factorization that cannot proceed because a usable pivot does not exist also throws `ArithmeticException`.

Gauss-Seidel, on the other hand, is an iterative process. Expected iterative outcomes such as iteration exhaustion or numerical breakdown are represented through `IterativeResult<double[]>` and `IterationStatus`.

## 10. Method selection

A practical V1 rule of thumb is:

| Situation | Preferred starting point |
| --- | --- |
| One small dense system | Pivoted `SolveGaussian` |
| Many right-hand sides with the same `A` | `FactorizeLu` then repeated `Solve` |
| Determinant after an LU already exists | `lu.Determinant()` |
| Need the inverse matrix itself | `lu.Inverse()` / `LinearSystemSolvers.Inverse` |
| Iterative experiment on a suitable matrix | `GaussSeidel` |
| Large/sparse/high-performance problem | Use a specialized backend rather than the V1 reference matrix |

## 11. Numerical limitations

Linear systems are one of the clearest examples of why “the program produced a number” is not the same as “the problem is numerically safe.”

Watch for:

- badly scaled rows or columns;
- nearly dependent equations;
- large differences in coefficient magnitude;
- a residual that is small only because the whole problem is scaled very large or very small;
- iterative methods applied without checking their convergence behavior.

The V1 implementation deliberately provides partial pivoting, explicit pivot thresholds, residuals and failure diagnostics while remaining small enough to audit. Condition estimation, sparse storage and high-performance kernels belong to later milestones rather than being hidden behind an apparently simple V1 API.
