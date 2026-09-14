using Sasd.Numerics.DifferentialEquations;

namespace Sasd.Math.Toolkit.Tests;

public sealed class BoundaryValueProblemTests
{
    [Fact]
    public void LinearShooting_SolvesForcedPolynomialDirichletProblem()
    {
        // y'' = 2, y(0)=1, y(1)=4 has y = 1 + 2x + x^2.
        // This verifies the forcing term, the reconstructed initial slope and the
        // returned first derivative, not only the right boundary value.
        var result = LinearShooting.Solve(
            _ => 0.0,
            _ => 0.0,
            _ => 2.0,
            x0: 0.0,
            leftValue: 1.0,
            xEnd: 1.0,
            rightValue: 4.0,
            step: 0.2);

        Assert.Equal(2.0, result.InitialSlope, 12);
        Assert.Equal(0.0, result.RightBoundaryResidual, 12);
        Assert.Equal(4.0, result.FinalPoint.Y, 12);
        Assert.Equal(4.0, result.FinalPoint.FirstDerivative, 12);

        var middle = result.Points[3]; // x = 0.6 on the chosen grid.
        Assert.Equal(0.6, middle.X, 12);
        Assert.Equal(2.56, middle.Y, 12);
        Assert.Equal(3.2, middle.FirstDerivative, 12);
    }

    [Fact]
    public void LinearShooting_SolvesHarmonicDirichletProblem()
    {
        // y'' = -y, y(0)=0, y(pi/2)=1 has y=sin(x) and y'(0)=1.
        var result = LinearShooting.Solve(
            _ => 0.0,
            _ => -1.0,
            _ => 0.0,
            x0: 0.0,
            leftValue: 0.0,
            xEnd: System.Math.PI / 2.0,
            rightValue: 1.0,
            step: 0.01);

        Assert.InRange(result.InitialSlope, 1.0 - 2e-9, 1.0 + 2e-9);
        Assert.Equal(System.Math.PI / 2.0, result.FinalPoint.X, 12);
        Assert.InRange(result.FinalPoint.Y, 1.0 - 2e-12, 1.0 + 2e-12);
        Assert.InRange(result.FinalPoint.FirstDerivative, -2e-9, 2e-9);
        Assert.InRange(System.Math.Abs(result.RightBoundaryResidual), 0.0, 2e-12);
    }

    [Fact]
    public void LinearShooting_RejectsNumericallySingularBoundaryMap()
    {
        // For y'' = -pi^2 y on [0,1], the homogeneous sensitivity satisfying
        // v(0)=0, v'(0)=1 has v(1)=0 analytically. The right boundary therefore
        // cannot determine the missing initial slope uniquely/stably.
        Assert.Throws<InvalidOperationException>(() => LinearShooting.Solve(
            _ => 0.0,
            _ => -(System.Math.PI * System.Math.PI),
            _ => 0.0,
            x0: 0.0,
            leftValue: 0.0,
            xEnd: 1.0,
            rightValue: 1.0,
            step: 0.01,
            singularityTolerance: 1e-5));
    }

    [Fact]
    public void LinearShooting_RejectsNonFiniteCoefficient()
    {
        Assert.Throws<ArithmeticException>(() => LinearShooting.Solve(
            _ => double.NaN,
            _ => 0.0,
            _ => 0.0,
            x0: 0.0,
            leftValue: 0.0,
            xEnd: 1.0,
            rightValue: 1.0,
            step: 0.1));
    }
}
