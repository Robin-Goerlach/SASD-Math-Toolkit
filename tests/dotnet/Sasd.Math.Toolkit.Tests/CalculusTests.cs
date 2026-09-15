using Sasd.Numerics.Common;
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

        var first = SplineDifferentiation.ClampedFirstDerivative(x, y, 1.0, 5.0, point);
        var second = SplineDifferentiation.ClampedSecondDerivative(x, y, 1.0, 5.0, point);

        var expectedFirst = (3.0 * point * point) - (4.0 * point) + 1.0;
        var expectedSecond = (6.0 * point) - 4.0;

        Assert.InRange(first, expectedFirst - 1e-10, expectedFirst + 1e-10);
        Assert.InRange(second, expectedSecond - 1e-10, expectedSecond + 1e-10);
    }

    [Fact]
    public void CompositeRules_IntegrateReferencePolynomialsAndValidatePanelCounts()
    {
        var trapezoid = NumericalIntegration.CompositeTrapezoid(x => (3.0 * x) + 2.0, 0.0, 2.0, 8);
        var simpson = NumericalIntegration.CompositeSimpson(x => x * x * x, 0.0, 2.0, 10);

        Assert.Equal(10.0, trapezoid, 12);
        Assert.Equal(4.0, simpson, 12);
        Assert.Throws<ArgumentException>(() =>
            NumericalIntegration.CompositeSimpson(System.Math.Sin, 0.0, 1.0, 3));
    }

    [Fact]
    public void AdaptiveSimpsonDetailed_IntegratesSinAndReportsDiagnostics()
    {
        var result = NumericalIntegration.AdaptiveSimpsonDetailed(
            System.Math.Sin,
            0.0,
            System.Math.PI,
            tolerance: 1e-11,
            maximumDepth: 20);

        Assert.True(result.Converged);
        Assert.Equal(AdaptiveIntegrationStatus.Converged, result.Status);
        Assert.InRange(result.Value, 2.0 - 1e-11, 2.0 + 1e-11);
        Assert.InRange(result.EstimatedError, 0.0, 1e-11);
        Assert.True(result.FunctionEvaluations >= 5);
        Assert.True(result.AcceptedPanels >= 2);
        Assert.Equal(result.Value, NumericalIntegration.AdaptiveSimpson(System.Math.Sin, 0.0, System.Math.PI, 1e-11, 20), 12);
    }

    [Fact]
    public void AdaptiveSimpsonDetailed_ReportsDepthLimitInsteadOfPretendingConvergence()
    {
        var result = NumericalIntegration.AdaptiveSimpsonDetailed(
            System.Math.Exp,
            0.0,
            1.0,
            tolerance: 1e-30,
            maximumDepth: 1);

        Assert.False(result.Converged);
        Assert.Equal(AdaptiveIntegrationStatus.MaximumDepthReached, result.Status);
        Assert.True(double.IsFinite(result.Value));
        Assert.True(result.EstimatedError > 0.0);
    }

    [Fact]
    public void RombergDetailed_IntegratesPolynomialAndReportsLevelLimit()
    {
        var result = NumericalIntegration.RombergDetailed(x => x * x, 0.0, 1.0, 1e-12, 12);

        Assert.True(result.Converged);
        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.InRange(result.Value, (1.0 / 3.0) - 1e-12, (1.0 / 3.0) + 1e-12);
        Assert.NotNull(result.EstimatedError);
        Assert.True(result.FunctionEvaluations >= 3);

        var limited = NumericalIntegration.RombergDetailed(x => x * x, 0.0, 1.0, 1e-30, 1);
        Assert.False(limited.Converged);
        Assert.Equal(IterationStatus.MaximumIterationsReached, limited.Status);
        Assert.Null(limited.EstimatedError);
        Assert.Equal(1, limited.Levels);
    }

    [Fact]
    public void GaussLegendre5_IsExactForDegreeEightPolynomialWithinFloatingPointRoundoff()
    {
        var integral = NumericalIntegration.GaussLegendre5(x => System.Math.Pow(x, 8), -1.0, 1.0);
        Assert.InRange(integral, (2.0 / 9.0) - 1e-14, (2.0 / 9.0) + 1e-14);
    }

    [Fact]
    public void AdaptiveGaussLegendreDetailed_IntegratesExponentialAndReportsDiagnostics()
    {
        var result = NumericalIntegration.AdaptiveGaussLegendre5Detailed(
            System.Math.Exp,
            0.0,
            1.0,
            tolerance: 1e-12,
            maximumDepth: 12);

        var expected = System.Math.E - 1.0;
        Assert.True(result.Converged);
        Assert.InRange(result.Value, expected - 1e-12, expected + 1e-12);
        Assert.InRange(result.EstimatedError, 0.0, 1e-12);
        Assert.True(result.FunctionEvaluations >= 15);
        Assert.True(result.AcceptedPanels >= 2);
        Assert.Equal(result.Value, NumericalIntegration.AdaptiveGaussLegendre5(System.Math.Exp, 0.0, 1.0, 1e-12, 12), 12);
    }

    [Fact]
    public void Integration_RejectsNonFiniteIntegrandResultsAcrossAlgorithmFamilies()
    {
        static double Invalid(double _) => double.NaN;

        Assert.Throws<ArithmeticException>(() => NumericalIntegration.CompositeTrapezoid(Invalid, 0.0, 1.0, 4));
        Assert.Throws<ArithmeticException>(() => NumericalIntegration.CompositeSimpson(Invalid, 0.0, 1.0, 4));
        Assert.Throws<ArithmeticException>(() => NumericalIntegration.AdaptiveSimpson(Invalid, 0.0, 1.0));
        Assert.Throws<ArithmeticException>(() => NumericalIntegration.Romberg(Invalid, 0.0, 1.0));
        Assert.Throws<ArithmeticException>(() => NumericalIntegration.GaussLegendre5(Invalid, 0.0, 1.0));
        Assert.Throws<ArithmeticException>(() => NumericalIntegration.AdaptiveGaussLegendre5(Invalid, 0.0, 1.0));
    }

    [Fact]
    public void Integration_RejectsInvalidOrUnrepresentableIntervalsAndUnsafeRombergDepth()
    {
        Assert.Throws<ArgumentException>(() =>
            NumericalIntegration.CompositeTrapezoid(System.Math.Sin, 1.0, 1.0, 4));
        Assert.Throws<ArgumentException>(() =>
            NumericalIntegration.CompositeTrapezoid(System.Math.Sin, 2.0, 1.0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NumericalIntegration.CompositeTrapezoid(System.Math.Sin, -double.MaxValue, double.MaxValue, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NumericalIntegration.Romberg(System.Math.Sin, 0.0, 1.0, maximumLevels: 31));
    }

    private static double Quartic(double x) =>
        (x * x * x * x) - (2.0 * x * x * x) + (x * x) + (3.0 * x) - 5.0;

    private static double Cubic(double x) =>
        (x * x * x) - (2.0 * x * x) + x + 1.0;
}
