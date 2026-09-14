using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Linear shooting solver for scalar second-order Dirichlet boundary-value problems.
/// </summary>
/// <remarks>
/// The implemented equation form is
/// <c>y'' = p(x) y' + q(x) y + r(x)</c> with prescribed values at both interval ends.
/// The method solves one particular and one homogeneous initial-value problem with
/// the existing RK4 second-order solver, then combines them linearly so the right
/// boundary value is satisfied.
/// </remarks>
public static class LinearShooting
{
    /// <summary>
    /// Solves a linear second-order boundary-value problem by classical shooting.
    /// </summary>
    /// <param name="firstDerivativeCoefficient">Coefficient <c>p(x)</c> multiplying <c>y'</c>.</param>
    /// <param name="valueCoefficient">Coefficient <c>q(x)</c> multiplying <c>y</c>.</param>
    /// <param name="forcing">Forcing term <c>r(x)</c>.</param>
    /// <param name="x0">Left interval endpoint.</param>
    /// <param name="leftValue">Required value <c>y(x0)</c>.</param>
    /// <param name="xEnd">Right interval endpoint; it must be greater than <paramref name="x0"/>.</param>
    /// <param name="rightValue">Required value <c>y(xEnd)</c>.</param>
    /// <param name="step">Nominal positive RK4 step used for both auxiliary initial-value problems.</param>
    /// <param name="singularityTolerance">
    /// Absolute threshold for the right-end value of the homogeneous auxiliary
    /// solution. Values at or below this threshold are treated as a singular or
    /// numerically ill-conditioned boundary map.
    /// </param>
    /// <returns>The combined trajectory and useful shooting diagnostics.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the homogeneous auxiliary solution cannot provide a stable shooting correction.
    /// </exception>
    public static LinearShootingResult Solve(
        Func<double, double> firstDerivativeCoefficient,
        Func<double, double> valueCoefficient,
        Func<double, double> forcing,
        double x0,
        double leftValue,
        double xEnd,
        double rightValue,
        double step,
        double singularityTolerance = NumericConstants.DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(firstDerivativeCoefficient);
        ArgumentNullException.ThrowIfNull(valueCoefficient);
        ArgumentNullException.ThrowIfNull(forcing);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(leftValue, nameof(leftValue));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Finite(rightValue, nameof(rightValue));
        NumericGuard.Positive(step, nameof(step));
        NumericGuard.Positive(singularityTolerance, nameof(singularityTolerance));

        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        // A convenient particular solution starts at the prescribed left value with
        // zero initial slope. Any other finite slope would also work, but zero keeps
        // the decomposition easy to explain and makes the final correction directly
        // equal to the reconstructed physical initial slope.
        var particular = RungeKutta.FourthOrderSecondOrder(
            (x, y, firstDerivative) =>
                (EvaluateCoefficient(firstDerivativeCoefficient, x, "p") * firstDerivative)
                + (EvaluateCoefficient(valueCoefficient, x, "q") * y)
                + EvaluateCoefficient(forcing, x, "r"),
            x0,
            leftValue,
            firstDerivative0: 0.0,
            xEnd,
            step);

        // The homogeneous sensitivity solution measures how much the right boundary
        // changes when the unknown initial slope changes by one unit.
        var homogeneous = RungeKutta.FourthOrderSecondOrder(
            (x, y, firstDerivative) =>
                (EvaluateCoefficient(firstDerivativeCoefficient, x, "p") * firstDerivative)
                + (EvaluateCoefficient(valueCoefficient, x, "q") * y),
            x0,
            y0: 0.0,
            firstDerivative0: 1.0,
            xEnd,
            step);

        if (particular.Count != homogeneous.Count)
        {
            throw new InvalidOperationException("Internal shooting trajectories do not share the same RK4 grid.");
        }

        var auxiliaryRightValue = homogeneous[^1].Y;
        if (System.Math.Abs(auxiliaryRightValue) <= singularityTolerance)
        {
            throw new InvalidOperationException(
                "Linear shooting cannot determine a stable initial slope because the homogeneous auxiliary solution is too small at the right boundary.");
        }

        var correction = (rightValue - particular[^1].Y) / auxiliaryRightValue;
        if (!double.IsFinite(correction))
        {
            throw new ArithmeticException("Linear shooting produced a non-finite boundary correction.");
        }

        var combined = new List<SecondOrderOdePoint>(particular.Count);
        for (var i = 0; i < particular.Count; i++)
        {
            if (particular[i].X != homogeneous[i].X)
            {
                throw new InvalidOperationException("Internal shooting trajectories do not share identical RK4 abscissas.");
            }

            var y = particular[i].Y + (correction * homogeneous[i].Y);
            var firstDerivative = particular[i].FirstDerivative
                + (correction * homogeneous[i].FirstDerivative);

            if (!double.IsFinite(y) || !double.IsFinite(firstDerivative))
            {
                throw new ArithmeticException("Linear shooting produced a non-finite combined solution state.");
            }

            combined.Add(new SecondOrderOdePoint(particular[i].X, y, firstDerivative));
        }

        var residual = combined[^1].Y - rightValue;
        return new LinearShootingResult(
            combined,
            initialSlope: correction,
            rightBoundaryResidual: residual,
            auxiliaryRightValue: auxiliaryRightValue);
    }

    private static double EvaluateCoefficient(Func<double, double> coefficient, double x, string name)
    {
        var value = coefficient(x);
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException($"Linear shooting coefficient {name}(x) returned a non-finite value.");
        }

        return value;
    }
}
