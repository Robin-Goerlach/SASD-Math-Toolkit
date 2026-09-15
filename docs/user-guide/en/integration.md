# Numerical integration

Numerical integration approximates a definite integral when an antiderivative is unavailable, inconvenient, or represented only by executable code. SASD Math Toolkit provides fixed-panel Newton-Cotes rules, Romberg extrapolation, five-point Gauss-Legendre quadrature, and adaptive variants.

```csharp
using Sasd.Numerics.Integration;
```

All current one-dimensional routines require finite bounds with `a < b`. If the mathematical integral is needed in the opposite direction, swap the endpoints and negate the result explicitly.

## Choosing a method

Use **composite trapezoid** when a simple baseline method is useful or the data model naturally suggests piecewise linear behavior. Use **composite Simpson** for smooth functions when an even fixed panel count is acceptable. Use **adaptive Simpson** when local difficulty varies strongly across the interval and you want refinement to concentrate where it is needed.

Use **Romberg** for smooth functions when repeated trapezoid refinement and Richardson extrapolation are effective. Use **five-point Gauss-Legendre** when one high-order panel is appropriate; the rule integrates polynomials through degree nine exactly in exact arithmetic. Use **adaptive five-point Gauss-Legendre** when the fixed rule is attractive but one panel is not sufficient over the whole interval.

## Composite trapezoid and Simpson rules

```csharp
var trapezoid = NumericalIntegration.CompositeTrapezoid(
    Math.Sin,
    0.0,
    Math.PI,
    intervals: 200);

var simpson = NumericalIntegration.CompositeSimpson(
    Math.Sin,
    0.0,
    Math.PI,
    intervals: 200);
```

The panel count is part of the numerical model. More panels usually reduce discretization error for a sufficiently smooth function, but they also increase callback evaluations and eventually expose floating-point limitations. Composite Simpson requires an even number of intervals.

For low-degree polynomials, the exactness properties are useful sanity checks: the trapezoid rule is exact for linear functions, while Simpson's rule is exact for cubic polynomials in exact arithmetic.

## Adaptive Simpson with diagnostics

The simple API returns only the best estimate:

```csharp
var integral = NumericalIntegration.AdaptiveSimpson(
    Math.Sin,
    0.0,
    Math.PI,
    tolerance: 1e-10,
    maximumDepth: 20);
```

For production code that needs to distinguish a successful tolerance test from a forced stop, prefer the detailed API:

```csharp
var result = NumericalIntegration.AdaptiveSimpsonDetailed(
    Math.Sin,
    0.0,
    Math.PI,
    tolerance: 1e-10,
    maximumDepth: 20);

if (!result.Converged)
{
    Console.WriteLine($"Stopped with {result.Status}");
}

Console.WriteLine(result.Value);
Console.WriteLine(result.EstimatedError);
Console.WriteLine(result.FunctionEvaluations);
```

`AdaptiveIntegrationStatus.Converged` means every accepted panel met its local criterion. `MaximumDepthReached` means at least one region still wanted refinement when the configured depth was exhausted. `NumericalResolutionReached` means the interval could no longer be bisected into distinct `double` values. In either non-converged case, `Value` remains the best estimate available to the algorithm, but it should not be presented as if the requested tolerance had been guaranteed.

The Simpson error indicator is based on the difference between one Simpson panel and its two half-panels, with the usual Richardson correction. It is an estimate, not a proof of the true mathematical error.

## Romberg integration

Romberg integration builds a triangular extrapolation table from successively halved trapezoid rules:

```csharp
var result = NumericalIntegration.RombergDetailed(
    x => Math.Exp(-x * x),
    0.0,
    1.0,
    tolerance: 1e-10,
    maximumLevels: 12);
```

`result.Value` is the best diagonal extrapolation. `EstimatedError` is the absolute difference between the last two diagonal estimates. `Status` is `IterationStatus.Converged` when that difference meets the requested tolerance; otherwise the last available estimate is returned with `MaximumIterationsReached`.

The work approximately doubles with every additional trapezoid level. The reference implementation therefore limits `maximumLevels` to 30 to keep the integer subdivision model well-defined. Values anywhere near that upper bound can still be computationally expensive; normal applications should use far fewer levels.

## Five-point Gauss-Legendre quadrature

A fixed five-node rule is available directly:

```csharp
var integral = NumericalIntegration.GaussLegendre5(
    x => Math.Exp(x),
    0.0,
    1.0);
```

The nodes and weights are mapped from the standard interval `[-1, 1]` onto `[a, b]`. For smooth functions, a Gaussian rule can obtain high accuracy with relatively few function evaluations.

The adaptive variant compares one five-node rule over a panel with the sum of two five-node rules over its halves:

```csharp
var result = NumericalIntegration.AdaptiveGaussLegendre5Detailed(
    Math.Exp,
    0.0,
    1.0,
    tolerance: 1e-12,
    maximumDepth: 16);
```

Its `EstimatedError` is the accumulated refinement difference used by this algorithm. As with adaptive Simpson, it is an error indicator rather than a formal bound.

## Non-finite values and singular integrands

All integration routines reject `NaN` and infinity returned by the integrand. This is intentional: silently propagating a non-finite sample through a long quadrature can produce a result that is difficult to diagnose.

An improper integral or a function with an endpoint singularity therefore needs an explicit transformation or interval treatment by the application. The current V1 routines are finite-interval quadrature methods; they do not automatically interpret infinite limits or principal values.

## Accuracy is not just a tolerance number

A requested tolerance only controls the algorithm's own refinement criterion. It cannot compensate for a badly scaled integrand, discontinuities, unresolved narrow peaks, cancellation, or loss of precision inside the user callback. When a result matters, vary the method or numerical controls and check whether the answer is stable.

The detailed adaptive and Romberg result types exist specifically so applications can record how an answer was obtained instead of storing only a bare `double`.
