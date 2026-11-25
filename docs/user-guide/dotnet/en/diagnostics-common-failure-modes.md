# Numerical recipes, diagnostics and common failure modes

A numerical routine rarely returns only a number. A trustworthy application also needs to know **how the algorithm stopped**, **how well the returned value satisfies the mathematical problem**, **which tolerance was used**, and **whether the problem itself may be ill-conditioned**.

This chapter brings the diagnostic conventions of the SASD Math Toolkit together in one place. It does not replace the domain chapters. Instead, it provides a common way to read results from root finding, integration, linear algebra, eigenvalue algorithms, ODE solvers and approximation routines.

## 1. Four questions before accepting a numerical result

Before treating a returned value as usable, ask four separate questions:

1. **Was the call valid?** Invalid dimensions, non-finite input, impossible options or an invalid interval are input-contract problems and normally raise a .NET exception.
2. **How did the algorithm terminate?** Iterative and adaptive routines expose a status such as `Converged`, `MaximumIterationsReached`, `NumericalBreakdown`, `NotBracketed`, `MaximumDepthReached` or `MinimumStepSizeReached`.
3. **How large is the defect?** A residual or error estimate says something about the returned approximation, but its meaning is specific to the problem.
4. **Is the problem well-conditioned enough for the requested interpretation?** A tiny residual does not automatically imply a tiny error in the unknown.

These questions are deliberately separate in the API. A status is not a residual, a residual is not a forward error, and a tolerance is not a guarantee of significant digits.

## 2. Exceptions and result statuses mean different things

The C#/.NET implementation follows a simple rule:

- **Invalid API usage or invalid numerical input** is rejected with normal .NET exceptions such as `ArgumentException` or `ArgumentOutOfRangeException`.
- **Expected numerical outcomes after a valid call** are normally reported through result status objects.
- **Direct arithmetic that cannot produce a meaningful finite result** may raise `ArithmeticException` when there is no useful iterative status to return.

For example, a bisection interval whose endpoints do not bracket a sign change is a mathematically meaningful outcome. `RootResult.Status` therefore reports `IterationStatus.NotBracketed`. By contrast, passing `double.NaN` as an input is an invalid call and is rejected before iteration starts.

This separation matters in application code. Do not use exceptions as the normal branch for “the method did not converge”, and do not ignore exceptions as if they were merely another convergence status.

## 3. `Converged` is a termination statement, not a proof of exactness

The generic `IterativeResult<T>` contains a `Status`, an `Iterations` count, a `Value`, an optional `Residual` and an optional diagnostic `Message`.

```csharp
var result = LinearSystemSolvers.GaussSeidel(
    matrix,
    rightHandSide,
    tolerance: 1e-10,
    maximumIterations: 500);

if (!result.Converged)
{
    Console.WriteLine($"Stopped with {result.Status}: {result.Message}");
}

if (result.HasFiniteResidual)
{
    Console.WriteLine($"Residual = {result.Residual:E3}");
}
```

`Converged` means that the algorithm satisfied **its documented criterion**. It does not mean that the returned mathematical quantity is exact. A converged result can still be affected by conditioning, floating-point rounding, model error, discretization error or an inappropriate problem scale.

The reverse is also important: a non-converged result may still contain a useful best-so-far approximation and a finite residual. The new `HasFiniteResidual` property is intentionally independent of `Converged`.

## 4. Residual, forward error and estimated error are not interchangeable

### Root finding

For a scalar root estimate `x`, `RootResult.Residual` is

`|f(x)|`.

A small value shows that the returned `x` nearly satisfies the equation `f(x)=0`. It does **not** directly measure `|x-x*|`, where `x*` is the exact root. Near a multiple root or where `f'` is very small, a small function residual can coexist with a much larger error in `x`.

```csharp
var root = RootSolvers.NewtonRaphson(
    x => Math.Cos(x) - x,
    x => -Math.Sin(x) - 1.0,
    0.5);

Console.WriteLine(root.Status);
Console.WriteLine(root.Root);
Console.WriteLine(root.Residual);
```

### Linear systems

For `A*x=b`, `LinearSystemSolvers.ResidualInfinityNorm` computes

`||A*x-b||∞`.

That is an equation defect, not a direct bound on `||x-x*||`. If `A` is ill-conditioned, a small residual may still correspond to a sensitive solution.

```csharp
var x = LinearSystemSolvers.SolveGaussian(matrix, rightHandSide);
var residual = LinearSystemSolvers.ResidualInfinityNorm(matrix, x, rightHandSide);
```

For direct solvers, computing this residual after the solve is a useful independent application-level check because the direct method itself does not return an iterative result envelope.

### Eigenpairs

For `A*v=lambda*v`, `EigenSolvers.EigenpairResidualNorm` computes

`||A*v-lambda*v||2`.

```csharp
var result = EigenSolvers.PowerMethod(matrix);
var residual = EigenSolvers.EigenpairResidualNorm(matrix, result.Value);
```

A small eigenpair residual verifies the eigen-equation well in an absolute sense. It does not say that the eigenvalue is insensitive to perturbations, and repeated or tightly clustered eigenvalues can make individual eigenvectors intrinsically sensitive.

### Adaptive integration

`AdaptiveIntegrationResult.EstimatedError` and `RombergIntegrationResult.EstimatedError` are **algorithmic error indicators** derived from refinement or extrapolation. They are not comparisons with an exact integral that the library somehow knows.

The estimate is useful for controlling refinement and for diagnostics. When an independent analytic result or a much higher-accuracy reference is available, compare against that as a separate validation step.

### Adaptive ODE integration

Runge-Kutta-Fehlberg controls a **local step error estimate**. Successful local error control does not turn the accumulated trajectory into an exact global solution. `AdaptiveOdeResult` therefore reports whether the end point was reached and how many trial steps were accepted or rejected; application validation may still compare selected points with invariants, analytical solutions or a finer independent run.

## 5. Tolerance must be interpreted in the scale of the problem

A tolerance such as `1e-10` has no universal meaning by itself.

Suppose one problem naturally contains values around `1e-6` while another contains values around `1e12`. An absolute threshold of `1e-10` is extremely strict for one scale and potentially meaningless for another. Some toolkit algorithms use absolute tolerances because that is the clearest historical/reference contract; adaptive RKF has explicit absolute and relative tolerance controls where both are important.

The practical rule is:

- read the method documentation to learn **what quantity is compared with the tolerance**;
- choose tolerances from the physical or mathematical scale of the problem;
- do not interpret `NumericConstants.DefaultTolerance` as a promise of a fixed number of correct decimal places;
- do not use `NumericConstants.NearlyZero` as a universal floating-point equality test.

`NearlyZero` is an internal/reference guard threshold for pivots, divisors and norms that are numerically negligible at the scale assumed by a particular routine. Public APIs such as LU factorization and inverse iteration expose a pivot tolerance when callers need control over that decision.

## 6. Common termination statuses and what to do next

### `MaximumIterationsReached`

The algorithm remained numerically meaningful but did not satisfy its criterion within the configured limit.

Useful next questions are: Is the tolerance realistic? Is the initial guess poor? Is convergence intrinsically slow? Does the method fit the problem? Is the residual still decreasing?

Increasing the iteration limit can be reasonable only after answering those questions. A larger limit is not a substitute for a suitable algorithm.

### `NumericalBreakdown`

A required numerical invariant failed: a divisor became negligible, an iterate became non-finite, overflow occurred, or continuation would otherwise be meaningless.

Do not automatically retry with a much looser tolerance. Inspect the diagnostic message and the problem scaling first. A breakdown often points to a structural issue rather than simple lack of patience.

### `NotBracketed`

A bracketing root method was given an interval without the required sign change. Choose a better interval or use problem knowledge to find a bracket. Do not reinterpret this as “the function has no root anywhere”.

### `MaximumDepthReached` / `NumericalResolutionReached`

Adaptive quadrature could not continue normal refinement. The returned estimate is the best value available at the limit, but `Converged` is false. Singularities, discontinuities, extreme oscillation or an unrealistic tolerance may be responsible.

### `MinimumStepSizeReached`

Adaptive ODE integration would need a step smaller than the configured minimum to satisfy the requested local error tolerance. Review the tolerance, minimum step and the behavior of the differential equation. This can also indicate stiffness, which RKF45 is not designed to solve efficiently.

### Singular or numerically singular matrix

Direct linear algebra routines may throw `ArithmeticException` when pivots fall below the configured threshold. Distinguish three cases: a genuinely singular matrix, a severely ill-conditioned matrix, and a matrix whose scaling makes the chosen absolute pivot tolerance unsuitable.

## 7. Non-finite values are treated as failures, not ordinary data

The V1 numerical algorithms expect finite floating-point inputs unless an API explicitly says otherwise. `NaN` and infinity are rejected at public boundaries where practical. If a callback supplied by the application returns a non-finite value during an algorithm, the routine either reports a numerical breakdown or throws an arithmetic exception according to its contract.

This policy is intentional. Letting `NaN` silently propagate through a hundred iterations can produce an apparently completed workflow with no useful diagnosis.

Applications should apply the same rule to imported data: validate before numerical processing, and preserve enough context to identify the original row, sample, observation or parameter that was invalid.

## 8. Conditioning is different from algorithmic convergence

A stable algorithm cannot make an ill-conditioned problem well-conditioned.

Examples:

- nearly singular linear systems can have large changes in `x` for small changes in `A` or `b`;
- multiple or clustered polynomial roots can be very sensitive to coefficient perturbations;
- repeated or clustered eigenvalues can make individual eigenvectors unstable;
- numerical differentiation amplifies noise;
- high-degree polynomial interpolation can be a poor model even when the interpolation equations are solved exactly.

Therefore, “the solver converged” is only one part of the quality story. For important results, also inspect problem structure and sensitivity.

## 9. Independent verification recipes

For important calculations, prefer a second check that is not merely the same stopping test repeated.

### Root

Evaluate the original function at the reported root and record `RootResult.Residual`. If a derivative is available, inspect whether the root lies in a region where the function is nearly flat.

### Direct linear solve

Recompute `||A*x-b||∞` with `ResidualInfinityNorm`. For highly sensitive applications, compare with a scaled formulation or an independent high-quality linear algebra package.

### Eigenpair

Use `EigenpairResidualNorm` on the original matrix. For a complete symmetric decomposition, also check orthogonality and, when appropriate, reconstruction against `A`.

### Integration

Repeat with a stricter tolerance or a different method when the integrand is difficult. Agreement between two genuinely different methods is stronger evidence than merely increasing one recursion limit.

### ODE

Compare against known invariants, an analytical solution, a finer step or an independent solver. For long integrations, inspect error growth over the trajectory rather than only the last point.

### Least squares

Inspect residuals in the **original data domain**, not only transformed coordinates. For named transformed models, the SASD result types report diagnostics in the original `y` domain so application interpretation remains meaningful.

## 10. A practical acceptance pattern

For production-facing code, a useful pattern is:

```csharp
var result = EigenSolvers.PowerMethod(
    matrix,
    tolerance: 1e-10,
    maximumIterations: 500);

if (!result.Converged)
{
    throw new InvalidOperationException(
        $"Eigenvalue calculation stopped with {result.Status}: {result.Message}");
}

var residual = EigenSolvers.EigenpairResidualNorm(matrix, result.Value);
if (residual > 1e-8)
{
    throw new InvalidOperationException(
        $"Eigenpair residual {residual:E3} exceeds the application acceptance limit.");
}

var eigenpair = result.Value;
```

Notice that the **algorithm tolerance** and the **application acceptance limit** are separate concepts. The library controls its numerical process; the application decides whether the result is good enough for its business, scientific or engineering purpose.

## 11. What should be logged

When a numerical result matters enough to reproduce later, store more than the final value. Useful metadata includes:

- algorithm name and relevant options;
- input scale or dataset identity;
- termination status;
- iteration/refinement/step counts;
- residual or estimated error when available;
- diagnostic message;
- toolkit version/commit for research or validation workflows.

Do not store only “success=true”. That discards the information most useful when two runs later disagree.

## 12. Compact checklist

Before accepting a result, verify:

- the input was finite and satisfied the API contract;
- the termination status is understood;
- the residual/error indicator has the expected definition and scale;
- the chosen tolerance is appropriate for the problem;
- iteration/depth/step limits did not stop the algorithm prematurely;
- no diagnostic message indicates breakdown;
- conditioning or data noise does not invalidate the interpretation;
- an independent check is used when the result is important.

This diagnostic discipline is intentionally part of the SASD Math Toolkit architecture. V1 favors explicit, inspectable numerical behavior over APIs that return a bare number and hide how it was obtained.
