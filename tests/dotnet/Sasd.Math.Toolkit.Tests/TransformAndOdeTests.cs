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
}
