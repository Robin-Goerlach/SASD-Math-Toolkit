using Sasd.Numerics.Approximation;

namespace Sasd.Math.Toolkit.Tests;

public sealed class FourierApproximationTests
{
    [Fact]
    public void FiveTermFourierFit_RecoversExactCoefficients()
    {
        const double period = 4.0;
        var omega = 2.0 * System.Math.PI / period;
        var x = Enumerable.Range(0, 20).Select(index => index * 0.2).ToArray();
        var y = x.Select(value =>
            1.5
            + (2.0 * System.Math.Cos(omega * value))
            - (0.5 * System.Math.Sin(omega * value))
            + (0.75 * System.Math.Cos(2.0 * omega * value))
            + (1.25 * System.Math.Sin(2.0 * omega * value))).ToArray();

        var result = LeastSquares.FitFiveTermFourierForPeriod(x, y, period);

        Assert.InRange(result.ConstantTerm, 1.5 - 1e-11, 1.5 + 1e-11);
        Assert.InRange(result.FundamentalCosineCoefficient, 2.0 - 1e-11, 2.0 + 1e-11);
        Assert.InRange(result.FundamentalSineCoefficient, -0.5 - 1e-11, -0.5 + 1e-11);
        Assert.InRange(result.SecondHarmonicCosineCoefficient, 0.75 - 1e-11, 0.75 + 1e-11);
        Assert.InRange(result.SecondHarmonicSineCoefficient, 1.25 - 1e-11, 1.25 + 1e-11);
        Assert.Equal(period, result.Period, 12);
        Assert.Equal(x.Length, result.SampleCount);
        Assert.InRange(result.RootMeanSquareError, 0.0, 1e-11);
    }

    [Fact]
    public void FiveTermFourierFit_ReportsResidualsAndEvaluatesPeriodically()
    {
        const double period = 6.0;
        double[] x = [0.0, 0.6, 1.2, 1.8, 2.4, 3.0, 3.6, 4.2, 4.8, 5.4];
        double[] y = [2.0, 2.9, 2.1, 0.4, -0.2, 0.8, 1.7, 2.5, 2.2, 1.4];

        var result = LeastSquares.FitFiveTermFourierForPeriod(x, y, period);
        var expectedSse = x
            .Select((value, index) => y[index] - result.Evaluate(value))
            .Sum(residual => residual * residual);

        Assert.Equal(expectedSse, result.ResidualSumOfSquares, 12);
        Assert.Equal(System.Math.Sqrt(expectedSse / x.Length), result.RootMeanSquareError, 12);
        Assert.Equal(result.Evaluate(1.25), result.Evaluate(1.25 + period), 11);
    }

    [Fact]
    public void FiveTermFourierFit_ValidatesFrequencySamplesAndBasisRank()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitFiveTermFourier(
            [0.0, 1.0, 2.0, 3.0, 4.0],
            [1.0, 2.0, 3.0, 4.0, 5.0],
            fundamentalAngularFrequency: 0.0));

        Assert.Throws<ArgumentException>(() => LeastSquares.FitFiveTermFourier(
            [0.0, 1.0, 2.0, 3.0],
            [1.0, 2.0, 3.0, 4.0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.FitFiveTermFourier(
            [0.0, 1.0, double.NaN, 3.0, 4.0],
            [1.0, 2.0, 3.0, 4.0, 5.0]));

        Assert.Throws<ArithmeticException>(() => LeastSquares.FitFiveTermFourier(
            [0.0, 0.0, 0.0, 0.0, 0.0],
            [1.0, 2.0, 3.0, 4.0, 5.0]));
    }
}
