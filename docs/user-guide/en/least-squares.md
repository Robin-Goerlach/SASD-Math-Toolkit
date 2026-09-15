# Least-squares approximation

Least-squares methods fit a model to more observations than can usually be matched exactly. SASD Math Toolkit supports polynomial fitting and arbitrary models that are linear combinations of caller-supplied basis functions. The V1 compatibility work also adds convenient named curve models one at a time.

## Polynomial fitting

For a polynomial

`y = c0 + c1*x + ... + cn*x^n`

use `FitPolynomial` and choose the degree explicitly.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [-2, -1, 0, 1, 2];
double[] y = [17, 6, 1, 2, 9];

var coefficients = LeastSquares.FitPolynomial(x, y, degree: 2);
var predicted = LeastSquares.EvaluatePolynomial(coefficients, 1.5);
```

Coefficients are returned in ascending power order: `c0`, `c1`, `c2`, and so on.

## Power-law fitting

A power law has the form

`y = a * x^b`.

Use `FitPowerLaw` when both variables are positive and a multiplicative scaling relationship is plausible.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0, 16.0];
var y = x.Select(value => 3.0 * Math.Pow(value, 2.5)).ToArray();

var fit = LeastSquares.FitPowerLaw(x, y);

Console.WriteLine(fit.Scale);                // approximately 3
Console.WriteLine(fit.Exponent);             // approximately 2.5
Console.WriteLine(fit.Evaluate(3.0));
Console.WriteLine(fit.RootMeanSquareError);
```

The solver takes logarithms and fits

`ln(y) = ln(a) + b*ln(x)`.

Therefore every x and y sample must be strictly greater than zero. This also means the least-squares objective is minimized in log space, not directly in the original y values.

## Exponential fitting

The exponential helper fits

`y = a * exp(b*x)`.

This form is useful for processes that are approximately exponential in x, including simple growth and decay curves. Here x may be any finite real value, but y must be positive because the implementation fits the transformed straight line

`ln(y) = ln(a) + b*x`.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(value => 2.5 * Math.Exp(-0.7 * value)).ToArray();

var fit = LeastSquares.FitExponential(x, y);

Console.WriteLine(fit.Scale); // approximately 2.5
Console.WriteLine(fit.Rate);  // approximately -0.7
Console.WriteLine(fit.Evaluate(1.5));
```

`Rate` is positive for growth, negative for decay and zero for a constant positive model. At least two distinct x values are required; otherwise the rate cannot be identified.

## Understanding residual diagnostics for transformed models

Both power-law and exponential fitting minimize squared residuals after a logarithmic transformation. `ResidualSumOfSquares` and `RootMeanSquareError` are nevertheless reported in the original y domain to make their units intuitive.

This distinction matters. A model that is optimal for logarithmic residuals is not necessarily the model that would minimize additive errors in the original units. If that error model matters to the application, use the transformed helpers as descriptive/reference fits rather than assuming they are a universal statistical optimum.

## Arbitrary linear basis functions

If a model can be written as

`c0*f0(x) + c1*f1(x) + ...`

then `FitBasis` can fit it directly. This is also the common numerical foundation for several named historical-style helpers.

## Practical checks

Always plot or inspect residuals instead of relying only on fitted parameters. Repeat the analysis with a simpler or more appropriate model when residuals show systematic structure. For transformed models, compare results against the scientific or engineering meaning of the assumed multiplicative error structure.

The current reference implementation uses normal equations. That is adequate for the V1 compatibility layer and moderate well-scaled problems, but QR/SVD will be preferable for difficult regression workloads in a later numerical-backend milestone.

## V1 model progress

The power-law and exponential helpers are now implemented. Dedicated logarithmic and five-term Fourier helpers remain to be added. Five-term polynomial behavior is already available through `FitPolynomial(..., degree: 4)`; a dedicated convenience name is optional rather than numerically necessary.
