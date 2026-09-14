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
}
