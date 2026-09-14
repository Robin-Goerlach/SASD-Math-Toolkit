using Sasd.Numerics.Common;

namespace Sasd.Numerics.Integration;

/// <summary>
/// Classical one-dimensional numerical integration algorithms.
/// </summary>
public static class NumericalIntegration
{
    private static readonly double[] Gauss5Nodes =
    [
        -0.9061798459386640,
        -0.5384693101056831,
         0.0,
         0.5384693101056831,
         0.9061798459386640
    ];

    private static readonly double[] Gauss5Weights =
    [
        0.2369268850561891,
        0.4786286704993665,
        0.5688888888888889,
        0.4786286704993665,
        0.2369268850561891
    ];

    public static double CompositeTrapezoid(Func<double, double> function, double a, double b, int intervals)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(intervals, nameof(intervals));
        ValidateInterval(a, b);

        var h = (b - a) / intervals;
        var sum = 0.5 * (function(a) + function(b));
        for (var i = 1; i < intervals; i++)
        {
            sum += function(a + (i * h));
        }

        return h * sum;
    }

    public static double CompositeSimpson(Func<double, double> function, double a, double b, int intervals)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(intervals, nameof(intervals));
        ValidateInterval(a, b);

        if ((intervals & 1) != 0)
        {
            throw new ArgumentException("Simpson's composite rule requires an even number of intervals.", nameof(intervals));
        }

        var h = (b - a) / intervals;
        var sum = function(a) + function(b);
        for (var i = 1; i < intervals; i++)
        {
            sum += (i & 1) == 0
                ? 2.0 * function(a + (i * h))
                : 4.0 * function(a + (i * h));
        }

        return (h / 3.0) * sum;
    }

    public static double AdaptiveSimpson(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumDepth = 20)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumDepth, nameof(maximumDepth));
        ValidateInterval(a, b);

        var fa = function(a);
        var fb = function(b);
        var c = (a + b) / 2.0;
        var fc = function(c);
        var whole = SimpsonEstimate(a, b, fa, fb, fc);

        return AdaptiveSimpsonCore(function, a, b, fa, fb, fc, whole, tolerance, maximumDepth);
    }

    public static double Romberg(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumLevels = 12)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumLevels, nameof(maximumLevels));
        ValidateInterval(a, b);

        var previous = new double[maximumLevels];
        var current = new double[maximumLevels];
        previous[0] = 0.5 * (b - a) * (function(a) + function(b));

        for (var level = 1; level < maximumLevels; level++)
        {
            var intervals = 1 << level;
            var h = (b - a) / intervals;
            var newSamples = 0.0;
            for (var k = 1; k < intervals; k += 2)
            {
                newSamples += function(a + (k * h));
            }

            current[0] = 0.5 * previous[0] + (h * newSamples);
            var factor = 4.0;
            for (var j = 1; j <= level; j++)
            {
                current[j] = current[j - 1] + ((current[j - 1] - previous[j - 1]) / (factor - 1.0));
                factor *= 4.0;
            }

            if (System.Math.Abs(current[level] - previous[level - 1]) <= tolerance)
            {
                return current[level];
            }

            (previous, current) = (current, previous);
        }

        return previous[maximumLevels - 1];
    }

    public static double GaussLegendre5(Func<double, double> function, double a, double b)
    {
        ArgumentNullException.ThrowIfNull(function);
        ValidateInterval(a, b);

        var midpoint = (a + b) / 2.0;
        var half = (b - a) / 2.0;
        var sum = 0.0;

        for (var i = 0; i < Gauss5Nodes.Length; i++)
        {
            sum += Gauss5Weights[i] * function(midpoint + (half * Gauss5Nodes[i]));
        }

        return half * sum;
    }

    public static double AdaptiveGaussLegendre5(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumDepth = 16)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumDepth, nameof(maximumDepth));
        ValidateInterval(a, b);

        return AdaptiveGaussCore(function, a, b, tolerance, maximumDepth);
    }

    private static double AdaptiveSimpsonCore(
        Func<double, double> function,
        double a,
        double b,
        double fa,
        double fb,
        double fc,
        double whole,
        double tolerance,
        int depth)
    {
        var c = (a + b) / 2.0;
        var leftMid = (a + c) / 2.0;
        var rightMid = (c + b) / 2.0;
        var fLeftMid = function(leftMid);
        var fRightMid = function(rightMid);
        var left = SimpsonEstimate(a, c, fa, fc, fLeftMid);
        var right = SimpsonEstimate(c, b, fc, fb, fRightMid);
        var delta = left + right - whole;

        if (depth <= 0 || System.Math.Abs(delta) <= 15.0 * tolerance)
        {
            return left + right + (delta / 15.0);
        }

        return AdaptiveSimpsonCore(function, a, c, fa, fc, fLeftMid, left, tolerance / 2.0, depth - 1)
            + AdaptiveSimpsonCore(function, c, b, fc, fb, fRightMid, right, tolerance / 2.0, depth - 1);
    }

    private static double AdaptiveGaussCore(
        Func<double, double> function,
        double a,
        double b,
        double tolerance,
        int depth)
    {
        var whole = GaussLegendre5(function, a, b);
        var midpoint = (a + b) / 2.0;
        var split = GaussLegendre5(function, a, midpoint) + GaussLegendre5(function, midpoint, b);

        if (depth <= 0 || System.Math.Abs(split - whole) <= tolerance)
        {
            return split;
        }

        return AdaptiveGaussCore(function, a, midpoint, tolerance / 2.0, depth - 1)
            + AdaptiveGaussCore(function, midpoint, b, tolerance / 2.0, depth - 1);
    }

    private static double SimpsonEstimate(double a, double b, double fa, double fb, double fm)
        => (b - a) * (fa + (4.0 * fm) + fb) / 6.0;

    private static void ValidateInterval(double a, double b)
    {
        NumericGuard.Finite(a, nameof(a));
        NumericGuard.Finite(b, nameof(b));
        if (a >= b)
        {
            throw new ArgumentException("Integration requires a < b.");
        }
    }
}
