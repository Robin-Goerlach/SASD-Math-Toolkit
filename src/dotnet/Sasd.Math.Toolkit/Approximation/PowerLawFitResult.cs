using Sasd.Numerics.Common;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Result of fitting the model <c>y = a * x^b</c> to positive data.
/// </summary>
public sealed class PowerLawFitResult
{
    internal PowerLawFitResult(
        double scale,
        double exponent,
        int sampleCount,
        double residualSumOfSquares,
        double rootMeanSquareError)
    {
        NumericGuard.Positive(scale, nameof(scale));
        NumericGuard.Finite(exponent, nameof(exponent));
        NumericGuard.Positive(sampleCount, nameof(sampleCount));
        if (!double.IsFinite(residualSumOfSquares) || residualSumOfSquares < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(residualSumOfSquares));
        }

        if (!double.IsFinite(rootMeanSquareError) || rootMeanSquareError < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(rootMeanSquareError));
        }

        Scale = scale;
        Exponent = exponent;
        SampleCount = sampleCount;
        ResidualSumOfSquares = residualSumOfSquares;
        RootMeanSquareError = rootMeanSquareError;
    }

    /// <summary>Gets the fitted scale factor <c>a</c>.</summary>
    public double Scale { get; }

    /// <summary>Gets the fitted exponent <c>b</c>.</summary>
    public double Exponent { get; }

    /// <summary>Gets the number of observations used for the fit.</summary>
    public int SampleCount { get; }

    /// <summary>
    /// Gets the sum of squared residuals in the original y domain.
    /// </summary>
    public double ResidualSumOfSquares { get; }

    /// <summary>
    /// Gets the root-mean-square residual in the original y domain.
    /// </summary>
    public double RootMeanSquareError { get; }

    /// <summary>
    /// Evaluates the fitted power law at a strictly positive x value.
    /// </summary>
    public double Evaluate(double x)
    {
        if (!double.IsFinite(x) || x <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Power-law evaluation requires a finite x value greater than zero.");
        }

        var value = Scale * System.Math.Pow(x, Exponent);
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("Power-law evaluation produced a non-finite value.");
        }

        return value;
    }
}
