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
}
