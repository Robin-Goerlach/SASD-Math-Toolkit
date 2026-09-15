using Sasd.Numerics.Interpolation;

namespace Sasd.Math.Toolkit.Tests;

public sealed class InterpolationTests
{
    [Fact]
    public void Lagrange_ReconstructsQuadratic()
    {
        double[] x = [0.0, 1.0, 2.0];
        double[] y = [1.0, 4.0, 9.0];

        var value = InterpolationAlgorithms.Lagrange(x, y, 1.5);

        Assert.Equal(6.25, value, 10);
    }

    [Fact]
    public void NewtonDividedDifference_ReconstructsQuadratic()
    {
        double[] x = [0.0, 1.0, 2.0];
        double[] y = [1.0, 4.0, 9.0];

        var value = InterpolationAlgorithms.NewtonDividedDifference(x, y, 1.5);

        Assert.Equal(6.25, value, 10);
    }

    [Fact]
    public void NewtonDividedDifferenceCoefficients_MatchNewtonFormAndAreIndependent()
    {
        double[] x = [0.0, 1.0, 2.0];
        double[] y = [1.0, 4.0, 9.0];

        var coefficients = InterpolationAlgorithms.NewtonDividedDifferenceCoefficients(x, y);
        y[0] = 999.0;

        Assert.Equal([1.0, 3.0, 1.0], coefficients);
    }

    [Fact]
    public void PolynomialInterpolation_RejectsDuplicateAbscissasAndNonFinitePoint()
    {
        double[] duplicateX = [0.0, 1.0, 1.0];
        double[] y = [1.0, 2.0, 3.0];

        Assert.Throws<ArgumentException>(() => InterpolationAlgorithms.Lagrange(duplicateX, y, 0.5));
        Assert.Throws<ArgumentException>(() =>
            InterpolationAlgorithms.NewtonDividedDifferenceCoefficients(duplicateX, y));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InterpolationAlgorithms.Lagrange([0.0, 1.0], [0.0, 1.0], double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InterpolationAlgorithms.NewtonDividedDifference([0.0, 1.0], [0.0, 1.0], double.PositiveInfinity));
    }

    [Fact]
    public void NaturalSpline_PassesThroughKnotsAndHasNaturalEndpointCurvature()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0];
        double[] y = [0.0, 1.0, 0.0, 1.0];
        var spline = InterpolationAlgorithms.NaturalCubicSpline(x, y);

        for (var i = 0; i < x.Length; i++)
        {
            Assert.Equal(y[i], spline.Evaluate(x[i]), 10);
        }

        Assert.InRange(System.Math.Abs(spline.SecondDerivative(x[0])), 0.0, 1e-12);
        Assert.InRange(System.Math.Abs(spline.SecondDerivative(x[^1])), 0.0, 1e-12);
    }

    [Fact]
    public void ClampedSpline_ReproducesCubicWhenEndpointSlopesAreExact()
    {
        static double Function(double x) => (x * x * x) - (2.0 * x * x) + x + 1.0;
        static double FirstDerivative(double x) => (3.0 * x * x) - (4.0 * x) + 1.0;
        static double SecondDerivative(double x) => (6.0 * x) - 4.0;

        double[] x = [0.0, 1.0, 2.0, 3.0];
        var y = x.Select(Function).ToArray();
        var spline = InterpolationAlgorithms.ClampedCubicSpline(
            x,
            y,
            FirstDerivative(x[0]),
            FirstDerivative(x[^1]));

        const double point = 1.5;
        Assert.InRange(System.Math.Abs(spline.Evaluate(point) - Function(point)), 0.0, 1e-12);
        Assert.InRange(System.Math.Abs(spline.FirstDerivative(point) - FirstDerivative(point)), 0.0, 1e-12);
        Assert.InRange(System.Math.Abs(spline.SecondDerivative(point) - SecondDerivative(point)), 0.0, 1e-12);
        Assert.InRange(System.Math.Abs(spline.FirstDerivative(x[0]) - FirstDerivative(x[0])), 0.0, 1e-12);
        Assert.InRange(System.Math.Abs(spline.FirstDerivative(x[^1]) - FirstDerivative(x[^1])), 0.0, 1e-12);
    }

    [Fact]
    public void CubicSpline_ExposesReadOnlyDomainMetadataAndRejectsExtrapolation()
    {
        var spline = InterpolationAlgorithms.NaturalCubicSpline(
            [0.0, 1.0, 2.0],
            [0.0, 1.0, 0.0]);

        Assert.Equal(0.0, spline.IntervalStart);
        Assert.Equal(2.0, spline.IntervalEnd);
        Assert.Equal(2, spline.SegmentCount);

        var knotList = Assert.IsAssignableFrom<IList<double>>(spline.Knots);
        Assert.Throws<NotSupportedException>(() => knotList[0] = 99.0);
        Assert.Equal(0.0, spline.Knots[0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => spline.Evaluate(-0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => spline.FirstDerivative(2.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => spline.SecondDerivative(double.NaN));
    }

    [Fact]
    public void CubicSpline_RejectsUnorderedAndNonFiniteData()
    {
        Assert.Throws<ArgumentException>(() =>
            InterpolationAlgorithms.NaturalCubicSpline([0.0, 2.0, 1.0], [0.0, 1.0, 2.0]));

        Assert.Throws<ArgumentException>(() =>
            InterpolationAlgorithms.NaturalCubicSpline([0.0, 1.0], [0.0, double.NaN]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InterpolationAlgorithms.ClampedCubicSpline([0.0, 1.0], [0.0, 1.0], double.PositiveInfinity, 1.0));
    }
}
