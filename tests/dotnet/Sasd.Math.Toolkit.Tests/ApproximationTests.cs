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
    public void PolynomialFit_StillReconstructsQuadraticExactly()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var y = x.Select(value => 1.0 - (2.0 * value) + (3.0 * value * value)).ToArray();

        var coefficients = LeastSquares.FitPolynomial(x, y, degree: 2);

        Assert.Equal(3, coefficients.Length);
        Assert.Equal(1.0, coefficients[0], 12);
        Assert.Equal(-2.0, coefficients[1], 12);
        Assert.Equal(3.0, coefficients[2], 12);
        Assert.Equal(6.25, LeastSquares.EvaluatePolynomial(coefficients, 1.5), 12);
    }
}
