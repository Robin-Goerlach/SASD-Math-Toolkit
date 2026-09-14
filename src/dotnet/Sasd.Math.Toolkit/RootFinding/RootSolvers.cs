using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Scalar root-finding algorithms implemented independently from historical source code.
/// </summary>
public static class RootSolvers
{
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
            throw new ArithmeticException("The supplied function returned a non-finite value.");
        }
    }
}
