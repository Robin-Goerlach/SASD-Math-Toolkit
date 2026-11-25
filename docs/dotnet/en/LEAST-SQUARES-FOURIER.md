# Five-term Fourier least-squares approximation

## Purpose

`LeastSquares.FitFiveTermFourier` fits the five-coefficient periodic model

`y = a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`.

This closes the historical five-term Fourier least-squares item in the V1 compatibility catalog. The implementation is independently written C# code; the historical Borland toolbox is used only to identify the functional target, not as a source-code or handbook-text source.

## What “five-term” means in this API

The five linear basis functions are:

1. `1`
2. `cos(w*x)`
3. `sin(w*x)`
4. `cos(2*w*x)`
5. `sin(2*w*x)`

The angular frequency `w` is known before fitting. The routine estimates only the five amplitudes. This is important: estimating an unknown frequency would be a nonlinear optimization problem and is deliberately outside this V1 helper.

The default `w = 1` provides the classical radian basis `1, cos(x), sin(x), cos(2x), sin(2x)`. If the natural description is a period `T`, use `FitFiveTermFourierForPeriod`, which performs `w = 2*pi/T` and then calls the same fitting path.

## Architecture

For fixed `w`, the model is linear in all five unknown coefficients. The implementation therefore creates the five Fourier basis functions and delegates directly to `LeastSquares.FitBasis`.

This is intentional. There is no second Fourier-specific least-squares solver to maintain: validation and model construction live in the Fourier helper, while coefficient estimation remains in the shared linear least-squares engine. Residual diagnostics reuse the same original-domain diagnostic path as the other named fit results.

The current V1 `FitBasis` implementation uses normal equations with Gaussian elimination and partial pivoting. This keeps the reference implementation small and inspectable. QR or SVD is a later robustness/performance improvement for difficult least-squares problems.

## Result model

`FiveTermFourierFitResult` exposes:

- `ConstantTerm` (`a0`)
- `FundamentalCosineCoefficient` (`a1`)
- `FundamentalSineCoefficient` (`b1`)
- `SecondHarmonicCosineCoefficient` (`a2`)
- `SecondHarmonicSineCoefficient` (`b2`)
- `FundamentalAngularFrequency`
- `Period`
- `SampleCount`
- `ResidualSumOfSquares`
- `RootMeanSquareError`
- `Evaluate(x)`

The result stores the exact frequency used for the fit, so later evaluation cannot accidentally use a different period.

## Example

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
Console.WriteLine(fit.Period);
Console.WriteLine(fit.Evaluate(1.25));
```

## Validation and rank

At least five observations are required because there are five unknown coefficients. Five samples are not automatically sufficient, however. The sample phases must make the five basis columns linearly independent. For example, repeatedly sampling the same phase cannot identify the harmonic coefficients and the shared solver reports a singular or numerically singular system.

All sample values and the angular frequency must be finite. The frequency must be positive and must permit a finite second harmonic and period. Extremely large combinations of x and frequency that overflow the phase calculation are rejected explicitly.

## Relationship to FFT

This helper is **not** an FFT. Least-squares Fourier fitting works with arbitrary sample locations and a caller-selected fundamental frequency, then estimates model coefficients. An FFT transforms a regularly sampled sequence into discrete frequency bins. Both use sinusoidal mathematics, but they answer different questions and therefore remain separate APIs.
