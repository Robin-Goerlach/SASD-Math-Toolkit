using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

public sealed record OdePoint(double X, double Y);

/// <summary>
/// Initial-value solvers based on the classical fourth-order Runge-Kutta method.
/// </summary>
public static class RungeKutta
{
    /// <summary>
    /// Solves a scalar first-order initial-value problem <c>y' = f(x, y)</c>.
    /// </summary>
    public static IReadOnlyList<OdePoint> FourthOrder(
        Func<double, double, double> derivative,
        double x0,
        double y0,
        double xEnd,
        double step)
    {
        ArgumentNullException.ThrowIfNull(derivative);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(y0, nameof(y0));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Positive(step, nameof(step));
        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        var points = new List<OdePoint> { new(x0, y0) };
        var x = x0;
        var y = y0;

        while (x < xEnd)
        {
            var remaining = xEnd - x;
            var h = System.Math.Min(step, remaining);
            EnsureStepAdvances(x, h);

            y = FourthOrderSingleStep(derivative, x, y, h);
            x = h == remaining ? xEnd : x + h;
            points.Add(new OdePoint(x, y));
        }

        return points;
    }

    /// <summary>
    /// Solves a scalar second-order equation <c>y'' = g(x, y, y')</c> by rewriting
    /// it as a two-component first-order system and applying the same RK4 core.
    /// </summary>
    /// <param name="secondDerivative">Function returning <c>y''</c> for <c>(x, y, y')</c>.</param>
    /// <param name="x0">Initial independent-variable value.</param>
    /// <param name="y0">Initial value <c>y(x0)</c>.</param>
    /// <param name="firstDerivative0">Initial derivative <c>y'(x0)</c>.</param>
    /// <param name="xEnd">Requested end point; it must be greater than <paramref name="x0"/>.</param>
    /// <param name="step">Nominal positive RK4 step size. The last step is shortened when required.</param>
    /// <returns>Points containing <c>x</c>, <c>y</c>, and <c>y'</c> along the computed trajectory.</returns>
    /// <remarks>
    /// Mathematically the method introduces <c>v = y'</c>, so the equation becomes
    /// <c>y' = v</c> and <c>v' = g(x, y, v)</c>. Keeping the transformation in this
    /// convenience API avoids maintaining a second, subtly different RK4 algorithm.
    /// </remarks>
    public static IReadOnlyList<SecondOrderOdePoint> FourthOrderSecondOrder(
        Func<double, double, double, double> secondDerivative,
        double x0,
        double y0,
        double firstDerivative0,
        double xEnd,
        double step)
    {
        ArgumentNullException.ThrowIfNull(secondDerivative);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(y0, nameof(y0));
        NumericGuard.Finite(firstDerivative0, nameof(firstDerivative0));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Positive(step, nameof(step));
        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        var systemPoints = FourthOrderSystem(
            (x, state) =>
            [
                state[1],
                secondDerivative(x, state[0], state[1])
            ],
            x0,
            [y0, firstDerivative0],
            xEnd,
            step);

        var result = new List<SecondOrderOdePoint>(systemPoints.Count);
        foreach (var point in systemPoints)
        {
            result.Add(new SecondOrderOdePoint(point.X, point.Y[0], point.Y[1]));
        }

        return result;
    }

    /// <summary>
    /// Solves a coupled first-order system <c>Y' = F(x, Y)</c> with classical RK4.
    /// </summary>
    public static IReadOnlyList<(double X, double[] Y)> FourthOrderSystem(
        Func<double, IReadOnlyList<double>, double[]> derivative,
        double x0,
        IReadOnlyList<double> y0,
        double xEnd,
        double step)
    {
        ArgumentNullException.ThrowIfNull(derivative);
        ArgumentNullException.ThrowIfNull(y0);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Positive(step, nameof(step));
        if (y0.Count == 0)
        {
            throw new ArgumentException("Initial state must not be empty.", nameof(y0));
        }

        ValidateFiniteInitialState(y0, nameof(y0));
        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        var y = y0.ToArray();
        var points = new List<(double X, double[] Y)> { (x0, y.ToArray()) };
        var x = x0;

        while (x < xEnd)
        {
            var remaining = xEnd - x;
            var h = System.Math.Min(step, remaining);
            EnsureStepAdvances(x, h);

            y = FourthOrderSystemSingleStep(derivative, x, y, h);
            x = h == remaining ? xEnd : x + h;
            points.Add((x, y.ToArray()));
        }

        return points;
    }

    /// <summary>
    /// Performs one classical scalar fourth-order Runge-Kutta step. This internal
    /// primitive is shared with multistep methods that require RK4 startup values.
    /// </summary>
    internal static double FourthOrderSingleStep(
        Func<double, double, double> derivative,
        double x,
        double y,
        double step)
    {
        var k1 = EvaluateScalarDerivative(derivative, x, y);
        var k2 = EvaluateScalarDerivative(derivative, x + (step / 2.0), y + (step * k1 / 2.0));
        var k3 = EvaluateScalarDerivative(derivative, x + (step / 2.0), y + (step * k2 / 2.0));
        var k4 = EvaluateScalarDerivative(derivative, x + step, y + (step * k3));
        var result = y + ((step / 6.0) * (k1 + (2.0 * k2) + (2.0 * k3) + k4));

        if (!double.IsFinite(result))
        {
            throw new ArithmeticException("RK4 produced a non-finite solution value.");
        }

        return result;
    }

    /// <summary>
    /// Performs one classical fourth-order Runge-Kutta step for a first-order system.
    /// Higher-order convenience APIs can therefore transform their equation to a
    /// system without duplicating the numerical integration formula.
    /// </summary>
    internal static double[] FourthOrderSystemSingleStep(
        Func<double, IReadOnlyList<double>, double[]> derivative,
        double x,
        IReadOnlyList<double> y,
        double step)
    {
        var k1 = EvaluateSystemDerivative(derivative, x, y, y.Count);
        var k2State = AddScaled(y, k1, step / 2.0);
        var k2 = EvaluateSystemDerivative(derivative, x + (step / 2.0), k2State, y.Count);
        var k3State = AddScaled(y, k2, step / 2.0);
        var k3 = EvaluateSystemDerivative(derivative, x + (step / 2.0), k3State, y.Count);
        var k4State = AddScaled(y, k3, step);
        var k4 = EvaluateSystemDerivative(derivative, x + step, k4State, y.Count);

        var result = new double[y.Count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = y[i] + ((step / 6.0) * (k1[i] + (2.0 * k2[i]) + (2.0 * k3[i]) + k4[i]));
            if (!double.IsFinite(result[i]))
            {
                throw new ArithmeticException("RK4 produced a non-finite system state.");
            }
        }

        return result;
    }

    private static double EvaluateScalarDerivative(
        Func<double, double, double> derivative,
        double x,
        double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw new ArithmeticException("RK4 encountered a non-finite scalar intermediate state.");
        }

        var value = derivative(x, y);
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("The derivative returned a non-finite value during RK4 integration.");
        }

        return value;
    }

    private static double[] EvaluateSystemDerivative(
        Func<double, IReadOnlyList<double>, double[]> derivative,
        double x,
        IReadOnlyList<double> state,
        int expectedLength)
    {
        if (!double.IsFinite(x))
        {
            throw new ArithmeticException("RK4 encountered a non-finite independent-variable value.");
        }

        EnsureFiniteRuntimeState(state, "RK4 encountered a non-finite intermediate system state.");
        var values = derivative(x, state)
            ?? throw new ArithmeticException("The system derivative returned null during RK4 integration.");

        if (values.Length != expectedLength)
        {
            throw new ArgumentException("Derivative dimension does not match state dimension.");
        }

        EnsureFiniteRuntimeState(values, "The system derivative returned a non-finite value during RK4 integration.");
        return values;
    }

    private static double[] AddScaled(IReadOnlyList<double> y, IReadOnlyList<double> k, double scale)
    {
        if (y.Count != k.Count)
        {
            throw new ArgumentException("Derivative dimension does not match state dimension.");
        }

        var result = new double[y.Count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = y[i] + (scale * k[i]);
            if (!double.IsFinite(result[i]))
            {
                throw new ArithmeticException("RK4 produced a non-finite intermediate system state.");
            }
        }

        return result;
    }

    private static void ValidateFiniteInitialState(IReadOnlyList<double> state, string parameterName)
    {
        for (var i = 0; i < state.Count; i++)
        {
            if (!double.IsFinite(state[i]))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Initial state values must be finite.");
            }
        }
    }

    private static void EnsureFiniteRuntimeState(IReadOnlyList<double> state, string message)
    {
        for (var i = 0; i < state.Count; i++)
        {
            if (!double.IsFinite(state[i]))
            {
                throw new ArithmeticException(message);
            }
        }
    }

    private static void EnsureStepAdvances(double x, double step)
    {
        if (x + step == x)
        {
            throw new ArithmeticException(
                "Step size is too small to advance the independent variable at the current magnitude.");
        }
    }
}
