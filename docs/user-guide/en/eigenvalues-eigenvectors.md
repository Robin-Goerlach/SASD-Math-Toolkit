# Eigenvalues and eigenvectors

Eigenvalue problems ask for a scalar `lambda` and a non-zero vector `v` such that

`A * v = lambda * v`.

They appear in stability analysis, vibration modes, repeated linear transformations, principal directions and many other numerical models. SASD Math Toolkit V1 contains several classical real dense-matrix methods. They solve different eigenvalue tasks; there is no single routine that should be used for every matrix.

## Choosing a method

Use **`PowerMethod`** when you need one eigenpair associated with the eigenvalue of largest magnitude and the spectrum has a reasonably clear dominant direction.

Use **`InversePowerMethod`** when you need the eigenvalue closest to zero and the matrix is nonsingular. The current V1 method is unshifted. It does not secretly choose a spectral shift for you.

Use **`WielandtSecondEigenpair`** mainly when you specifically want the classical historical workflow for the second-largest eigenvalue magnitude. It is useful as a reference algorithm, but repeated or nearly repeated dominant eigenvalues are a known weak point.

Use **`CyclicJacobi`** when the matrix is real and symmetric and you want the complete eigensystem. This is generally the most informative V1 API for a small dense symmetric problem because it returns all eigenvalues and an orthonormal eigenvector basis.

V1 does not contain a general nonsymmetric QR/Schur eigensolver or a general complex-eigenvalue API.

## A first power-method example

```csharp
using Sasd.Numerics.LinearAlgebra;

var matrix = new DenseMatrix(new double[,]
{
    { 4.0, 1.0, 0.0 },
    { 1.0, 3.0, 0.0 },
    { 0.0, 0.0, 1.0 }
});

var result = EigenSolvers.PowerMethod(
    matrix,
    tolerance: 1e-12,
    maximumIterations: 500);

if (result.Converged)
{
    var pair = result.Value;
    Console.WriteLine($"lambda = {pair.Eigenvalue}");
    Console.WriteLine($"residual = {result.Residual}");

    foreach (var component in pair.Components)
    {
        Console.WriteLine(component);
    }
}
else
{
    Console.WriteLine($"{result.Status}: {result.Message}");
}
```

The result is not just a number. `IterativeResult<Eigenpair>` tells you how the algorithm terminated:

- `Converged` means the method's convergence tests were satisfied;
- `Iterations` is the number of iterations used;
- `Residual` is the Euclidean eigen-equation defect used by the power/inverse-power routines;
- `MaximumIterationsReached` means the latest approximation is returned but the requested criterion was not reached;
- `NumericalBreakdown` describes a numerical state in which the iteration could not continue meaningfully.

Invalid method parameters are reported as argument exceptions instead of normal iteration statuses.

## Eigenvectors are not unique

If `v` is an eigenvector, then every non-zero scalar multiple of it is also an eigenvector. In particular, `v` and `-v` are the same eigendirection. SASD's iterative solvers normalize returned vectors, but your tests should normally not fail merely because another mathematically valid implementation chose the opposite sign.

For a repeated eigenvalue the situation is broader: individual eigenvectors inside the repeated eigenspace are not unique. A complete symmetric eigensolver may return a different orthonormal basis of that same subspace and still be correct.

## Checking an eigenpair independently

For a pair `(lambda, v)`, the quantity

`||A*v - lambda*v||2`

measures how well the pair satisfies the defining equation. SASD exposes this directly:

```csharp
var residual = EigenSolvers.EigenpairResidualNorm(matrix, result.Value);
```

This is an excellent regression and plausibility check. A small residual does **not** mean that the eigenvalue problem is well-conditioned. Closely clustered eigenvalues can be sensitive even while a returned pair has a small equation defect.

`Eigenpair` itself owns its vector. The public `Components` view is read-only, while `Eigenvector` returns a defensive array copy. Modifying that copy cannot mutate the numerical result.

## Power method in more detail

Power iteration repeatedly applies the matrix and normalizes the resulting vector. With suitable assumptions, components belonging to eigenvalues of smaller magnitude become relatively less important and the dominant eigendirection emerges.

A practical limitation follows immediately: the initial vector must have a non-zero component in that eigendirection. SASD uses a deterministic all-ones vector by default. If you know that this vector is orthogonal to the eigenvector you seek, provide another start vector:

```csharp
var result = EigenSolvers.PowerMethod(
    matrix,
    initialVector: new[] { 1.0, 0.2, -0.4 },
    tolerance: 1e-12);
```

A small spectral gap between the largest and second-largest magnitudes usually means slower convergence. If two dominant magnitudes are equal, plain power iteration may be a poor choice.

The implementation uses scaled vector normalization, so very large but finite start vectors do not fail merely because their components would overflow if squared directly.

## Inverse power method

Unshifted inverse iteration repeatedly solves

`A * y = x`

and normalizes `y`. Directions associated with small-magnitude eigenvalues are amplified by the inverse operation. SASD factorizes `A` once with partial-pivoted LU and reuses that factorization for all iterations.

```csharp
var smallest = EigenSolvers.InversePowerMethod(
    matrix,
    tolerance: 1e-12,
    maximumIterations: 200,
    pivotTolerance: 1e-14);
```

`pivotTolerance` belongs to the LU setup: pivots at or below that threshold are treated as numerically singular. It should not be confused with the eigenvalue convergence tolerance.

Because the V1 method is unshifted, a singular matrix cannot be processed by this API: the LU factorization fails before the iteration begins. A future shifted inverse iteration will deserve an explicit `shift` parameter rather than hidden behavior.

## Wielandt deflation and the second eigenpair

The classical Wielandt transformation removes one known eigenvalue from a matrix by a rank-one update. The direct API is useful when you already have an approximate eigenpair:

```csharp
var dominant = EigenSolvers.PowerMethod(matrix, tolerance: 1e-12);
if (!dominant.Converged)
{
    throw new InvalidOperationException("Dominant eigenpair did not converge.");
}

var deflation = WielandtDeflation.Create(
    matrix,
    dominant.Value,
    residualTolerance: 1e-8);

Console.WriteLine(deflation.SourceResidual);
```

The source residual is checked because deflating an arbitrary vector/eigenvalue claim would produce a matrix, but not a trustworthy mathematical deflation of the intended eigenvalue.

For the complete historical workflow, use:

```csharp
var second = EigenSolvers.WielandtSecondEigenpair(
    matrix,
    tolerance: 1e-11);
```

This first computes the dominant pair, deflates it, computes the dominant pair of the deflated matrix and restores the second eigenvector in the original coordinates.

Repeated or nearly repeated dominant eigenvalues can make restoration ill-conditioned. Check `second.Status` and `second.Message`; do not treat every returned value as converged.

## Complete symmetric eigensystem with cyclic Jacobi

For a real symmetric matrix, the spectral theorem gives real eigenvalues and an orthonormal eigenvector basis. Cyclic Jacobi exploits that structure by repeatedly applying rotations that reduce off-diagonal entries.

```csharp
var decompositionResult = EigenSolvers.CyclicJacobi(
    matrix,
    tolerance: 1e-12,
    maximumSweeps: 100,
    symmetryTolerance: 1e-12);

if (!decompositionResult.Converged)
{
    Console.WriteLine(decompositionResult.Message);
}

var decomposition = decompositionResult.Value;
for (var i = 0; i < decomposition.Size; i++)
{
    var lambda = decomposition.GetEigenvalue(i);
    var vector = decomposition.GetEigenvector(i);
    var pair = decomposition.GetEigenpair(i);
    var equationResidual = EigenSolvers.EigenpairResidualNorm(matrix, pair);

    Console.WriteLine($"{i}: lambda={lambda}, residual={equationResidual}");
}
```

Important conventions:

- eigenvalues are sorted in descending **numerical value**, not descending absolute magnitude;
- eigenvectors are columns of `decomposition.Eigenvectors`;
- `GetEigenvalue` is convenient when you only need one scalar without allocating an eigenvalue-array copy;
- `GetEigenvector` returns an independent copy;
- the decomposition's public matrix/array properties are also defensive copies.

### What does Jacobi's `Residual` mean?

For `CyclicJacobi`, `decompositionResult.Residual` is the off-diagonal Frobenius norm of the transformed working matrix. It tells you how close the Jacobi process is to diagonal form.

That is a different diagnostic from `||A*v-lambda*v||2`. If you want to validate the final individual eigenpairs, call `EigenpairResidualNorm` as shown above.

## Tolerances are algorithm criteria, not guaranteed digits

A tolerance such as `1e-12` is a stopping threshold inside the numerical method. It is not a promise of twelve correct decimal digits. Scaling, conditioning, floating-point roundoff, spectral gaps and the structure of the matrix all matter.

For difficult problems, useful checks include:

1. inspect `Status`, `Iterations` and the algorithm residual;
2. independently evaluate `EigenpairResidualNorm`;
3. for a symmetric complete decomposition, check eigenvector orthogonality when it matters to the application;
4. repeat with a stricter/looser tolerance or another valid start vector when sensitivity is suspected;
5. compare against an independent mature library for high-consequence or large-scale work.

## Scope and performance

These algorithms target small and medium dense reference problems, education, deterministic SASD tests and future cross-language conformance. They deliberately use understandable loops rather than blocked kernels, SIMD-specific implementations or native dependencies.

For large dense, sparse or high-performance eigenproblems, a future SASD backend should delegate to mature BLAS/LAPACK or specialized sparse libraries while preserving clear SASD-level contracts.
