using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Scalar root-finding algorithms implemented independently from historical source code.
/// </summary>
/// <remarks>
/// Invalid programming input is reported through exceptions. Expected iterative outcomes
/// such as a missing bracket, a nearly singular step or an exhausted iteration budget are
/// represented by <see cref="RootResult.Status"/> so callers can distinguish numerical
/// behavior from malformed input.
/// </remarks>
public static class RootSolvers
{
    /// <summary>
    /// Finds a real root inside an interval whose endpoint function values have opposite signs.
    /// </summary>
    /// <param name="function">Scalar function whose zero is sought.</param>
    /// <param name="left">Finite left endpoint of the initial bracket.</param>
    /// <param name="right">Finite right endpoint of the initial bracket.</param>
    /// <param name="options">Optional convergence settings.</param>
    /// <returns>
    /// A result containing the best estimate and termination status. If the endpoints do not
    /// bracket a sign change, the result status is <see cref="IterationStatus.NotBracketed"/>.
    /// </returns>
    /// <remarks>
    /// The method keeps a valid sign-changing bracket throughout the iteration. This makes it
    /// a useful robust reference method when a bracket is known, although its linear
    /// convergence is normally slower than a well-started Newton iteration.
    /// </remarks>
    public static RootResult Bisection(
        Func<double, double> function,
        double left,
        double right,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(function);
        options ??= new RootFindingOptions();
        options.Validate();
        NumericGuard.Finite(left, nameof(left));
        NumericGuard.Finite(right, nameof(right));

        if (left >= right)
        {
            throw new ArgumentException("The left endpoint must be smaller than the right endpoint.");
        }

        var fLeft = function(left);
        var fRight = function(right);
        EnsureFiniteFunctionValue(fLeft);
        EnsureFiniteFunctionValue(fRight);

        if (System.Math.Abs(fLeft) <= options.Tolerance)
        {
            return new RootResult(left, fLeft, 0, IterationStatus.Converged);
        }

        if (System.Math.Abs(fRight) <= options.Tolerance)
        {
            return new RootResult(right, fRight, 0, IterationStatus.Converged);
        }

        if (System.Math.Sign(fLeft) == System.Math.Sign(fRight))
        {
            return new RootResult(double.NaN, double.NaN, 0, IterationStatus.NotBracketed,
                "Function values at the interval endpoints must have opposite signs.");
        }

        var previous = left;
        var midpoint = left;
        var fMid = fLeft;

        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            midpoint = left + ((right - left) / 2.0);
            fMid = function(midpoint);
            EnsureFiniteFunctionValue(fMid);

            if (HasConverged(midpoint, previous, fMid, options.Tolerance))
            {
                return new RootResult(midpoint, fMid, iteration, IterationStatus.Converged);
            }

            if (System.Math.Sign(fLeft) != System.Math.Sign(fMid))
            {
                right = midpoint;
                fRight = fMid;
            }
            else
            {
                left = midpoint;
                fLeft = fMid;
            }

            previous = midpoint;
        }

        return new RootResult(midpoint, fMid, options.MaximumIterations,
            IterationStatus.MaximumIterationsReached, "Maximum number of iterations reached.");
    }

    /// <summary>
    /// Finds a real root with Newton-Raphson iteration from one initial guess.
    /// </summary>
    /// <param name="function">Scalar function whose zero is sought.</param>
    /// <param name="derivative">Derivative of <paramref name="function"/>.</param>
    /// <param name="initialGuess">Finite starting estimate.</param>
    /// <param name="options">Optional convergence settings.</param>
    /// <returns>A result containing the best estimate and termination status.</returns>
    /// <remarks>
    /// Newton-Raphson is usually fast near a simple root, but it is a local method. If the
    /// derivative is too close to zero for a stable step, the method returns
    /// <see cref="IterationStatus.NumericalBreakdown"/> rather than performing the division.
    /// </remarks>
    public static RootResult NewtonRaphson(
        Func<double, double> function,
        Func<double, double> derivative,
        double initialGuess,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(derivative);
        options ??= new RootFindingOptions();
        options.Validate();
        NumericGuard.Finite(initialGuess, nameof(initialGuess));

        var x = initialGuess;
        var fx = function(x);
        EnsureFiniteFunctionValue(fx);

        if (System.Math.Abs(fx) <= options.Tolerance)
        {
            return new RootResult(x, fx, 0, IterationStatus.Converged);
        }

        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            var dfx = derivative(x);
            EnsureFiniteFunctionValue(dfx);

            if (System.Math.Abs(dfx) <= NumericConstants.NearlyZero)
            {
                return new RootResult(x, fx, iteration - 1, IterationStatus.NumericalBreakdown,
                    "Derivative is too close to zero for a stable Newton step.");
            }

            var next = x - (fx / dfx);
            EnsureFiniteIterate(next, "Newton-Raphson");

            var fNext = function(next);
            EnsureFiniteFunctionValue(fNext);

            if (HasConverged(next, x, fNext, options.Tolerance))
            {
                return new RootResult(next, fNext, iteration, IterationStatus.Converged);
            }

            x = next;
            fx = fNext;
        }

        return new RootResult(x, fx, options.MaximumIterations,
            IterationStatus.MaximumIterationsReached, "Maximum number of iterations reached.");
    }

    /// <summary>
    /// Finds a real root with the derivative-free secant iteration.
    /// </summary>
    /// <param name="function">Scalar function whose zero is sought.</param>
    /// <param name="firstGuess">First finite starting estimate.</param>
    /// <param name="secondGuess">Second finite starting estimate.</param>
    /// <param name="options">Optional convergence settings.</param>
    /// <returns>A result containing the best estimate and termination status.</returns>
    /// <remarks>
    /// The secant method estimates a local slope from two successive function values. It
    /// does not preserve a sign-changing bracket. When that slope becomes numerically flat,
    /// the method reports <see cref="IterationStatus.NumericalBreakdown"/>.
    /// </remarks>
    public static RootResult Secant(
        Func<double, double> function,
        double firstGuess,
        double secondGuess,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(function);
        options ??= new RootFindingOptions();
        options.Validate();
        NumericGuard.Finite(firstGuess, nameof(firstGuess));
        NumericGuard.Finite(secondGuess, nameof(secondGuess));

        var x0 = firstGuess;
        var x1 = secondGuess;
        var f0 = function(x0);
        var f1 = function(x1);
        EnsureFiniteFunctionValue(f0);
        EnsureFiniteFunctionValue(f1);

        if (System.Math.Abs(f0) <= options.Tolerance)
        {
            return new RootResult(x0, f0, 0, IterationStatus.Converged);
        }

        if (System.Math.Abs(f1) <= options.Tolerance)
        {
            return new RootResult(x1, f1, 0, IterationStatus.Converged);
        }

        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            var denominator = f1 - f0;
            if (System.Math.Abs(denominator) <= NumericConstants.NearlyZero)
            {
                return new RootResult(x1, f1, iteration - 1, IterationStatus.NumericalBreakdown,
                    "Secant slope is too close to zero.");
            }

            var x2 = x1 - (f1 * (x1 - x0) / denominator);
            EnsureFiniteIterate(x2, "Secant");

            var f2 = function(x2);
            EnsureFiniteFunctionValue(f2);

            if (HasConverged(x2, x1, f2, options.Tolerance))
            {
                return new RootResult(x2, f2, iteration, IterationStatus.Converged);
            }

            x0 = x1;
            f0 = f1;
            x1 = x2;
            f1 = f2;
        }

        return new RootResult(x1, f1, options.MaximumIterations,
            IterationStatus.MaximumIterationsReached, "Maximum number of iterations reached.");
    }

    private static bool HasConverged(double current, double previous, double functionValue, double tolerance)
    {
        if (System.Math.Abs(functionValue) <= tolerance)
        {
            return true;
        }

        var scale = System.Math.Max(1.0, System.Math.Abs(previous));
        return System.Math.Abs(current - previous) <= tolerance * scale;
    }

    private static void EnsureFiniteFunctionValue(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("The supplied function or derivative returned a non-finite value.");
        }
    }

    private static void EnsureFiniteIterate(double value, string algorithmName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException($"{algorithmName} produced a non-finite iterate.");
        }
    }
}
