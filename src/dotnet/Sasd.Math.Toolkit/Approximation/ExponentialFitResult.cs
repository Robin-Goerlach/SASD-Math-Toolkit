using Sasd.Numerics.Common;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Result of fitting the model <c>y = a * exp(b*x)</c> to positive y observations.
/// </summary>
public sealed class ExponentialFitResult
{
    internal ExponentialFitResult(
        double scale,
        double rate,
        int sampleCount,
        double residualSumOfSquares,
        double rootMeanSquareError)
    {
        NumericGuard.Positive(scale, nameof(scale));
        NumericGuard.Finite(rate, nameof(rate));
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
        Rate = rate;
        SampleCount = sampleCount;
        ResidualSumOfSquares = residualSumOfSquares;
        RootMeanSquareError = rootMeanSquareError;
    }

    /// <summary>Gets the fitted positive scale factor <c>a</c>.</summary>
    public double Scale { get; }

    /// <summary>
    /// Gets the fitted exponential rate <c>b</c>. Positive values describe growth,
    /// negative values describe decay, and zero describes a constant positive model.
    /// </summary>
    public double Rate { get; }

    /// <summary>Gets the number of observations used for the fit.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the sum of squared residuals in the original y domain.</summary>
    public double ResidualSumOfSquares { get; }

    /// <summary>Gets the root-mean-square residual in the original y domain.</summary>
    public double RootMeanSquareError { get; }

    /// <summary>
    /// Evaluates the fitted exponential curve at any finite x value.
    /// </summary>
    public double Evaluate(double x)
    {
        NumericGuard.Finite(x, nameof(x));

        var value = Scale * System.Math.Exp(Rate * x);
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("Exponential evaluation produced a non-finite value.");
        }

        return value;
    }
}
