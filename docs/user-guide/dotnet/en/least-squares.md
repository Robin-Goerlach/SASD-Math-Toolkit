# Least-squares approximation

Least-squares methods fit a model to observations that cannot usually be matched exactly. SASD Math Toolkit supports polynomial fitting and arbitrary models that are linear combinations of caller-supplied basis functions, plus the named curve models retained from the classical compatibility foundation.

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

Coefficients are returned in ascending power order: `c0`, `c1`, `c2`, and so on. The polynomial convenience API still requires at least `degree + 1` samples; it does not silently choose an underdetermined coefficient vector.

## Power-law fitting

A power law has the form `y = a * x^b`. `FitPowerLaw` applies the logarithmic transformation `ln(y) = ln(a) + b*ln(x)`, so both x and y must be strictly positive.

## Exponential fitting

`FitExponential` fits `y = a * exp(b*x)`. x may be any finite real value, but y must be positive because the implementation fits `ln(y) = ln(a) + b*x`.

## Logarithmic fitting

`FitLogarithmic` fits `y = a + b*ln(x)`. x must be positive while y may be any finite real value.

## Five-term Fourier fitting

For periodic data with a known fundamental period or angular frequency, the five-term model is

`a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`.

The frequency is supplied by the caller; this routine does not estimate it. This fit is not an FFT: it estimates a small periodic model and can use non-uniformly spaced samples.

The named Fourier helper requires a full-rank phase design because all five named coefficients are expected to be individually identifiable. Degenerate phase selections are therefore rejected even though the lower-level SVD could mathematically return one minimum-norm coefficient vector.

## Arbitrary linear basis functions

If a model can be written as `c0*f0(x) + c1*f1(x) + ...`, `FitBasis` fits it directly. Each basis function is evaluated exactly once per input sample and cached in the design matrix.

The general basis API now supports both the ordinary full-rank case and difficult rank structures:

- square/tall full-column-rank design -> Householder QR;
- rank-deficient or underdetermined design -> SVD minimum-norm solution.

This means `FitBasis` can be used as a lower-level regression building block even when coefficient vectors are not unique.

## Why QR remains the preferred normal case

The original compatibility implementation used normal equations:

`(A^T A)c = A^T y`.

Explicitly forming `A^T A` squares the condition number and can turn a moderately difficult regression problem into a much less reliable one.

For full-column-rank square/tall designs, the modern implementation factorizes the design matrix directly as `A = Q*R` using Householder reflections. It then solves the triangular problem after applying `Q^T` to the observations. QR is cheaper than a complete SVD and remains the preferred default when rank is not problematic.

`QrFactorization` is public in `Sasd.Numerics.LinearAlgebra` when an application needs the decomposition or wants to reuse it for several right-hand sides.

## When SVD takes over

When QR reports rank deficiency, or when the design matrix is wide/underdetermined, the general basis engine uses `SingularValueDecomposition`.

SVD supplies singular values and an explicit tolerance-dependent numerical rank. Components at or below the configured rank threshold are truncated, and the returned coefficient vector is the minimum-Euclidean-norm solution for the retained directions.

That distinction matters. A rank-deficient model does not have a unique coefficient vector even when all predictions are perfectly determined. SVD makes the choice explicit instead of pretending that an arbitrary coefficient set is unique.

For direct access:

```csharp
var svd = SingularValueDecomposition.Decompose(design);
Console.WriteLine(svd.EstimatedRank);
Console.WriteLine(svd.ConditionNumber);

var coefficients = svd.SolveLeastSquares(observations);
```

See the modern dense linear algebra chapter for pseudoinverse and condition-number details.

## Understanding residual diagnostics and transformations

Power-law and exponential fitting transform y before fitting and therefore minimize squared residuals in logarithmic y coordinates. Their reported RSS and RMSE are calculated afterwards in the original y domain.

Logarithmic and five-term Fourier fitting leave y unchanged. Their `ResidualSumOfSquares` is therefore directly the ordinary least-squares objective in the original y units.

## Practical checks

Always inspect or plot residuals instead of relying only on fitted parameters. Scaling still matters with QR and SVD, and a numerically successful fit is not proof that the chosen model is scientifically appropriate.

For rank-sensitive work, inspect singular values and rank rather than treating a returned coefficient vector as proof that every parameter is independently measurable. The historical model set remains available, while further development now targets regression diagnostics, weighting, robust methods and the wider requirements of SASD Statistical Workbench.
