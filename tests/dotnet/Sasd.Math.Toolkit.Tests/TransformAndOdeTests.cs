using System.Numerics;
using Sasd.Numerics.DifferentialEquations;
using Sasd.Numerics.Transforms;

namespace Sasd.Math.Toolkit.Tests;

public sealed class TransformAndOdeTests
{
    [Fact]
    public void Fft_RoundTripsSignal()
    {
        Complex[] input = [1.0, 2.0, 3.0, 4.0];
        var spectrum = FastFourierTransform.Forward(input);
        var restored = FastFourierTransform.Inverse(spectrum);
        for (var i = 0; i < input.Length; i++) Assert.InRange(Complex.Abs(input[i] - restored[i]), 0.0, 1e-10);
    }

    [Fact]
    public void RungeKutta_SolvesExponentialGrowth()
    {
        var points = RungeKutta.FourthOrder((_, y) => y, 0.0, 1.0, 1.0, 0.01);
        Assert.InRange(points[^1].Y, System.Math.E - 1e-7, System.Math.E + 1e-7);
    }

    [Fact]
    public void RungeKuttaFehlberg_AdaptivelySolvesExponentialGrowth()
    {
        var result = RungeKuttaFehlberg.Integrate(
            (_, y) => y,
            0.0,
            1.0,
            1.0,
            new RungeKuttaFehlbergOptions
            {
                InitialStep = 0.25,
                MaximumStep = 0.5,
                AbsoluteTolerance = 1e-10,
                RelativeTolerance = 1e-10
            });

        Assert.True(result.Completed);
        Assert.Equal(AdaptiveOdeStatus.Completed, result.Status);
        Assert.Equal(1.0, result.FinalPoint.X, 12);
        Assert.InRange(result.FinalPoint.Y, System.Math.E - 5e-9, System.Math.E + 5e-9);
        Assert.True(result.AcceptedSteps > 1);
    }

    [Fact]
    public void RungeKuttaFehlberg_RejectsOverlargeTrialAndRecovers()
    {
        var result = RungeKuttaFehlberg.Integrate(
            (_, y) => y,
            0.0,
            1.0,
            1.0,
            new RungeKuttaFehlbergOptions
            {
                InitialStep = 1.0,
                MaximumStep = 1.0,
                AbsoluteTolerance = 1e-12,
                RelativeTolerance = 1e-12
            });

        Assert.True(result.Completed);
        Assert.True(result.RejectedSteps > 0);
        Assert.True(result.AcceptedSteps > 0);
        Assert.InRange(result.FinalPoint.Y, System.Math.E - 1e-8, System.Math.E + 1e-8);
    }

    [Fact]
    public void RungeKuttaFehlberg_ReportsMinimumStepWhenToleranceCannotBeMet()
    {
        var result = RungeKuttaFehlberg.Integrate(
            (_, y) => y,
            0.0,
            1.0,
            1.0,
            new RungeKuttaFehlbergOptions
            {
                InitialStep = 1.0,
                MinimumStep = 1.0,
                MaximumStep = 1.0,
                AbsoluteTolerance = 1e-16,
                RelativeTolerance = 1e-16
            });

        Assert.False(result.Completed);
        Assert.Equal(AdaptiveOdeStatus.MinimumStepSizeReached, result.Status);
        Assert.Equal(0, result.AcceptedSteps);
        Assert.Equal(1, result.RejectedSteps);
        Assert.Equal(0.0, result.FinalPoint.X, 12);
    }

    [Fact]
    public void AdamsPredictorCorrector_SolvesExponentialGrowth()
    {
        var points = AdamsBashforthMoulton.Integrate(
            (_, y) => y,
            x0: 0.0,
            y0: 1.0,
            xEnd: 1.0,
            maximumStep: 0.1);

        Assert.Equal(1.0, points[^1].X, 12);
        Assert.InRange(points[^1].Y, System.Math.E - 1e-5, System.Math.E + 1e-5);
    }

    [Fact]
    public void AdamsPredictorCorrector_IsExactForCubicWithQuadraticDerivative()
    {
        // y = x^3 has y' = 3x^2. The RK4 starter and fourth-order Adams formulas
        // integrate this low-degree polynomial to floating-point round-off.
        var points = AdamsBashforthMoulton.Integrate(
            (x, _) => 3.0 * x * x,
            x0: 0.0,
            y0: 0.0,
            xEnd: 1.0,
            maximumStep: 0.2);

        Assert.InRange(points[^1].Y, 1.0 - 1e-12, 1.0 + 1e-12);
    }

    [Fact]
    public void AdamsPredictorCorrector_UsesUniformGridAndEndsExactlyAtRequestedPoint()
    {
        var points = AdamsBashforthMoulton.Integrate(
            (_, _) => 1.0,
            x0: 0.0,
            y0: 0.0,
            xEnd: 1.0,
            maximumStep: 0.3);

        // ceil(1 / 0.3) = 4, so the reference implementation uses four equal
        // 0.25 intervals rather than three 0.3 intervals plus a short final step.
        Assert.Equal(5, points.Count);
        for (var i = 1; i < points.Count; i++)
        {
            Assert.Equal(0.25, points[i].X - points[i - 1].X, 12);
        }

        Assert.Equal(1.0, points[^1].X, 12);
        Assert.Equal(1.0, points[^1].Y, 12);
    }

    [Fact]
    public void AdamsPredictorCorrector_RejectsNonFiniteDerivative()
    {
        Assert.Throws<ArithmeticException>(() => AdamsBashforthMoulton.Integrate(
            (x, y) => x >= 0.5 ? double.NaN : y,
            x0: 0.0,
            y0: 1.0,
            xEnd: 1.0,
            maximumStep: 0.1));
    }
}
