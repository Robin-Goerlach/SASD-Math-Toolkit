using System.Numerics;
using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Root solvers that operate directly in the complex plane.
/// </summary>
public static class ComplexRootSolvers
{
    /// <summary>
    /// Finds a complex root with Muller's method using three starting points.
    /// </summary>
    /// <remarks>
    /// Muller's method fits a quadratic through the three most recent points and chooses
    /// one of its roots as the next step. The square root is intentionally complex, so a
    /// sequence that starts on the real axis can move naturally toward a complex root.
    /// </remarks>
    public static ComplexRootResult Muller(
        Func<Complex, Complex> function,
        Complex firstGuess,
        Complex secondGuess,
        Complex thirdGuess,
        RootFindingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(function);
        options ??= new RootFindingOptions();
        options.Validate();
        EnsureFinite(firstGuess, nameof(firstGuess));
        EnsureFinite(secondGuess, nameof(secondGuess));
        EnsureFinite(thirdGuess, nameof(thirdGuess));

        var x0 = firstGuess;
        var x1 = secondGuess;
        var x2 = thirdGuess;
        var f0 = Evaluate(function, x0);
        var f1 = Evaluate(function, x1);
        var f2 = Evaluate(function, x2);

        if (f0.Magnitude <= options.Tolerance)
        {
            return new ComplexRootResult(x0, f0, 0, IterationStatus.Converged);
        }

        if (f1.Magnitude <= options.Tolerance)
        {
            return new ComplexRootResult(x1, f1, 0, IterationStatus.Converged);
        }

        if (f2.Magnitude <= options.Tolerance)
        {
            return new ComplexRootResult(x2, f2, 0, IterationStatus.Converged);
        }

        for (var iteration = 1; iteration <= options.MaximumIterations; iteration++)
        {
            var h1 = x1 - x0;
            var h2 = x2 - x1;
            if (IsNearlyZero(h1) || IsNearlyZero(h2) || IsNearlyZero(h1 + h2))
            {
                return Breakdown(x2, f2, iteration - 1,
                    "Muller iteration requires three sufficiently distinct points.");
            }

            var delta1 = (f1 - f0) / h1;
            var delta2 = (f2 - f1) / h2;
            var quadraticCoefficient = (delta2 - delta1) / (h1 + h2);
            var linearCoefficient = delta2 + (h2 * quadraticCoefficient);
            var discriminant = Complex.Sqrt(
                (linearCoefficient * linearCoefficient) - (4.0 * f2 * quadraticCoefficient));

            // Choosing the larger denominator avoids subtractive cancellation and is one
            // of the important stability details of the practical Muller iteration.
            var plus = linearCoefficient + discriminant;
            var minus = linearCoefficient - discriminant;
            var denominator = plus.Magnitude >= minus.Magnitude ? plus : minus;

            if (IsNearlyZero(denominator))
            {
                return Breakdown(x2, f2, iteration - 1,
                    "Muller quadratic step became numerically singular.");
            }

            var step = (-2.0 * f2) / denominator;
            var next = x2 + step;
            EnsureFinite(next, nameof(function));
            var fNext = Evaluate(function, next);

            if (HasConverged(next, x2, fNext, options.Tolerance))
            {
                return new ComplexRootResult(next, fNext, iteration, IterationStatus.Converged);
            }

            x0 = x1;
            f0 = f1;
            x1 = x2;
            f1 = f2;
            x2 = next;
            f2 = fNext;
        }

        return new ComplexRootResult(
            x2,
            f2,
            options.MaximumIterations,
            IterationStatus.MaximumIterationsReached,
            "Maximum number of iterations reached.");
    }

    private static ComplexRootResult Breakdown(Complex root, Complex functionValue, int iterations, string message) =>
        new(root, functionValue, iterations, IterationStatus.NumericalBreakdown, message);

    private static Complex Evaluate(Func<Complex, Complex> function, Complex x)
    {
        var value = function(x);
        EnsureFinite(value, nameof(function));
        return value;
    }

    internal static bool HasConverged(Complex current, Complex previous, Complex functionValue, double tolerance)
    {
        if (functionValue.Magnitude <= tolerance)
        {
            return true;
        }

        var scale = System.Math.Max(1.0, previous.Magnitude);
        return (current - previous).Magnitude <= tolerance * scale;
    }

    internal static bool IsNearlyZero(Complex value) => value.Magnitude <= NumericConstants.NearlyZero;

    internal static void EnsureFinite(Complex value, string parameterName)
    {
        if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary))
        {
            throw new ArithmeticException($"{parameterName} produced or contained a non-finite complex value.");
        }
    }
}
