using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

public sealed record OdePoint(double X, double Y);

/// <summary>
/// Initial-value solvers for ordinary differential equations.
/// </summary>
public static class RungeKutta
{
    public static IReadOnlyList<OdePoint> FourthOrder(
        Func<double, double, double> derivative,
        double x0,
        double y0,
        double xEnd,
        double step)
    {
        ArgumentNullException.ThrowIfNull(derivative);
        NumericGuard.Positive(step, nameof(step));
        if (xEnd <= x0) throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));

        var points = new List<OdePoint> { new(x0, y0) };
        var x = x0;
        var y = y0;

        while (x < xEnd)
        {
            var h = System.Math.Min(step, xEnd - x);
            y = FourthOrderSingleStep(derivative, x, y, h);
            x += h;
            points.Add(new OdePoint(x, y));
        }

        return points;
    }

    public static IReadOnlyList<(double X, double[] Y)> FourthOrderSystem(
        Func<double, IReadOnlyList<double>, double[]> derivative,
        double x0,
        IReadOnlyList<double> y0,
        double xEnd,
        double step)
    {
        ArgumentNullException.ThrowIfNull(derivative);
        ArgumentNullException.ThrowIfNull(y0);
        NumericGuard.Positive(step, nameof(step));
        if (y0.Count == 0) throw new ArgumentException("Initial state must not be empty.", nameof(y0));
        if (xEnd <= x0) throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));

        var y = y0.ToArray();
        var points = new List<(double X, double[] Y)> { (x0, y.ToArray()) };
        var x = x0;

        while (x < xEnd)
        {
            var h = System.Math.Min(step, xEnd - x);
            var k1 = derivative(x, y);
            var k2 = derivative(x + (h / 2.0), AddScaled(y, k1, h / 2.0));
            var k3 = derivative(x + (h / 2.0), AddScaled(y, k2, h / 2.0));
            var k4 = derivative(x + h, AddScaled(y, k3, h));
            ValidateDerivativeLength(y, k1, k2, k3, k4);

            for (var i = 0; i < y.Length; i++)
            {
                y[i] += (h / 6.0) * (k1[i] + (2.0 * k2[i]) + (2.0 * k3[i]) + k4[i]);
            }

            x += h;
            points.Add((x, y.ToArray()));
        }

        return points;
    }

    /// <summary>
    /// Performs one classical fourth-order Runge-Kutta step. This internal primitive
    /// is shared with multistep methods that require RK4 startup values.
    /// </summary>
    internal static double FourthOrderSingleStep(
        Func<double, double, double> derivative,
        double x,
        double y,
        double step)
    {
        var k1 = derivative(x, y);
        var k2 = derivative(x + (step / 2.0), y + (step * k1 / 2.0));
        var k3 = derivative(x + (step / 2.0), y + (step * k2 / 2.0));
        var k4 = derivative(x + step, y + (step * k3));
        return y + ((step / 6.0) * (k1 + (2.0 * k2) + (2.0 * k3) + k4));
    }

    private static double[] AddScaled(IReadOnlyList<double> y, IReadOnlyList<double> k, double scale)
    {
        if (y.Count != k.Count) throw new ArgumentException("Derivative dimension does not match state dimension.");
        var result = new double[y.Count];
        for (var i = 0; i < result.Length; i++) result[i] = y[i] + (scale * k[i]);
        return result;
    }

    private static void ValidateDerivativeLength(double[] y, params double[][] derivatives)
    {
        if (derivatives.Any(item => item.Length != y.Length))
        {
            throw new ArgumentException("Derivative dimension does not match state dimension.");
        }
    }
}
