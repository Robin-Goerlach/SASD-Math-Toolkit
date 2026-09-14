using Sasd.Numerics.Differentiation;
using Sasd.Numerics.Integration;

namespace Sasd.Math.Toolkit.Tests;

public sealed class CalculusTests
{
    [Fact]
    public void Differentiation_MatchesSinDerivative()
    {
        var derivative = NumericalDifferentiation.FirstDerivative(System.Math.Sin, 0.3);
        Assert.InRange(derivative, System.Math.Cos(0.3) - 1e-8, System.Math.Cos(0.3) + 1e-8);
    }

    [Fact]
    public void TabularTwoPoint_FirstDerivative_IsExactForLinearDataAtBoundaries()
    {
        var x = new[] { 0.0, 0.4, 1.2, 2.0 };
        var y = x.Select(value => (3.0 * value) + 2.0).ToArray();

        Assert.Equal(3.0, TabularDifferentiation.FirstDerivativeTwoPoint(x, y, 0), 12);
        Assert.Equal(3.0, TabularDifferentiation.FirstDerivativeTwoPoint(x, y, x.Length - 1), 12);
    }

    [Fact]
    public void TabularThreePoint_Derivatives_AreExactForQuadraticOnNonUniformGrid()
    {
        var x = new[] { 0.0, 0.5, 2.0 };
        var y = x.Select(value => (value * value) + (2.0 * value) + 1.0).ToArray();

        var first = TabularDifferentiation.FirstDerivativeThreePoint(x, y, 1);
        var second = TabularDifferentiation.SecondDerivativeThreePoint(x, y, 1);

        Assert.Equal(3.0, first, 12);
        Assert.Equal(2.0, second, 12);
    }

    [Fact]
    public void TabularFivePoint_Derivatives_AreExactForQuarticIncludingBoundaryStencil()
    {
        var x = new[] { 0.0, 0.7, 1.5, 2.4, 4.0 };
        var y = x.Select(Quartic).ToArray();

        Assert.InRange(TabularDifferentiation.FirstDerivativeFivePoint(x, y, 0), 3.0 - 1e-10, 3.0 + 1e-10);
        Assert.InRange(TabularDifferentiation.SecondDerivativeFivePoint(x, y, 0), 2.0 - 1e-10, 2.0 + 1e-10);

        var point = x[2];
        var expectedFirst = (4.0 * point * point * point) - (6.0 * point * point) + (2.0 * point) + 3.0;
        var expectedSecond = (12.0 * point * point) - (12.0 * point) + 2.0;

        Assert.InRange(
            TabularDifferentiation.FirstDerivativeFivePoint(x, y, 2),
            expectedFirst - 1e-10,
            expectedFirst + 1e-10);
        Assert.InRange(
            TabularDifferentiation.SecondDerivativeFivePoint(x, y, 2),
            expectedSecond - 1e-10,
            expectedSecond + 1e-10);
    }

    [Fact]
    public void TabularDifferentiation_RejectsNonIncreasingAbscissas()
    {
        var x = new[] { 0.0, 1.0, 1.0 };
        var y = new[] { 0.0, 1.0, 2.0 };

        Assert.Throws<ArgumentException>(() => TabularDifferentiation.FirstDerivativeThreePoint(x, y, 1));
    }

    [Fact]
    public void SplineDifferentiation_ClampedSplineReproducesCubicDerivatives()
    {
        var x = new[] { 0.0, 0.5, 1.5, 2.0 };
        var y = x.Select(Cubic).ToArray();
        const double point = 1.2;

        // f(x)=x^3-2x^2+x+1 has f'(0)=1 and f'(2)=5. A clamped cubic
        // spline with exact endpoint derivatives reproduces the cubic itself.
        var first = SplineDifferentiation.ClampedFirstDerivative(x, y, 1.0, 5.0, point);
        var second = SplineDifferentiation.ClampedSecondDerivative(x, y, 1.0, 5.0, point);

        var expectedFirst = (3.0 * point * point) - (4.0 * point) + 1.0;
        var expectedSecond = (6.0 * point) - 4.0;

        Assert.InRange(first, expectedFirst - 1e-10, expectedFirst + 1e-10);
        Assert.InRange(second, expectedSecond - 1e-10, expectedSecond + 1e-10);
    }

    [Fact]
    public void Simpson_IntegratesSin()
    {
        // Composite Simpson converges with fourth order for smooth functions.
        // 200 intervals keep this test strict without demanding more accuracy
        // than the selected discretization can mathematically provide.
        var integral = NumericalIntegration.CompositeSimpson(System.Math.Sin, 0.0, System.Math.PI, 200);
        Assert.InRange(integral, 2.0 - 1e-9, 2.0 + 1e-9);
    }

    [Fact]
    public void Romberg_IntegratesPolynomial()
    {
        var integral = NumericalIntegration.Romberg(x => x * x, 0.0, 1.0);
        Assert.InRange(integral, (1.0 / 3.0) - 1e-10, (1.0 / 3.0) + 1e-10);
    }

    private static double Quartic(double x) =>
        (x * x * x * x) - (2.0 * x * x * x) + (x * x) + (3.0 * x) - 5.0;

    private static double Cubic(double x) =>
        (x * x * x) - (2.0 * x * x) + x + 1.0;
}
