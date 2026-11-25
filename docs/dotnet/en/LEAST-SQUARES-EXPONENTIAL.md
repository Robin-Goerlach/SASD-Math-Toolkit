# Exponential least-squares approximation

## Purpose

`LeastSquares.FitExponential` fits the two-parameter model

`y = a * exp(b*x)`

to observations with positive y values. Positive `b` describes exponential growth, negative `b` describes exponential decay, and `b = 0` gives a constant positive model.

This is an independently written C# implementation. The historical Borland toolbox is used only to identify the V1 functional target; historical source code and handbook prose are not reused.

## Mathematical transformation

Taking the natural logarithm of the model gives

`ln(y) = ln(a) + b*x`.

The transformed problem is therefore a straight-line least-squares fit. The implementation deliberately reuses the same internal straight-line adapter and the same public `LeastSquares.FitBasis` engine that support the power-law helper. It does not introduce a second regression solver.

If the transformed intercept is `c`, the original scale is reconstructed as

`a = exp(c)`.

## Domain requirements

Every x value must be finite, but x may be negative, zero or positive. Every y observation must be finite and strictly greater than zero because `ln(y)` is required.

At least two samples and at least two distinct x values are necessary. Repeating one x value cannot determine an exponential rate, no matter how many y observations are supplied at that same abscissa.

## Result model

`ExponentialFitResult` exposes:

- `Scale` — fitted `a`;
- `Rate` — fitted `b`;
- `SampleCount`;
- `ResidualSumOfSquares`;
- `RootMeanSquareError`;
- `Evaluate(x)` for any finite x value.

The result object is immutable and validates its numerical diagnostics when it is created.

## Error interpretation

The regression minimizes squared residuals in **log-y space** because the logarithmic transformation is what makes the model linear. The reported RSS and RMSE are then calculated in the original y domain so their units remain meaningful to application code.

These original-domain diagnostics are descriptive. They are not the objective that was minimized. If additive errors in the original y units are the actual statistical model, a nonlinear least-squares method may be more appropriate than this historical transformed fit.

## Example

```csharp
using Sasd.Numerics.Approximation;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(value => 2.5 * Math.Exp(-0.7 * value)).ToArray();

var fit = LeastSquares.FitExponential(x, y);

Console.WriteLine(fit.Scale);               // approximately 2.5
Console.WriteLine(fit.Rate);                // approximately -0.7
Console.WriteLine(fit.Evaluate(1.5));
Console.WriteLine(fit.RootMeanSquareError);
```

## Architecture and numerical notes

The named transformed models are kept in `LeastSquares.TransformedModels.cs`, while the core polynomial and arbitrary-basis engine remains in `LeastSquares.cs`. This keeps the public API under one `LeastSquares` type without turning the implementation into one oversized source file.

The current general engine uses normal equations and Gaussian elimination with partial pivoting. That is intentionally readable and dependency-free for V1. QR/SVD-based backends can later improve conditioning without changing the named exponential API.

The next named historical least-squares model is the logarithmic form, which can reuse the same straight-line adapter after transforming the x coordinate instead of y.
