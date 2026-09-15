using Sasd.Numerics.Common;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Result of fitting the model <c>y = a + b*ln(x)</c> to positive x observations.
/// </summary>
public sealed class LogarithmicFitResult
{
    internal LogarithmicFitResult(
        double intercept,
        double logCoefficient,
        int sampleCount,
        double residualSumOfSquares,
        double rootMeanSquareError)
    {
        NumericGuard.Finite(intercept, nameof(intercept));
        NumericGuard.Finite(logCoefficient, nameof(logCoefficient));
        NumericGuard.Positive(sampleCount, nameof(sampleCount));

        if (!double.IsFinite(residualSumOfSquares) || residualSumOfSquares < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(residualSumOfSquares));
        }

        if (!double.IsFinite(rootMeanSquareError) || rootMeanSquareError < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(rootMeanSquareError));
        }

        Intercept = intercept;
        LogCoefficient = logCoefficient;
        SampleCount = sampleCount;
        ResidualSumOfSquares = residualSumOfSquares;
        RootMeanSquareError = rootMeanSquareError;
    }

    /// <summary>Gets the fitted intercept <c>a</c>.</summary>
    public double Intercept { get; }

    /// <summary>Gets the fitted coefficient <c>b</c> multiplying <c>ln(x)</c>.</summary>
    public double LogCoefficient { get; }

    /// <summary>Gets the number of observations used for the fit.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the sum of squared residuals in the original y domain.</summary>
    public double ResidualSumOfSquares { get; }

    /// <summary>Gets the root-mean-square residual in the original y domain.</summary>
    public double RootMeanSquareError { get; }

    /// <summary>
    /// Evaluates the fitted logarithmic curve at a finite x value greater than zero.
    /// </summary>
    public double Evaluate(double x)
    {
        if (!double.IsFinite(x) || x <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                "Logarithmic evaluation requires a finite x value greater than zero.");
        }

        var value = Intercept + (LogCoefficient * System.Math.Log(x));
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("Logarithmic evaluation produced a non-finite value.");
        }

        return value;
    }
}
