using Sasd.Numerics.Common;

namespace Sasd.Numerics.Integration;

/// <summary>
/// Classical one-dimensional numerical integration algorithms.
/// </summary>
/// <remarks>
/// The implementation favors transparent reference algorithms and explicit diagnostics over
/// micro-optimization. Public methods reject non-finite bounds and non-finite integrand values.
/// Integration bounds must satisfy <c>a &lt; b</c>; callers that need the opposite orientation
/// should swap the bounds and negate the returned value explicitly.
/// </remarks>
public static class NumericalIntegration
{
    private const int MaximumSupportedRombergLevels = 30;

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

    /// <summary>
    /// Integrates a function with the composite trapezoid rule on equally sized panels.
    /// </summary>
    public static double CompositeTrapezoid(
        Func<double, double> function,
        double a,
        double b,
        int intervals)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(intervals, nameof(intervals));
        var width = ValidateInterval(a, b);
        var h = width / intervals;
        EnsureUsableStep(h);

        var sum = 0.5 * (EvaluateFinite(function, a) + EvaluateFinite(function, b));
        for (var i = 1; i < intervals; i++)
        {
            sum += EvaluateFinite(function, a + (i * h));
        }

        return EnsureFiniteComputation(h * sum, "Composite trapezoid accumulation overflowed.");
    }

    /// <summary>
    /// Integrates a function with the composite Simpson rule on an even number of equally sized panels.
    /// </summary>
    public static double CompositeSimpson(
        Func<double, double> function,
        double a,
        double b,
        int intervals)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(intervals, nameof(intervals));
        var width = ValidateInterval(a, b);

        if ((intervals & 1) != 0)
        {
            throw new ArgumentException("Simpson's composite rule requires an even number of intervals.", nameof(intervals));
        }

        var h = width / intervals;
        EnsureUsableStep(h);

        var sum = EvaluateFinite(function, a) + EvaluateFinite(function, b);
        for (var i = 1; i < intervals; i++)
        {
            var value = EvaluateFinite(function, a + (i * h));
            sum += (i & 1) == 0 ? 2.0 * value : 4.0 * value;
        }

        return EnsureFiniteComputation((h / 3.0) * sum, "Composite Simpson accumulation overflowed.");
    }

    /// <summary>
    /// Integrates a function with adaptive Simpson refinement and returns only the best value estimate.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AdaptiveSimpsonDetailed"/> when the caller needs to know whether the requested
    /// tolerance was met before the recursion-depth or floating-point-resolution limit was reached.
    /// </remarks>
    public static double AdaptiveSimpson(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumDepth = 20)
        => AdaptiveSimpsonDetailed(function, a, b, tolerance, maximumDepth).Value;

    /// <summary>
    /// Integrates a function with adaptive Simpson refinement and exposes convergence diagnostics.
    /// </summary>
    public static AdaptiveIntegrationResult AdaptiveSimpsonDetailed(
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

        if (!TryMidpoint(a, b, out var c))
        {
            throw new ArithmeticException("The integration interval cannot be bisected at the current floating-point resolution.");
        }

        var evaluations = 0;
        var fa = EvaluateFinite(function, a, ref evaluations);
        var fb = EvaluateFinite(function, b, ref evaluations);
        var fc = EvaluateFinite(function, c, ref evaluations);
        var whole = SimpsonEstimate(a, b, fa, fb, fc);

        var estimate = AdaptiveSimpsonCore(
            function,
            a,
            b,
            fa,
            fb,
            fc,
            whole,
            tolerance,
            maximumDepth,
            ref evaluations);

        return new AdaptiveIntegrationResult(
            estimate.Value,
            estimate.Error,
            evaluations,
            estimate.Panels,
            estimate.Status);
    }

    /// <summary>
    /// Integrates a function with Romberg extrapolation and returns only the best diagonal estimate.
    /// </summary>
    /// <remarks>
    /// Use <see cref="RombergDetailed"/> when convergence status, level count and the last diagonal
    /// difference are relevant to the application.
    /// </remarks>
    public static double Romberg(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumLevels = 12)
        => RombergDetailed(function, a, b, tolerance, maximumLevels).Value;

    /// <summary>
    /// Integrates a function with Romberg extrapolation and reports whether the diagonal sequence converged.
    /// </summary>
    public static RombergIntegrationResult RombergDetailed(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumLevels = 12)
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumLevels, nameof(maximumLevels));
        var width = ValidateInterval(a, b);

        if (maximumLevels > MaximumSupportedRombergLevels)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLevels),
                $"Romberg integration supports at most {MaximumSupportedRombergLevels} levels in this reference implementation.");
        }

        var previous = new double[maximumLevels];
        var current = new double[maximumLevels];
        var evaluations = 0;
        var fa = EvaluateFinite(function, a, ref evaluations);
        var fb = EvaluateFinite(function, b, ref evaluations);
        previous[0] = EnsureFiniteComputation(0.5 * width * (fa + fb), "Romberg base estimate overflowed.");
        double? lastError = null;

        for (var level = 1; level < maximumLevels; level++)
        {
            var intervals = 1 << level;
            var h = width / intervals;
            EnsureUsableStep(h);

            var newSamples = 0.0;
            for (var k = 1; k < intervals; k += 2)
            {
                newSamples += EvaluateFinite(function, a + (k * h), ref evaluations);
            }

            current[0] = EnsureFiniteComputation(
                (0.5 * previous[0]) + (h * newSamples),
                "Romberg trapezoid refinement overflowed.");

            var factor = 4.0;
            for (var j = 1; j <= level; j++)
            {
                current[j] = EnsureFiniteComputation(
                    current[j - 1] + ((current[j - 1] - previous[j - 1]) / (factor - 1.0)),
                    "Romberg extrapolation overflowed.");
                factor *= 4.0;
            }

            lastError = System.Math.Abs(current[level] - previous[level - 1]);
            if (lastError <= tolerance)
            {
                return new RombergIntegrationResult(
                    current[level],
                    lastError,
                    level + 1,
                    evaluations,
                    IterationStatus.Converged);
            }

            (previous, current) = (current, previous);
        }

        return new RombergIntegrationResult(
            previous[maximumLevels - 1],
            lastError,
            maximumLevels,
            evaluations,
            IterationStatus.MaximumIterationsReached);
    }

    /// <summary>
    /// Integrates a function with one five-node Gauss-Legendre rule over the complete interval.
    /// </summary>
    public static double GaussLegendre5(Func<double, double> function, double a, double b)
    {
        ArgumentNullException.ThrowIfNull(function);
        ValidateInterval(a, b);
        var evaluations = 0;
        return GaussLegendre5Core(function, a, b, ref evaluations);
    }

    /// <summary>
    /// Integrates a function with adaptive five-node Gauss-Legendre refinement and returns only the best estimate.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AdaptiveGaussLegendre5Detailed"/> when the caller needs explicit refinement diagnostics.
    /// </remarks>
    public static double AdaptiveGaussLegendre5(
        Func<double, double> function,
        double a,
        double b,
        double tolerance = 1e-10,
        int maximumDepth = 16)
        => AdaptiveGaussLegendre5Detailed(function, a, b, tolerance, maximumDepth).Value;

    /// <summary>
    /// Integrates a function with adaptive five-node Gauss-Legendre refinement and exposes convergence diagnostics.
    /// </summary>
    public static AdaptiveIntegrationResult AdaptiveGaussLegendre5Detailed(
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

        var evaluations = 0;
        var whole = GaussLegendre5Core(function, a, b, ref evaluations);
        var estimate = AdaptiveGaussCore(
            function,
            a,
            b,
            whole,
            tolerance,
            maximumDepth,
            ref evaluations);

        return new AdaptiveIntegrationResult(
            estimate.Value,
            estimate.Error,
            evaluations,
            estimate.Panels,
            estimate.Status);
    }

    private static AdaptiveEstimate AdaptiveSimpsonCore(
        Func<double, double> function,
        double a,
        double b,
        double fa,
        double fb,
        double fc,
        double whole,
        double tolerance,
        int depth,
        ref int evaluations)
    {
        if (!TryMidpoint(a, b, out var c) ||
            !TryMidpoint(a, c, out var leftMid) ||
            !TryMidpoint(c, b, out var rightMid))
        {
            return new AdaptiveEstimate(
                whole,
                double.PositiveInfinity,
                1,
                AdaptiveIntegrationStatus.NumericalResolutionReached);
        }

        var fLeftMid = EvaluateFinite(function, leftMid, ref evaluations);
        var fRightMid = EvaluateFinite(function, rightMid, ref evaluations);
        var left = SimpsonEstimate(a, c, fa, fc, fLeftMid);
        var right = SimpsonEstimate(c, b, fc, fb, fRightMid);
        var split = EnsureFiniteComputation(left + right, "Adaptive Simpson accumulation overflowed.");
        var delta = EnsureFiniteComputation(split - whole, "Adaptive Simpson error estimate overflowed.");
        var error = System.Math.Abs(delta) / 15.0;
        var corrected = EnsureFiniteComputation(split + (delta / 15.0), "Adaptive Simpson correction overflowed.");

        if (error <= tolerance)
        {
            return new AdaptiveEstimate(corrected, error, 2, AdaptiveIntegrationStatus.Converged);
        }

        if (depth <= 0)
        {
            return new AdaptiveEstimate(corrected, error, 2, AdaptiveIntegrationStatus.MaximumDepthReached);
        }

        var leftEstimate = AdaptiveSimpsonCore(
            function,
            a,
            c,
            fa,
            fc,
            fLeftMid,
            left,
            tolerance / 2.0,
            depth - 1,
            ref evaluations);
        var rightEstimate = AdaptiveSimpsonCore(
            function,
            c,
            b,
            fc,
            fb,
            fRightMid,
            right,
            tolerance / 2.0,
            depth - 1,
            ref evaluations);

        return Combine(leftEstimate, rightEstimate, "Adaptive Simpson recursive accumulation overflowed.");
    }

    private static AdaptiveEstimate AdaptiveGaussCore(
        Func<double, double> function,
        double a,
        double b,
        double whole,
        double tolerance,
        int depth,
        ref int evaluations)
    {
        if (!TryMidpoint(a, b, out var midpoint))
        {
            return new AdaptiveEstimate(
                whole,
                double.PositiveInfinity,
                1,
                AdaptiveIntegrationStatus.NumericalResolutionReached);
        }

        var left = GaussLegendre5Core(function, a, midpoint, ref evaluations);
        var right = GaussLegendre5Core(function, midpoint, b, ref evaluations);
        var split = EnsureFiniteComputation(left + right, "Adaptive Gauss-Legendre accumulation overflowed.");
        var error = System.Math.Abs(EnsureFiniteComputation(
            split - whole,
            "Adaptive Gauss-Legendre error estimate overflowed."));

        if (error <= tolerance)
        {
            return new AdaptiveEstimate(split, error, 2, AdaptiveIntegrationStatus.Converged);
        }

        if (depth <= 0)
        {
            return new AdaptiveEstimate(split, error, 2, AdaptiveIntegrationStatus.MaximumDepthReached);
        }

        // The already computed half-interval rules become the whole-interval estimates for
        // the recursive calls. This avoids needless repeated callback evaluations while
        // keeping the algorithm and its error comparison straightforward.
        var leftEstimate = AdaptiveGaussCore(
            function,
            a,
            midpoint,
            left,
            tolerance / 2.0,
            depth - 1,
            ref evaluations);
        var rightEstimate = AdaptiveGaussCore(
            function,
            midpoint,
            b,
            right,
            tolerance / 2.0,
            depth - 1,
            ref evaluations);

        return Combine(leftEstimate, rightEstimate, "Adaptive Gauss-Legendre recursive accumulation overflowed.");
    }

    private static double GaussLegendre5Core(
        Func<double, double> function,
        double a,
        double b,
        ref int evaluations)
    {
        var width = b - a;
        var midpoint = a + (width / 2.0);
        var half = width / 2.0;
        var sum = 0.0;

        for (var i = 0; i < Gauss5Nodes.Length; i++)
        {
            var point = midpoint + (half * Gauss5Nodes[i]);
            sum += Gauss5Weights[i] * EvaluateFinite(function, point, ref evaluations);
        }

        return EnsureFiniteComputation(half * sum, "Gauss-Legendre accumulation overflowed.");
    }

    private static AdaptiveEstimate Combine(
        AdaptiveEstimate left,
        AdaptiveEstimate right,
        string overflowMessage)
    {
        var value = EnsureFiniteComputation(left.Value + right.Value, overflowMessage);
        var error = left.Error + right.Error;
        if (!double.IsFinite(error))
        {
            error = double.PositiveInfinity;
        }

        return new AdaptiveEstimate(
            value,
            error,
            left.Panels + right.Panels,
            CombineStatus(left.Status, right.Status));
    }

    private static AdaptiveIntegrationStatus CombineStatus(
        AdaptiveIntegrationStatus left,
        AdaptiveIntegrationStatus right)
    {
        if (left == AdaptiveIntegrationStatus.NumericalResolutionReached ||
            right == AdaptiveIntegrationStatus.NumericalResolutionReached)
        {
            return AdaptiveIntegrationStatus.NumericalResolutionReached;
        }

        if (left == AdaptiveIntegrationStatus.MaximumDepthReached ||
            right == AdaptiveIntegrationStatus.MaximumDepthReached)
        {
            return AdaptiveIntegrationStatus.MaximumDepthReached;
        }

        return AdaptiveIntegrationStatus.Converged;
    }

    private static double SimpsonEstimate(double a, double b, double fa, double fb, double fm)
        => EnsureFiniteComputation(
            (b - a) * (fa + (4.0 * fm) + fb) / 6.0,
            "Simpson estimate overflowed.");

    private static double EvaluateFinite(Func<double, double> function, double point)
    {
        var evaluations = 0;
        return EvaluateFinite(function, point, ref evaluations);
    }

    private static double EvaluateFinite(Func<double, double> function, double point, ref int evaluations)
    {
        if (!double.IsFinite(point))
        {
            throw new ArithmeticException("Numerical integration produced a non-finite sample point.");
        }

        var value = function(point);
        evaluations++;
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("The supplied integrand returned a non-finite value.");
        }

        return value;
    }

    private static double EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }

        return value;
    }

    private static void EnsureUsableStep(double step)
    {
        if (!double.IsFinite(step) || step <= 0.0)
        {
            throw new ArithmeticException("The requested panel count produces a non-finite or zero integration step.");
        }
    }

    private static bool TryMidpoint(double a, double b, out double midpoint)
    {
        midpoint = a + ((b - a) / 2.0);
        return double.IsFinite(midpoint) && midpoint > a && midpoint < b;
    }

    private static double ValidateInterval(double a, double b)
    {
        NumericGuard.Finite(a, nameof(a));
        NumericGuard.Finite(b, nameof(b));
        if (a >= b)
        {
            throw new ArgumentException("Integration requires a < b.");
        }

        var width = b - a;
        if (!double.IsFinite(width) || width <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(b),
                "The integration interval width must be representable as a positive finite double.");
        }

        return width;
    }

    private readonly record struct AdaptiveEstimate(
        double Value,
        double Error,
        int Panels,
        AdaptiveIntegrationStatus Status);
}
