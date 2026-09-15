using Sasd.Numerics.Approximation;

namespace Sasd.Math.Toolkit.Tests;

public sealed class ApproximationTests
{
    [Fact]
    public void PowerLawFit_RecoversExactScaleAndExponent()
    {
        const double expectedScale = 3.0;
        const double expectedExponent = 2.5;
        double[] x = [1.0, 2.0, 4.0, 8.0, 16.0];
        var y = x.Select(value => expectedScale * System.Math.Pow(value, expectedExponent)).ToArray();

        var result = LeastSquares.FitPowerLaw(x, y);

        Assert.InRange(result.Scale, expectedScale - 1e-11, expectedScale + 1e-11);
        Assert.InRange(result.Exponent, expectedExponent - 1e-12, expectedExponent + 1e-12);
        Assert.Equal(x.Length, result.SampleCount);
        Assert.InRange(result.RootMeanSquareError, 0.0, 1e-10);
        Assert.InRange(result.Evaluate(3.0),
            expectedScale * System.Math.Pow(3.0, expectedExponent) - 1e-10,
            expectedScale * System.Math.Pow(3.0, expectedExponent) + 1e-10);
    }

    [Fact]
    public void PowerLawFit_ReportsResidualsInOriginalDomain()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0];
        double[] y = [2.0, 4.1, 6.2, 7.9];

        var result = LeastSquares.FitPowerLaw(x, y);

        var expectedSse = x
            .Select((value, index) => y[index] - result.Evaluate(value))
            .Sum(residual => residual * residual);

        Assert.Equal(expectedSse, result.ResidualSumOfSquares, 12);
        Assert.Equal(System.Math.Sqrt(expectedSse / x.Length), result.RootMeanSquareError, 12);
    }

    [Fact]
    public void PowerLawFit_RejectsInvalidDomainsAndUndeterminedExponent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitPowerLaw(
            [0.0, 1.0],
            [1.0, 2.0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitPowerLaw(
            [1.0, 2.0],
            [1.0, -2.0]));

        Assert.Throws<ArgumentException>(() => LeastSquares.FitPowerLaw(
            [2.0, 2.0, 2.0],
            [1.0, 2.0, 3.0]));

        var result = LeastSquares.FitPowerLaw([1.0, 2.0], [2.0, 4.0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Evaluate(0.0));
    }

    [Fact]
    public void ExponentialFit_RecoversExactGrowthOrDecayParameters()
    {
        const double expectedScale = 2.5;
        const double expectedRate = -0.7;
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var y = x.Select(value => expectedScale * System.Math.Exp(expectedRate * value)).ToArray();

        var result = LeastSquares.FitExponential(x, y);

        Assert.InRange(result.Scale, expectedScale - 1e-12, expectedScale + 1e-12);
        Assert.InRange(result.Rate, expectedRate - 1e-12, expectedRate + 1e-12);
        Assert.Equal(x.Length, result.SampleCount);
        Assert.InRange(result.RootMeanSquareError, 0.0, 1e-12);
        Assert.InRange(
            result.Evaluate(0.75),
            expectedScale * System.Math.Exp(expectedRate * 0.75) - 1e-12,
            expectedScale * System.Math.Exp(expectedRate * 0.75) + 1e-12);
    }

    [Fact]
    public void ExponentialFit_ReportsResidualsInOriginalDomain()
    {
        double[] x = [0.0, 0.5, 1.0, 1.5];
        double[] y = [2.0, 2.8, 4.2, 6.1];

        var result = LeastSquares.FitExponential(x, y);

        var expectedSse = x
            .Select((value, index) => y[index] - result.Evaluate(value))
            .Sum(residual => residual * residual);

        Assert.Equal(expectedSse, result.ResidualSumOfSquares, 12);
        Assert.Equal(System.Math.Sqrt(expectedSse / x.Length), result.RootMeanSquareError, 12);
    }

    [Fact]
    public void ExponentialFit_RejectsInvalidInputsAndUndeterminedRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitExponential(
            [0.0, 1.0],
            [1.0, 0.0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitExponential(
            [0.0, double.NaN],
            [1.0, 2.0]));

        Assert.Throws<ArgumentException>(() => LeastSquares.FitExponential(
            [2.0, 2.0, 2.0],
            [1.0, 2.0, 3.0]));

        var result = LeastSquares.FitExponential([0.0, 1.0], [2.0, 4.0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Evaluate(double.PositiveInfinity));
    }

    [Fact]
    public void LogarithmicFit_RecoversExactParametersIncludingNegativeYValues()
    {
        const double expectedIntercept = 1.25;
        const double expectedLogCoefficient = -2.5;
        double[] x = [1.0, System.Math.E, System.Math.Exp(2.0), System.Math.Exp(3.0)];
        var y = x
            .Select(value => expectedIntercept + (expectedLogCoefficient * System.Math.Log(value)))
            .ToArray();

        var result = LeastSquares.FitLogarithmic(x, y);

        Assert.InRange(result.Intercept, expectedIntercept - 1e-12, expectedIntercept + 1e-12);
        Assert.InRange(
            result.LogCoefficient,
            expectedLogCoefficient - 1e-12,
            expectedLogCoefficient + 1e-12);
        Assert.Equal(x.Length, result.SampleCount);
        Assert.InRange(result.RootMeanSquareError, 0.0, 1e-12);
        Assert.InRange(
            result.Evaluate(System.Math.Exp(1.5)),
            expectedIntercept + (expectedLogCoefficient * 1.5) - 1e-12,
            expectedIntercept + (expectedLogCoefficient * 1.5) + 1e-12);
    }

    [Fact]
    public void LogarithmicFit_ReportsTheSameOriginalYResidualObjectiveItMinimizes()
    {
        double[] x = [1.0, 2.0, 4.0, 8.0];
        double[] y = [3.0, 4.2, 4.8, 6.1];

        var result = LeastSquares.FitLogarithmic(x, y);

        var expectedSse = x
            .Select((value, index) => y[index] - result.Evaluate(value))
            .Sum(residual => residual * residual);

        Assert.Equal(expectedSse, result.ResidualSumOfSquares, 12);
        Assert.Equal(System.Math.Sqrt(expectedSse / x.Length), result.RootMeanSquareError, 12);
    }

    [Fact]
    public void LogarithmicFit_RejectsInvalidDomainNonFiniteYAndUndeterminedCoefficient()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitLogarithmic(
            [0.0, 1.0],
            [1.0, 2.0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitLogarithmic(
            [1.0, 2.0],
            [1.0, double.NaN]));

        Assert.Throws<ArgumentException>(() => LeastSquares.FitLogarithmic(
            [2.0, 2.0, 2.0],
            [-1.0, 0.0, 1.0]));

        var result = LeastSquares.FitLogarithmic([1.0, 2.0], [-1.0, 1.0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Evaluate(0.0));
    }

    [Fact]
    public void PolynomialFit_StillReconstructsQuadraticExactly()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var y = x.Select(value => 1.0 - (2.0 * value) + (3.0 * value * value)).ToArray();

        var coefficients = LeastSquares.FitPolynomial(x, y, degree: 2);

        Assert.Equal(3, coefficients.Length);
        Assert.Equal(1.0, coefficients[0], 12);
        Assert.Equal(-2.0, coefficients[1], 12);
        Assert.Equal(3.0, coefficients[2], 12);
        Assert.Equal(4.75, LeastSquares.EvaluatePolynomial(coefficients, 1.5), 12);
    }
}
