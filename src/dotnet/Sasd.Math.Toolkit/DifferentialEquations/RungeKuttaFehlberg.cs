using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Adaptive Runge-Kutta-Fehlberg 4(5) solver for scalar first-order initial-value problems.
/// </summary>
/// <remarks>
/// The method evaluates the classical Fehlberg embedded fourth- and fifth-order
/// formulas with the same six derivative stages. Their difference estimates the
/// local truncation error and controls the next step size. The fifth-order estimate
/// is retained for accepted steps.
/// </remarks>
public static class RungeKuttaFehlberg
{
    /// <summary>
    /// Integrates <c>y' = f(x, y)</c> from an initial condition to a larger end point
    /// using adaptive RKF45 steps.
    /// </summary>
    /// <param name="derivative">Derivative function <c>f(x, y)</c>.</param>
    /// <param name="x0">Initial independent-variable value.</param>
    /// <param name="y0">Initial dependent-variable value.</param>
    /// <param name="xEnd">Requested end point; it must be greater than <paramref name="x0"/>.</param>
    /// <param name="options">Optional tolerance and step-size configuration.</param>
    /// <returns>An adaptive integration result containing accepted solution points and diagnostics.</returns>
    public static AdaptiveOdeResult Integrate(
        Func<double, double, double> derivative,
        double x0,
        double y0,
        double xEnd,
        RungeKuttaFehlbergOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(derivative);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(y0, nameof(y0));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        options ??= new RungeKuttaFehlbergOptions();
        options.Validate();

        var points = new List<OdePoint> { new(x0, y0) };
        var x = x0;
        var y = y0;
        var step = System.Math.Min(options.InitialStep, xEnd - x0);
        var acceptedSteps = 0;
        var rejectedSteps = 0;

        while (x < xEnd && acceptedSteps + rejectedSteps < options.MaximumStepAttempts)
        {
            var remaining = xEnd - x;
            var h = System.Math.Min(step, remaining);

            // At very large x values, adding a tiny h can round back to x. Reporting
            // this explicitly is safer than looping indefinitely without progress.
            if (x + h == x)
            {
                return new AdaptiveOdeResult(
                    points,
                    acceptedSteps,
                    rejectedSteps,
                    AdaptiveOdeStatus.NumericalBreakdown,
                    "Step size is too small to advance the independent variable at the current magnitude.");
            }

            if (!TryFehlbergStep(derivative, x, y, h, out var fourthOrder, out var fifthOrder, out var errorEstimate))
            {
                return new AdaptiveOdeResult(
                    points,
                    acceptedSteps,
                    rejectedSteps,
                    AdaptiveOdeStatus.NumericalBreakdown,
                    "The derivative or an RKF45 intermediate value became non-finite.");
            }

            // Combining absolute and relative tolerances is more useful than a single
            // absolute threshold: values near zero still have an absolute floor, while
            // larger solution magnitudes receive proportional error allowance.
            var tolerance = options.AbsoluteTolerance
                + (options.RelativeTolerance * System.Math.Max(System.Math.Abs(y), System.Math.Abs(fifthOrder)));
            var scaleFactor = ComputeScaleFactor(errorEstimate, tolerance, options);

            if (errorEstimate <= tolerance)
            {
                y = fifthOrder;
                x = h == remaining ? xEnd : x + h;
                acceptedSteps++;
                points.Add(new OdePoint(x, y));

                step = System.Math.Clamp(
                    h * scaleFactor,
                    options.MinimumStep,
                    options.MaximumStep);
                continue;
            }

            rejectedSteps++;
            var proposedStep = h * scaleFactor;
            var smallestUsableStep = System.Math.Min(options.MinimumStep, remaining);

            // If even the smallest permitted trial step was rejected, silently taking
            // a still smaller step would violate the caller's configuration. Return
            // the partial trajectory and a diagnostic status instead.
            if (h <= smallestUsableStep * (1.0 + 1e-12) && proposedStep < h)
            {
                return new AdaptiveOdeResult(
                    points,
                    acceptedSteps,
                    rejectedSteps,
                    AdaptiveOdeStatus.MinimumStepSizeReached,
                    "The requested local-error tolerance cannot be met at the configured minimum step size.");
            }

            step = System.Math.Clamp(
                proposedStep,
                options.MinimumStep,
                options.MaximumStep);
        }

        if (x >= xEnd)
        {
            return new AdaptiveOdeResult(
                points,
                acceptedSteps,
                rejectedSteps,
                AdaptiveOdeStatus.Completed);
        }

        return new AdaptiveOdeResult(
            points,
            acceptedSteps,
            rejectedSteps,
            AdaptiveOdeStatus.MaximumStepAttemptsReached,
            "Maximum number of adaptive step attempts reached before the requested end point.");
    }

    private static bool TryFehlbergStep(
        Func<double, double, double> derivative,
        double x,
        double y,
        double h,
        out double fourthOrder,
        out double fifthOrder,
        out double errorEstimate)
    {
        fourthOrder = double.NaN;
        fifthOrder = double.NaN;
        errorEstimate = double.NaN;

        if (!TryIncrement(derivative, x, y, h, out var k1)) return false;
        if (!TryIncrement(derivative, x + (h / 4.0), y + (k1 / 4.0), h, out var k2)) return false;
        if (!TryIncrement(
                derivative,
                x + (3.0 * h / 8.0),
                y + (3.0 * k1 / 32.0) + (9.0 * k2 / 32.0),
                h,
                out var k3)) return false;
        if (!TryIncrement(
                derivative,
                x + (12.0 * h / 13.0),
                y + (1932.0 * k1 / 2197.0) - (7200.0 * k2 / 2197.0) + (7296.0 * k3 / 2197.0),
                h,
                out var k4)) return false;
        if (!TryIncrement(
                derivative,
                x + h,
                y + (439.0 * k1 / 216.0) - (8.0 * k2) + (3680.0 * k3 / 513.0) - (845.0 * k4 / 4104.0),
                h,
                out var k5)) return false;
        if (!TryIncrement(
                derivative,
                x + (h / 2.0),
                y - (8.0 * k1 / 27.0) + (2.0 * k2) - (3544.0 * k3 / 2565.0)
                    + (1859.0 * k4 / 4104.0) - (11.0 * k5 / 40.0),
                h,
                out var k6)) return false;

        fourthOrder = y
            + (25.0 * k1 / 216.0)
            + (1408.0 * k3 / 2565.0)
            + (2197.0 * k4 / 4104.0)
            - (k5 / 5.0);

        fifthOrder = y
            + (16.0 * k1 / 135.0)
            + (6656.0 * k3 / 12825.0)
            + (28561.0 * k4 / 56430.0)
            - (9.0 * k5 / 50.0)
            + (2.0 * k6 / 55.0);

        errorEstimate = System.Math.Abs(fifthOrder - fourthOrder);
        return double.IsFinite(fourthOrder)
            && double.IsFinite(fifthOrder)
            && double.IsFinite(errorEstimate);
    }

    private static bool TryIncrement(
        Func<double, double, double> derivative,
        double x,
        double y,
        double h,
        out double increment)
    {
        increment = double.NaN;
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            return false;
        }

        var value = derivative(x, y);
        if (!double.IsFinite(value))
        {
            return false;
        }

        increment = h * value;
        return double.IsFinite(increment);
    }

    private static double ComputeScaleFactor(
        double errorEstimate,
        double tolerance,
        RungeKuttaFehlbergOptions options)
    {
        if (errorEstimate <= double.Epsilon)
        {
            return options.MaximumScaleFactor;
        }

        // The embedded 4(5) error estimate varies approximately with h^5. Taking
        // the fifth root therefore predicts the step-size change needed to meet the
        // requested local tolerance. The clamp avoids violent step-size oscillations.
        var raw = options.SafetyFactor * System.Math.Pow(tolerance / errorEstimate, 0.2);
        return System.Math.Clamp(raw, options.MinimumScaleFactor, options.MaximumScaleFactor);
    }
}
