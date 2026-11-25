# Logarithmic least-squares approximation

## Purpose

`LeastSquares.FitLogarithmic` fits the two-parameter model

`y = a + b * ln(x)`

to observations with strictly positive x values. This closes the named logarithmic least-squares item in the historical V1 compatibility catalog while keeping the public API aligned with modern C# conventions.

This is an independently written implementation. The historical Borland toolbox is used only as a functional reference for V1 coverage; historical source code and handbook prose are not reused.

## Mathematical form

Introduce the transformed explanatory variable

`u = ln(x)`.

The model then becomes the ordinary straight line

`y = a + b*u`.

The implementation therefore validates and transforms x, then delegates the actual regression to the same shared straight-line path used by the other named transformed models. That path ultimately uses the general `LeastSquares.FitBasis` engine.

This separation is intentional: model-specific code describes the transformation and domain rules, while the numerical least-squares engine remains centralized and testable.

## Important difference from power and exponential fits

For the power model, both x and y are logarithmically transformed. For the exponential model, y is logarithmically transformed. In both cases the fitted objective is therefore expressed in transformed y coordinates.

The logarithmic model transforms **only x**. The y observations are never transformed. Consequently its ordinary least-squares objective is already the sum of squared residuals in the original y units.

`ResidualSumOfSquares` and `RootMeanSquareError` therefore describe the same residual domain that the logarithmic fit minimizes.

## Domain requirements

- every x value must be finite and strictly greater than zero because `ln(x)` must exist as a real number;
- y may be negative, zero or positive, but every y value must be finite;
- at least two observations are required;
- at least two distinct x values are required to identify the logarithmic coefficient.

## Result model

`LogarithmicFitResult` exposes:

- `Intercept` — fitted `a`;
- `LogCoefficient` — fitted `b` multiplying `ln(x)`;
- `SampleCount`;
- `ResidualSumOfSquares`;
- `RootMeanSquareError`;
- `Evaluate(x)` for finite positive x values.

The explicit `LogCoefficient` name avoids an ambiguous generic `Slope`: the slope is with respect to `ln(x)`, not directly with respect to x.

## Example

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0];
double[] y = [2.0, 3.1, 4.0, 5.2];

var fit = LeastSquares.FitLogarithmic(x, y);

Console.WriteLine(fit.Intercept);
Console.WriteLine(fit.LogCoefficient);
Console.WriteLine(fit.Evaluate(3.0));
Console.WriteLine(fit.RootMeanSquareError);
```

## Numerical notes

The shared V1 linear least-squares engine currently forms normal equations and solves them using Gaussian elimination with partial pivoting. This is deliberately readable and dependency-free. It is appropriate as the reference implementation for moderate, reasonably scaled problems, but QR or SVD will be preferable for difficult or ill-conditioned regression workloads later.

A logarithmic curve is a model choice, not merely a numerical trick. Residual plots and subject-matter knowledge should still be used to decide whether the relationship `a + b ln(x)` is plausible for the data.
