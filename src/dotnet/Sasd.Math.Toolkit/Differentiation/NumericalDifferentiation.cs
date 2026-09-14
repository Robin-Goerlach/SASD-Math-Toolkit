using Sasd.Numerics.Common;

namespace Sasd.Numerics.Differentiation;

/// <summary>
/// Finite-difference differentiation routines with Richardson refinement.
/// </summary>
public static class NumericalDifferentiation
{
    public static double FirstDerivative(Func<double, double> function, double x, double step = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(step, nameof(step));

        var coarse = (function(x + step) - function(x - step)) / (2.0 * step);
        var half = step / 2.0;
        var fine = (function(x + half) - function(x - half)) / (2.0 * half);

        return fine + ((fine - coarse) / 3.0);
    }

    public static double SecondDerivative(Func<double, double> function, double x, double step = 1e-4)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(step, nameof(step));

        var fx = function(x);
        var coarse = (function(x + step) - (2.0 * fx) + function(x - step)) / (step * step);
        var half = step / 2.0;
        var fine = (function(x + half) - (2.0 * fx) + function(x - half)) / (half * half);

        return fine + ((fine - coarse) / 3.0);
    }

    public static double FirstDerivativeFivePoint(Func<double, double> function, double x, double step = 1e-4)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(step, nameof(step));

        return (function(x - (2.0 * step))
            - (8.0 * function(x - step))
            + (8.0 * function(x + step))
            - function(x + (2.0 * step))) / (12.0 * step);
    }

    public static double SecondDerivativeFivePoint(Func<double, double> function, double x, double step = 1e-3)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(step, nameof(step));

        return (-function(x + (2.0 * step))
            + (16.0 * function(x + step))
            - (30.0 * function(x))
            + (16.0 * function(x - step))
            - function(x - (2.0 * step))) / (12.0 * step * step);
    }
}
