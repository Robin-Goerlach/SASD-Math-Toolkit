using Sasd.Numerics.Common;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Result of fitting the five-term Fourier model
/// <c>a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)</c>.
/// </summary>
public sealed class FiveTermFourierFitResult
{
    internal FiveTermFourierFitResult(
        double constantTerm,
        double fundamentalCosineCoefficient,
        double fundamentalSineCoefficient,
        double secondHarmonicCosineCoefficient,
        double secondHarmonicSineCoefficient,
        double fundamentalAngularFrequency,
        int sampleCount,
        double residualSumOfSquares,
        double rootMeanSquareError)
    {
        NumericGuard.Finite(constantTerm, nameof(constantTerm));
        NumericGuard.Finite(fundamentalCosineCoefficient, nameof(fundamentalCosineCoefficient));
        NumericGuard.Finite(fundamentalSineCoefficient, nameof(fundamentalSineCoefficient));
        NumericGuard.Finite(secondHarmonicCosineCoefficient, nameof(secondHarmonicCosineCoefficient));
        NumericGuard.Finite(secondHarmonicSineCoefficient, nameof(secondHarmonicSineCoefficient));
        NumericGuard.Positive(fundamentalAngularFrequency, nameof(fundamentalAngularFrequency));
        NumericGuard.Positive(sampleCount, nameof(sampleCount));

        if (!double.IsFinite(residualSumOfSquares) || residualSumOfSquares < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(residualSumOfSquares));
        }

        if (!double.IsFinite(rootMeanSquareError) || rootMeanSquareError < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(rootMeanSquareError));
        }

        var period = (2.0 * System.Math.PI) / fundamentalAngularFrequency;
        if (!double.IsFinite(period) || period <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fundamentalAngularFrequency),
                "The angular frequency must correspond to a finite positive period.");
        }

        ConstantTerm = constantTerm;
        FundamentalCosineCoefficient = fundamentalCosineCoefficient;
        FundamentalSineCoefficient = fundamentalSineCoefficient;
        SecondHarmonicCosineCoefficient = secondHarmonicCosineCoefficient;
        SecondHarmonicSineCoefficient = secondHarmonicSineCoefficient;
        FundamentalAngularFrequency = fundamentalAngularFrequency;
        Period = period;
        SampleCount = sampleCount;
        ResidualSumOfSquares = residualSumOfSquares;
        RootMeanSquareError = rootMeanSquareError;
    }

    /// <summary>Gets the constant coefficient <c>a0</c>.</summary>
    public double ConstantTerm { get; }

    /// <summary>Gets the coefficient <c>a1</c> multiplying <c>cos(w*x)</c>.</summary>
    public double FundamentalCosineCoefficient { get; }

    /// <summary>Gets the coefficient <c>b1</c> multiplying <c>sin(w*x)</c>.</summary>
    public double FundamentalSineCoefficient { get; }

    /// <summary>Gets the coefficient <c>a2</c> multiplying <c>cos(2*w*x)</c>.</summary>
    public double SecondHarmonicCosineCoefficient { get; }

    /// <summary>Gets the coefficient <c>b2</c> multiplying <c>sin(2*w*x)</c>.</summary>
    public double SecondHarmonicSineCoefficient { get; }

    /// <summary>Gets the fundamental angular frequency <c>w</c> in radians per x-unit.</summary>
    public double FundamentalAngularFrequency { get; }

    /// <summary>Gets the fundamental period <c>2*pi/w</c>.</summary>
    public double Period { get; }

    /// <summary>Gets the number of observations used for the fit.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the sum of squared residuals in the original y domain.</summary>
    public double ResidualSumOfSquares { get; }

    /// <summary>Gets the root-mean-square residual in the original y domain.</summary>
    public double RootMeanSquareError { get; }

    /// <summary>Evaluates the fitted Fourier model at a finite x value.</summary>
    public double Evaluate(double x) => EvaluateModel(
        ConstantTerm,
        FundamentalCosineCoefficient,
        FundamentalSineCoefficient,
        SecondHarmonicCosineCoefficient,
        SecondHarmonicSineCoefficient,
        FundamentalAngularFrequency,
        x);

    internal static double EvaluateModel(
        double constantTerm,
        double fundamentalCosineCoefficient,
        double fundamentalSineCoefficient,
        double secondHarmonicCosineCoefficient,
        double secondHarmonicSineCoefficient,
        double fundamentalAngularFrequency,
        double x)
    {
        NumericGuard.Finite(x, nameof(x));
        NumericGuard.Positive(fundamentalAngularFrequency, nameof(fundamentalAngularFrequency));

        var fundamentalPhase = fundamentalAngularFrequency * x;
        var secondHarmonicPhase = 2.0 * fundamentalPhase;
        if (!double.IsFinite(fundamentalPhase) || !double.IsFinite(secondHarmonicPhase))
        {
            throw new ArithmeticException("Fourier evaluation produced a non-finite phase.");
        }

        var value = constantTerm
            + (fundamentalCosineCoefficient * System.Math.Cos(fundamentalPhase))
            + (fundamentalSineCoefficient * System.Math.Sin(fundamentalPhase))
            + (secondHarmonicCosineCoefficient * System.Math.Cos(secondHarmonicPhase))
            + (secondHarmonicSineCoefficient * System.Math.Sin(secondHarmonicPhase));

        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("Fourier evaluation produced a non-finite value.");
        }

        return value;
    }
}
