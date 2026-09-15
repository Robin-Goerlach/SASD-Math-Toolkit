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

The solver takes logarithms and fits `ln(y) = ln(a) + b*ln(x)`. Every x and y sample must therefore be strictly positive.

## Exponential fitting

The exponential helper fits `y = a * exp(b*x)`. x may be any finite real value, but y must be positive because the implementation fits `ln(y) = ln(a) + b*x`.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(value => 2.5 * Math.Exp(-0.7 * value)).ToArray();

var fit = LeastSquares.FitExponential(x, y);

Console.WriteLine(fit.Scale); // approximately 2.5
Console.WriteLine(fit.Rate);  // approximately -0.7
Console.WriteLine(fit.Evaluate(1.5));
```

`Rate` is positive for growth, negative for decay and zero for a constant positive model.

## Logarithmic fitting

The logarithmic helper fits `y = a + b * ln(x)`. x must be positive, while y may be negative, zero or positive as long as it is finite.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0];
double[] y = [2.0, 3.1, 4.0, 5.2];

var fit = LeastSquares.FitLogarithmic(x, y);

Console.WriteLine(fit.Intercept);
Console.WriteLine(fit.LogCoefficient);
Console.WriteLine(fit.Evaluate(3.0));
```

`Intercept` is the fitted value at `x = 1`, because `ln(1) = 0`.

## Five-term Fourier fitting

For periodic data with a known fundamental period or angular frequency, use the five-term model

`a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`.

```csharp
using Sasd.Numerics.Approximation;

const double period = 4.0;
var omega = 2.0 * Math.PI / period;
var x = Enumerable.Range(0, 20).Select(i => i * 0.2).ToArray();
var y = x.Select(value =>
    1.5
    + 2.0 * Math.Cos(omega * value)
    - 0.5 * Math.Sin(omega * value)
    + 0.75 * Math.Cos(2.0 * omega * value)
    + 1.25 * Math.Sin(2.0 * omega * value)).ToArray();

var fit = LeastSquares.FitFiveTermFourierForPeriod(x, y, period);

Console.WriteLine(fit.ConstantTerm);
Console.WriteLine(fit.FundamentalCosineCoefficient);
Console.WriteLine(fit.SecondHarmonicSineCoefficient);
Console.WriteLine(fit.Evaluate(1.25));
```

The frequency is not estimated by this routine: you provide `w` directly with `FitFiveTermFourier`, or provide a period with `FitFiveTermFourierForPeriod`. At least five observations are required, and their phases must contain enough independent information to identify all five coefficients.

This fit is not an FFT. It estimates a small periodic model from samples, including non-uniformly spaced samples, while an FFT analyzes frequency bins of a regularly sampled sequence.

## Understanding residual diagnostics and transformations

Power-law and exponential fitting transform y before fitting and therefore minimize squared residuals in logarithmic y coordinates. Their reported RSS and RMSE are calculated afterwards in the original y domain.

Logarithmic and five-term Fourier fitting leave y unchanged. Their `ResidualSumOfSquares` is therefore directly the ordinary least-squares objective in the original y units.

## Arbitrary linear basis functions

If a model can be written as `c0*f0(x) + c1*f1(x) + ...`, `FitBasis` can fit it directly. This is the common numerical foundation for the polynomial and Fourier helpers and for several named transformed models.

## Practical checks

Always inspect or plot residuals instead of relying only on fitted parameters. For periodic models, confirm that the chosen fundamental period has scientific or engineering meaning; a wrong period can still produce numerical coefficients, but those coefficients may describe the data poorly.

The current reference implementation uses normal equations. That is adequate for the V1 compatibility layer and moderate well-scaled problems, but QR/SVD will be preferable for difficult regression workloads in a later numerical-backend milestone.

## V1 model progress

The historical V1 least-squares model set is now covered: general linear basis, polynomial, power, exponential, logarithmic and five-term Fourier fitting all have callable APIs. Future work in this area can therefore focus on numerical robustness and broader SASD statistical requirements rather than historical feature parity.
