using Sasd.Numerics.DifferentialEquations;

namespace Sasd.Math.Toolkit.Tests;

public sealed class SecondOrderSystemOdeTests
{
    [Fact]
    public void RungeKuttaSecondOrderSystem_SolvesCoupledOscillators()
    {
        // Two equal masses connected only to each other:
        // y0'' = -(y0-y1), y1'' = +(y0-y1).
        // With y0(0)=1, y1(0)=-1 and zero velocities, symmetry gives
        // y0=cos(sqrt(2)x), y1=-cos(sqrt(2)x).
        var end = System.Math.PI / (2.0 * System.Math.Sqrt(2.0));
        var points = RungeKutta.FourthOrderSecondOrderSystem(
            (_, values, _) =>
            {
                var difference = values[0] - values[1];
                return [-difference, difference];
            },
            x0: 0.0,
            initialValues: [1.0, -1.0],
            initialFirstDerivatives: [0.0, 0.0],
            xEnd: end,
            step: 0.01);

        var final = points[^1];
        var expectedSpeed = System.Math.Sqrt(2.0);

        Assert.Equal(2, final.Dimension);
        Assert.Equal(end, final.X, 12);
        Assert.InRange(final.GetValue(0), -2e-8, 2e-8);
        Assert.InRange(final.GetValue(1), -2e-8, 2e-8);
        Assert.InRange(final.GetFirstDerivative(0), -expectedSpeed - 2e-8, -expectedSpeed + 2e-8);
        Assert.InRange(final.GetFirstDerivative(1), expectedSpeed - 2e-8, expectedSpeed + 2e-8);
    }

    [Fact]
    public void RungeKuttaSecondOrderSystem_ValidatesDimensionsAndFiniteAccelerations()
    {
        Assert.Throws<ArgumentException>(() => RungeKutta.FourthOrderSecondOrderSystem(
            (_, _, _) => [0.0, 0.0],
            x0: 0.0,
            initialValues: [0.0, 0.0],
            initialFirstDerivatives: [0.0],
            xEnd: 1.0,
            step: 0.1));

        Assert.Throws<ArgumentException>(() => RungeKutta.FourthOrderSecondOrderSystem(
            (_, _, _) => [0.0],
            x0: 0.0,
            initialValues: [0.0, 0.0],
            initialFirstDerivatives: [0.0, 0.0],
            xEnd: 1.0,
            step: 0.1));

        Assert.Throws<ArithmeticException>(() => RungeKutta.FourthOrderSecondOrderSystem(
            (_, _, _) => [double.NaN, 0.0],
            x0: 0.0,
            initialValues: [0.0, 0.0],
            initialFirstDerivatives: [0.0, 0.0],
            xEnd: 1.0,
            step: 0.1));
    }

    [Fact]
    public void SecondOrderSystemOdePoint_CopiesInputVectors()
    {
        var values = new[] { 1.0, 2.0 };
        var derivatives = new[] { 3.0, 4.0 };
        var point = new SecondOrderSystemOdePoint(0.5, values, derivatives);

        values[0] = 99.0;
        derivatives[1] = 99.0;

        Assert.Equal(1.0, point.GetValue(0), 12);
        Assert.Equal(4.0, point.GetFirstDerivative(1), 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => point.GetValue(2));
    }
}
