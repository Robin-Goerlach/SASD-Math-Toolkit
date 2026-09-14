using Sasd.Numerics.Common;
using Sasd.Numerics.DifferentialEquations;

namespace Sasd.Math.Toolkit.Tests;

public sealed class NonlinearShootingTests
{
    [Fact]
    public void NonlinearShooting_SolvesCubicNonlinearDirichletProblem()
    {
        // y = 1/(1-x) satisfies y'' = 2y^3. On [0, 0.5] the boundary values
        // are y(0)=1 and y(0.5)=2, while the unknown exact initial slope is 1.
        var result = NonlinearShooting.Solve(
            (_, y, _) => 2.0 * y * y * y,
            x0: 0.0,
            leftValue: 1.0,
            xEnd: 0.5,
            rightValue: 2.0,
            step: 0.005,
            firstSlopeGuess: 0.5,
            secondSlopeGuess: 1.5,
            options: new NonlinearShootingOptions(BoundaryTolerance: 1e-10, MaximumIterations: 25));

        Assert.True(result.Converged);
        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.True(result.Iterations > 0);
        Assert.InRange(result.InitialSlope, 1.0 - 2e-8, 1.0 + 2e-8);
        Assert.InRange(System.Math.Abs(result.RightBoundaryResidual), 0.0, 1e-10);
        Assert.InRange(result.FinalPoint.Y, 2.0 - 1e-10, 2.0 + 1e-10);
        Assert.InRange(result.FinalPoint.FirstDerivative, 4.0 - 2e-7, 4.0 + 2e-7);

        // With h=0.005, point 50 is x=0.25 where the exact solution is 4/3.
        var middle = result.Points[50];
        Assert.Equal(0.25, middle.X, 12);
        Assert.InRange(middle.Y, (4.0 / 3.0) - 2e-8, (4.0 / 3.0) + 2e-8);
    }

    [Fact]
    public void NonlinearShooting_ReturnsLastShotWhenMaximumIterationsAreReached()
    {
        var result = NonlinearShooting.Solve(
            (_, y, _) => 2.0 * y * y * y,
            x0: 0.0,
            leftValue: 1.0,
            xEnd: 0.5,
            rightValue: 2.0,
            step: 0.01,
            firstSlopeGuess: -1.0,
            secondSlopeGuess: 3.0,
            options: new NonlinearShootingOptions(BoundaryTolerance: 1e-14, MaximumIterations: 1));

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.MaximumIterationsReached, result.Status);
        Assert.Equal(1, result.Iterations);
        Assert.True(double.IsFinite(result.InitialSlope));
        Assert.True(double.IsFinite(result.RightBoundaryResidual));
        Assert.Equal(0.5, result.FinalPoint.X, 12);
    }

    [Fact]
    public void NonlinearShooting_ReportsSecantBreakdownForIdenticalResiduals()
    {
        // Both identical slope guesses produce the same nonzero residual. The secant
        // denominator is therefore zero, which is an expected iterative breakdown and
        // should be reported as status rather than disguised as convergence.
        var result = NonlinearShooting.Solve(
            (_, y, _) => y * y,
            x0: 0.0,
            leftValue: 0.0,
            xEnd: 1.0,
            rightValue: 1.0,
            step: 0.05,
            firstSlopeGuess: 0.0,
            secondSlopeGuess: 0.0);

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(-1.0, result.RightBoundaryResidual, 12);
    }

    [Fact]
    public void NonlinearShooting_RejectsNonFiniteAcceleration()
    {
        Assert.Throws<ArithmeticException>(() => NonlinearShooting.Solve(
            (_, _, _) => double.NaN,
            x0: 0.0,
            leftValue: 0.0,
            xEnd: 1.0,
            rightValue: 1.0,
            step: 0.1,
            firstSlopeGuess: 0.0,
            secondSlopeGuess: 1.0));
    }
}
