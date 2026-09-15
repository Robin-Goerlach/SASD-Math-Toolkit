using Sasd.Numerics.Common;
using Sasd.Numerics.RootFinding;

namespace Sasd.Math.Toolkit.Tests;

public sealed class RootFindingTests
{
    private const double CosXEqualsXRoot = 0.7390851332151607;

    [Fact]
    public void Bisection_FindsCosXEqualsXRoot()
    {
        var result = RootSolvers.Bisection(x => System.Math.Cos(x) - x, 0.0, 1.0);

        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.InRange(result.Root, CosXEqualsXRoot - 1e-12, CosXEqualsXRoot + 1e-12);
        Assert.InRange(result.Residual, 0.0, 1e-12);
    }

    [Fact]
    public void Bisection_ReportsMissingBracketWithoutThrowing()
    {
        var result = RootSolvers.Bisection(x => (x * x) + 1.0, -1.0, 1.0);

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NotBracketed, result.Status);
        Assert.True(double.IsNaN(result.Root));
        Assert.True(double.IsNaN(result.Residual));
        Assert.NotNull(result.Message);
        Assert.Contains("opposite signs", result.Message!);
    }

    [Fact]
    public void NewtonRaphson_FindsCosXEqualsXRoot()
    {
        var result = RootSolvers.NewtonRaphson(
            x => System.Math.Cos(x) - x,
            x => -System.Math.Sin(x) - 1.0,
            0.0);

        Assert.True(result.Converged);
        Assert.InRange(result.Root, CosXEqualsXRoot - 1e-12, CosXEqualsXRoot + 1e-12);
        Assert.InRange(result.Residual, 0.0, 1e-12);
    }

    [Fact]
    public void NewtonRaphson_ReportsBreakdownForZeroDerivative()
    {
        var result = RootSolvers.NewtonRaphson(
            x => (x * x) + 1.0,
            x => 2.0 * x,
            0.0);

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0.0, result.Root);
        Assert.Equal(1.0, result.Residual);
    }

    [Fact]
    public void NewtonRaphson_RejectsOverflowedIterateBeforeReevaluatingFunction()
    {
        var sawNonFiniteInput = false;

        double Function(double x)
        {
            if (!double.IsFinite(x))
            {
                sawNonFiniteInput = true;
            }

            return 1e308;
        }

        Assert.Throws<ArithmeticException>(() =>
            RootSolvers.NewtonRaphson(Function, _ => 2e-15, 0.0));

        Assert.False(sawNonFiniteInput);
    }

    [Fact]
    public void Secant_FindsCosXEqualsXRoot()
    {
        var result = RootSolvers.Secant(x => System.Math.Cos(x) - x, 0.0, 1.0);

        Assert.True(result.Converged);
        Assert.InRange(result.Root, CosXEqualsXRoot - 1e-12, CosXEqualsXRoot + 1e-12);
        Assert.InRange(result.Residual, 0.0, 1e-12);
    }

    [Fact]
    public void Secant_ReportsBreakdownForFlatSlope()
    {
        var result = RootSolvers.Secant(_ => 1.0, 0.0, 1.0);

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(1.0, result.Residual);
    }

    [Fact]
    public void RootResult_ReportsAbsoluteResidual()
    {
        var result = new RootResult(
            Root: 2.0,
            FunctionValue: -0.25,
            Iterations: 3,
            Status: IterationStatus.MaximumIterationsReached);

        Assert.Equal(0.25, result.Residual);
    }

    [Fact]
    public void RootSolvers_RejectNonFiniteCallbackResults()
    {
        Assert.Throws<ArithmeticException>(() =>
            RootSolvers.Bisection(_ => double.NaN, 0.0, 1.0));
    }
}
