using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Fixed-step fourth-order Adams-Bashforth/Adams-Moulton predictor-corrector solver
/// for scalar first-order initial-value problems.
/// </summary>
/// <remarks>
/// The method uses three RK4 startup steps to create the four-point derivative
/// history required by the four-step Adams-Bashforth predictor. Each subsequent
/// prediction is corrected with the fourth-order Adams-Moulton formula.
/// </remarks>
public static class AdamsBashforthMoulton
{
    /// <summary>
    /// Integrates <c>y' = f(x, y)</c> from <paramref name="x0"/> to
    /// <paramref name="xEnd"/> with the classical AB4/AM4 predictor-corrector pair.
    /// </summary>
    /// <param name="derivative">Derivative function <c>f(x, y)</c>.</param>
    /// <param name="x0">Initial independent-variable value.</param>
    /// <param name="y0">Initial dependent-variable value.</param>
    /// <param name="xEnd">End point; it must be greater than <paramref name="x0"/>.</param>
    /// <param name="maximumStep">
    /// Maximum requested uniform step spacing. The solver chooses an integer number
    /// of equal steps no larger than this value so that the grid ends exactly at
    /// <paramref name="xEnd"/>.
    /// </param>
    /// <param name="correctorIterations">
    /// Number of Adams-Moulton fixed-point correction passes per predicted point.
    /// One pass gives the classical predictor-corrector workflow.
    /// </param>
    /// <returns>The initial point followed by every point on the uniform integration grid.</returns>
    public static IReadOnlyList<OdePoint> Integrate(
        Func<double, double, double> derivative,
        double x0,
        double y0,
        double xEnd,
        double maximumStep,
        int correctorIterations = 1)
    {
        ArgumentNullException.ThrowIfNull(derivative);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(y0, nameof(y0));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Positive(maximumStep, nameof(maximumStep));
        NumericGuard.Positive(correctorIterations, nameof(correctorIterations));

        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        var interval = xEnd - x0;
        var rawStepCount = System.Math.Ceiling(interval / maximumStep);
        if (!double.IsFinite(rawStepCount) || rawStepCount > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumStep),
                "The requested spacing would require more integration steps than this reference implementation can index.");
        }

        var stepCount = System.Math.Max(1, (int)rawStepCount);
        var h = interval / stepCount;
        var points = new List<OdePoint>(stepCount + 1) { new(x0, y0) };
        var derivatives = new double[stepCount + 1];
        derivatives[0] = EvaluateDerivative(derivative, x0, y0);

        // AB4 needs f_n, f_(n-1), f_(n-2), and f_(n-3). Generate the first
        // three solution points with the already established RK4 implementation.
        // If the requested interval is shorter than four steps, RK4 alone is the
        // mathematically appropriate startup path and there is not yet enough
        // history to apply the multistep formula.
        var startupSteps = System.Math.Min(3, stepCount);
        for (var index = 0; index < startupSteps; index++)
        {
            var current = points[^1];
            var nextX = index + 1 == stepCount ? xEnd : x0 + ((index + 1) * h);
            var nextY = RungeKutta.FourthOrderSingleStep(derivative, current.X, current.Y, h);
            EnsureFinite(nextY, "RK4 startup produced a non-finite solution value.");

            points.Add(new OdePoint(nextX, nextY));
            derivatives[index + 1] = EvaluateDerivative(derivative, nextX, nextY);
        }

        for (var n = 3; n < stepCount; n++)
        {
            var yn = points[n].Y;
            var nextX = n + 1 == stepCount ? xEnd : x0 + ((n + 1) * h);

            // Explicit four-step Adams-Bashforth prediction:
            // y_(n+1)^p = y_n + h/24 * (55 f_n - 59 f_(n-1)
            //                           + 37 f_(n-2) - 9 f_(n-3)).
            var predicted = yn + ((h / 24.0) * (
                (55.0 * derivatives[n])
                - (59.0 * derivatives[n - 1])
                + (37.0 * derivatives[n - 2])
                - (9.0 * derivatives[n - 3])));
            EnsureFinite(predicted, "Adams-Bashforth prediction produced a non-finite solution value.");

            var corrected = predicted;
            for (var iteration = 0; iteration < correctorIterations; iteration++)
            {
                var predictedDerivative = EvaluateDerivative(derivative, nextX, corrected);

                // Implicit fourth-order Adams-Moulton corrector. Repeated passes use
                // the previous corrected value as the fixed-point estimate for
                // f_(n+1); one pass is the conventional PECE-style correction.
                corrected = yn + ((h / 24.0) * (
                    (9.0 * predictedDerivative)
                    + (19.0 * derivatives[n])
                    - (5.0 * derivatives[n - 1])
                    + derivatives[n - 2]));
                EnsureFinite(corrected, "Adams-Moulton correction produced a non-finite solution value.");
            }

            points.Add(new OdePoint(nextX, corrected));
            derivatives[n + 1] = EvaluateDerivative(derivative, nextX, corrected);
        }

        return points;
    }

    private static double EvaluateDerivative(
        Func<double, double, double> derivative,
        double x,
        double y)
    {
        var value = derivative(x, y);
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("The derivative returned a non-finite value during Adams integration.");
        }

        return value;
    }

    private static void EnsureFinite(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
