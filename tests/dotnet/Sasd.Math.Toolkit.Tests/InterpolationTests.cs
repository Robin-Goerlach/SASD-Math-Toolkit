using Sasd.Numerics.Interpolation;

namespace Sasd.Math.Toolkit.Tests;

public sealed class InterpolationTests
{
    [Fact]
    public void Lagrange_ReconstructsQuadratic()
    {
        double[] x = [0.0, 1.0, 2.0];
        double[] y = [1.0, 4.0, 9.0];
        Assert.Equal(6.25, InterpolationAlgorithms.Lagrange(x, y, 1.5), 10);
    }

    [Fact]
    public void NewtonDividedDifference_ReconstructsQuadratic()
    {
        double[] x = [0.0, 1.0, 2.0];
        double[] y = [1.0, 4.0, 9.0];
        Assert.Equal(6.25, InterpolationAlgorithms.NewtonDividedDifference(x, y, 1.5), 10);
    }

    [Fact]
    public void NaturalSpline_PassesThroughKnots()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0];
        double[] y = [0.0, 1.0, 0.0, 1.0];
        var spline = InterpolationAlgorithms.NaturalCubicSpline(x, y);
        for (var i = 0; i < x.Length; i++) Assert.Equal(y[i], spline.Evaluate(x[i]), 10);
    }
}
