# Least-squares approximation

Least-squares methods fit a model to more observations than can usually be matched exactly. SASD Math Toolkit supports polynomial fitting and arbitrary models that are linear combinations of caller-supplied basis functions, plus the named curve models retained from the classical compatibility foundation.

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

A power law has the form `y = a * x^b`. `FitPowerLaw` applies the logarithmic transformation `ln(y) = ln(a) + b*ln(x)`, so both x and y must be strictly positive.

## Exponential fitting

`FitExponential` fits `y = a * exp(b*x)`. x may be any finite real value, but y must be positive because the implementation fits `ln(y) = ln(a) + b*x`.

## Logarithmic fitting

`FitLogarithmic` fits `y = a + b*ln(x)`. x must be positive while y may be any finite real value.

## Five-term Fourier fitting

For periodic data with a known fundamental period or angular frequency, the five-term model is

`a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`.

The frequency is supplied by the caller; this routine does not estimate it. This fit is not an FFT: it estimates a small periodic model and can use non-uniformly spaced samples.

## Arbitrary linear basis functions

If a model can be written as `c0*f0(x) + c1*f1(x) + ...`, `FitBasis` fits it directly. Each basis function is evaluated exactly once per input sample and the resulting design matrix is solved with Householder QR.

## Why QR is now the default

The original compatibility implementation used normal equations. They solve

`(A^T A)c = A^T y`

but explicitly forming `A^T A` squares the condition number and can turn a moderately difficult regression problem into a much less reliable one.

The modern implementation factorizes the design matrix directly as `A = Q*R` using Householder reflections. It then solves the triangular problem after applying `Q^T` to the observations. This is a substantially better default for general dense least squares while remaining dependency-free and understandable.

`QrFactorization` is also public in `Sasd.Numerics.LinearAlgebra` when an application needs the decomposition or wants to reuse it for several right-hand sides.

Rank-deficient and underdetermined regression is intentionally not guessed at. Those cases currently report failure and are reserved for the planned SVD layer, which can provide singular values, robust rank diagnostics and pseudoinverse solutions.

## Understanding residual diagnostics and transformations

Power-law and exponential fitting transform y before fitting and therefore minimize squared residuals in logarithmic y coordinates. Their reported RSS and RMSE are calculated afterwards in the original y domain.

Logarithmic and five-term Fourier fitting leave y unchanged. Their `ResidualSumOfSquares` is therefore directly the ordinary least-squares objective in the original y units.

## Practical checks

Always inspect or plot residuals instead of relying only on fitted parameters. Scaling still matters even with QR, and a numerically successful fit is not proof that the chosen model is scientifically appropriate.

The historical model set remains available, but future work in this area now focuses on SVD, rank/conditioning diagnostics and the broader statistical requirements of SASD Statistical Workbench rather than historical feature parity.
