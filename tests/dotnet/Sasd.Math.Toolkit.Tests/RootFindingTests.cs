using Sasd.Numerics.Common;
using Sasd.Numerics.RootFinding;

namespace Sasd.Math.Toolkit.Tests;

public sealed class RootFindingTests
{
    [Fact]
    public void Bisection_FindsCosXEqualsXRoot()
    {
        var result = RootSolvers.Bisection(x => System.Math.Cos(x) - x, 0.0, 1.0);
        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.InRange(result.Root, 0.7390851331 - 1e-10, 0.7390851331 + 1e-10);
    }

    [Fact]
    public void NewtonRaphson_FindsCosXEqualsXRoot()
    {
        var result = RootSolvers.NewtonRaphson(
            x => System.Math.Cos(x) - x,
            x => -System.Math.Sin(x) - 1.0,
            0.0);
        Assert.True(result.Converged);
        Assert.InRange(result.Root, 0.7390851331 - 1e-10, 0.7390851331 + 1e-10);
    }

    [Fact]
    public void Secant_FindsCosXEqualsXRoot()
    {
        var result = RootSolvers.Secant(x => System.Math.Cos(x) - x, 0.0, 1.0);
        Assert.True(result.Converged);
        Assert.InRange(result.Root, 0.7390851331 - 1e-10, 0.7390851331 + 1e-10);
    }
}
