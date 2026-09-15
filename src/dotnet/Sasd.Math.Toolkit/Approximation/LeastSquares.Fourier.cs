using Sasd.Numerics.Common;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Fourier-series least-squares helpers built on the shared linear-basis engine.
/// </summary>
public static partial class LeastSquares
{
    /// <summary>
    /// Fits the five-term Fourier model
    /// <c>a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)</c>.
    /// </summary>
    /// <param name="x">Sample abscissas.</param>
    /// <param name="y">Observed values.</param>
    /// <param name="fundamentalAngularFrequency">
    /// Fundamental angular frequency <c>w</c> in radians per x-unit. The default value 1.0
    /// reproduces the classical basis <c>1, cos(x), sin(x), cos(2x), sin(2x)</c>.
    /// </param>
    /// <remarks>
    /// The angular frequency is treated as known. For a fixed frequency the model is linear in
    /// its five coefficients, so coefficient estimation uses the shared basis-design machinery.
    /// Unlike the fully general <see cref="FitBasis"/> API, this named model deliberately requires
    /// full column rank because five named Fourier coefficients should be individually identifiable.
    /// Degenerate phase selections therefore remain an error instead of silently returning one of
    /// infinitely many minimum-norm coefficient vectors.
    /// </remarks>
    public static FiveTermFourierFitResult FitFiveTermFourier(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double fundamentalAngularFrequency = 1.0)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        NumericGuard.Positive(fundamentalAngularFrequency, nameof(fundamentalAngularFrequency));

        if (x.Count < 5)
        {
            throw new ArgumentException(
                "At least five data points are required to determine the five Fourier coefficients.",
                nameof(x));
        }

        var secondHarmonicAngularFrequency = 2.0 * fundamentalAngularFrequency;
        var period = (2.0 * System.Math.PI) / fundamentalAngularFrequency;
        if (!double.IsFinite(secondHarmonicAngularFrequency) || !double.IsFinite(period))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fundamentalAngularFrequency),
                "The angular frequency must allow a finite second harmonic and a finite period.");
        }

        // Validate phase products before evaluating trigonometric basis functions. Math.Sin/Math.Cos
        // return NaN for infinite arguments; reporting the problematic sample directly is clearer
        // than allowing that NaN to surface later as a generic basis-function failure.
        for (var i = 0; i < x.Count; i++)
        {
            if (!double.IsFinite(x[i]) || !double.IsFinite(y[i]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    $"Fourier sample {i} contains a non-finite x or y value.");
            }

            var fundamentalPhase = fundamentalAngularFrequency * x[i];
            var secondHarmonicPhase = secondHarmonicAngularFrequency * x[i];
            if (!double.IsFinite(fundamentalPhase) || !double.IsFinite(secondHarmonicPhase))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(x),
                    $"Fourier sample {i} produces a non-finite phase for the requested angular frequency.");
            }
        }

        var coefficients = FitBasisRequiringFullColumnRank(
            x,
            y,
            [
                static _ => 1.0,
                value => System.Math.Cos(fundamentalAngularFrequency * value),
                value => System.Math.Sin(fundamentalAngularFrequency * value),
                value => System.Math.Cos(secondHarmonicAngularFrequency * value),
                value => System.Math.Sin(secondHarmonicAngularFrequency * value)
            ]);

        var (residualSumOfSquares, rootMeanSquareError) = ComputeOriginalDomainDiagnostics(
            x,
            y,
            value => FiveTermFourierFitResult.EvaluateModel(
                coefficients[0],
                coefficients[1],
                coefficients[2],
                coefficients[3],
                coefficients[4],
                fundamentalAngularFrequency,
                value),
            "five-term Fourier model");

        return new FiveTermFourierFitResult(
            coefficients[0],
            coefficients[1],
            coefficients[2],
            coefficients[3],
            coefficients[4],
            fundamentalAngularFrequency,
            x.Count,
            residualSumOfSquares,
            rootMeanSquareError);
    }

    /// <summary>
    /// Fits the five-term Fourier model when the fundamental period is easier to specify than
    /// angular frequency.
    /// </summary>
    /// <remarks>
    /// This is a convenience wrapper around <see cref="FitFiveTermFourier"/> using
    /// <c>w = 2*pi/period</c>. It keeps period conversion in one tested place and avoids callers
    /// repeatedly reimplementing the same conversion.
    /// </remarks>
    public static FiveTermFourierFitResult FitFiveTermFourierForPeriod(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double period)
    {
        NumericGuard.Positive(period, nameof(period));

        var angularFrequency = (2.0 * System.Math.PI) / period;
        if (!double.IsFinite(angularFrequency) || angularFrequency <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(period),
                "The period must produce a finite positive angular frequency.");
        }

        return FitFiveTermFourier(x, y, angularFrequency);
    }
}
