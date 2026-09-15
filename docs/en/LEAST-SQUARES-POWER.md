# Power-law least-squares approximation

## Purpose

`LeastSquares.FitPowerLaw` fits the two-parameter model

`y = a * x^b`

to observed data. It is the first dedicated historical-style least-squares model helper added on top of the toolkit's existing polynomial and arbitrary linear-basis fitting foundation.

This is an independently written C# implementation. The historical Borland toolbox is used only to identify the V1 functional target; historical source code and handbook prose are not reused.

## Mathematical transformation

A power law is nonlinear in the exponent `b`, but it becomes linear after taking natural logarithms:

`ln(y) = ln(a) + b * ln(x)`.

The implementation therefore transforms all samples to `(ln(x), ln(y))` and then reuses `LeastSquares.FitBasis` with the basis functions `1` and `ln(x)`. If the fitted intercept is `c`, the final scale is reconstructed as

`a = exp(c)`.

This makes the implementation small, reviewable and consistent with the general least-squares engine rather than introducing a second independent regression solver.

## Domain requirements

The logarithmic transformation requires every x and y sample to be finite and strictly greater than zero. At least two samples and at least two distinct x values are required; otherwise the exponent cannot be determined.

These constraints are part of the mathematical model rather than arbitrary API restrictions. Data that include zero or negative values need another model or a different fitting method.

## Diagnostics

`PowerLawFitResult` exposes:

- `Scale` — fitted `a`;
- `Exponent` — fitted `b`;
- `SampleCount`;
- `ResidualSumOfSquares`;
- `RootMeanSquareError`;
- `Evaluate(x)` for positive x values.

The regression itself minimizes squared residuals in **log space** because that is where the model is linear. The reported residual sum of squares and RMSE are calculated afterwards in the original y domain so they are easier to interpret in the units of the input observations. These two facts should not be confused: the diagnostics describe the resulting curve, but they are not the objective minimized by the transformed regression.

## Example

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0];
double[] y = [3.0, 16.970562748, 96.0, 543.058007951];

var fit = LeastSquares.FitPowerLaw(x, y);

Console.WriteLine(fit.Scale);
Console.WriteLine(fit.Exponent);
Console.WriteLine(fit.Evaluate(3.0));
Console.WriteLine(fit.RootMeanSquareError);
```

## Numerical notes

The current general least-squares engine uses normal equations and Gaussian elimination with partial pivoting. This is intentionally a readable, dependency-free reference implementation, not the final answer for badly conditioned or very large regression problems. A future backend can add QR or SVD without changing the conceptual power-law API.

The logarithmic transformation also changes the statistical error model. If additive errors in the original y domain are important, a nonlinear least-squares method that minimizes original-domain residuals may be more appropriate. That is beyond this historical V1 helper.
