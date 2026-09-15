using Sasd.Numerics.Common;

namespace Sasd.Numerics.Interpolation;

/// <summary>
/// Polynomial and cubic-spline interpolation algorithms.
/// </summary>
/// <remarks>
/// Polynomial interpolation accepts distinct abscissas in any order. Cubic splines require
/// strictly increasing abscissas because the returned <see cref="CubicSpline"/> is a
/// piecewise function over consecutive intervals.
/// </remarks>
public static class InterpolationAlgorithms
{
    /// <summary>
    /// Evaluates the unique interpolation polynomial through the supplied data with the
    /// Lagrange basis formula.
    /// </summary>
    /// <param name="x">Distinct finite abscissas.</param>
    /// <param name="y">Finite ordinates corresponding to <paramref name="x"/>.</param>
    /// <param name="point">Finite point at which the interpolation polynomial is evaluated.</param>
    /// <returns>The interpolated value.</returns>
    /// <remarks>
    /// This routine evaluates the Lagrange form directly and is intentionally optimized for
    /// transparency rather than repeated evaluations. When many points must be evaluated for
    /// the same data set, Newton divided-difference coefficients are normally more convenient.
    /// Extrapolation is allowed mathematically, but can be numerically unreliable.
    /// </remarks>
    public static double Lagrange(IReadOnlyList<double> x, IReadOnlyList<double> y, double point)
    {
        ValidatePoints(x, y, 1);
        EnsureDistinct(x);
        NumericGuard.Finite(point, nameof(point));

        var result = 0.0;
        for (var i = 0; i < x.Count; i++)
        {
            var basis = 1.0;
            for (var j = 0; j < x.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var numerator = FiniteDifference(point, x[j], "Lagrange numerator");
                var denominator = FiniteDifference(x[i], x[j], "Lagrange denominator");
                basis *= numerator / denominator;
                EnsureFiniteComputed(basis, "Lagrange basis evaluation");
            }

            result += y[i] * basis;
            EnsureFiniteComputed(result, "Lagrange interpolation");
        }

        return result;
    }

    /// <summary>
    /// Builds Newton divided-difference coefficients for a fixed interpolation data set.
    /// </summary>
    /// <param name="x">Distinct finite abscissas. Their order defines the Newton basis.</param>
    /// <param name="y">Finite ordinates corresponding to <paramref name="x"/>.</param>
    /// <returns>
    /// Coefficients <c>a0, a1, ...</c> for
    /// <c>a0 + a1(x-x0) + a2(x-x0)(x-x1) + ...</c>.
    /// </returns>
    /// <remarks>
    /// The returned array is independent from the input collections and can be reused for
    /// repeated evaluations with the same abscissas.
    /// </remarks>
    public static double[] NewtonDividedDifferenceCoefficients(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        ValidatePoints(x, y, 1);
        EnsureDistinct(x);

        var coefficients = y.ToArray();
        for (var order = 1; order < coefficients.Length; order++)
        {
            for (var i = coefficients.Length - 1; i >= order; i--)
            {
                var numerator = coefficients[i] - coefficients[i - 1];
                EnsureFiniteComputed(numerator, "Newton divided-difference numerator");

                var denominator = FiniteDifference(x[i], x[i - order], "Newton divided-difference denominator");
                coefficients[i] = numerator / denominator;
                EnsureFiniteComputed(coefficients[i], "Newton divided-difference coefficient");
            }
        }

        return coefficients;
    }

    /// <summary>
    /// Evaluates the Newton divided-difference interpolation polynomial through the supplied data.
    /// </summary>
    /// <param name="x">Distinct finite abscissas.</param>
    /// <param name="y">Finite ordinates corresponding to <paramref name="x"/>.</param>
    /// <param name="point">Finite point at which the interpolation polynomial is evaluated.</param>
    /// <returns>The interpolated value.</returns>
    /// <remarks>
    /// This convenience method builds the divided-difference coefficients for each call. For
    /// many evaluations of one data set, call <see cref="NewtonDividedDifferenceCoefficients"/>
    /// once and evaluate the Newton form in application code or a future reusable interpolant.
    /// </remarks>
    public static double NewtonDividedDifference(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double point)
    {
        NumericGuard.Finite(point, nameof(point));
        var coefficients = NewtonDividedDifferenceCoefficients(x, y);
        var result = coefficients[^1];

        for (var i = coefficients.Length - 2; i >= 0; i--)
        {
            var offset = FiniteDifference(point, x[i], "Newton interpolation offset");
            result = coefficients[i] + (offset * result);
            EnsureFiniteComputed(result, "Newton interpolation");
        }

        return result;
    }

    /// <summary>
    /// Constructs a natural cubic spline through the supplied interpolation points.
    /// </summary>
    /// <param name="x">Strictly increasing finite knots.</param>
    /// <param name="y">Finite values at the knots.</param>
    /// <returns>A spline whose second derivative is zero at both end knots.</returns>
    /// <remarks>
    /// At least two points are required. The returned spline is defined only on the closed
    /// interval from the first to the last knot; extrapolation is deliberately rejected.
    /// </remarks>
    public static CubicSpline NaturalCubicSpline(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        ValidatePoints(x, y, 2);
        EnsureStrictlyIncreasing(x);
        return BuildCubicSpline(x, y, leftDerivative: null, rightDerivative: null);
    }

    /// <summary>
    /// Constructs a clamped cubic spline through the supplied interpolation points.
    /// </summary>
    /// <param name="x">Strictly increasing finite knots.</param>
    /// <param name="y">Finite values at the knots.</param>
    /// <param name="leftDerivative">Finite prescribed first derivative at the first knot.</param>
    /// <param name="rightDerivative">Finite prescribed first derivative at the last knot.</param>
    /// <returns>A spline that interpolates all points and honors both endpoint derivatives.</returns>
    /// <remarks>
    /// A clamped spline is useful when endpoint slopes are known from the underlying model.
    /// Exact endpoint slopes can substantially improve boundary behavior compared with the
    /// natural condition.
    /// </remarks>
    public static CubicSpline ClampedCubicSpline(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double leftDerivative,
        double rightDerivative)
    {
        ValidatePoints(x, y, 2);
        EnsureStrictlyIncreasing(x);
        NumericGuard.Finite(leftDerivative, nameof(leftDerivative));
        NumericGuard.Finite(rightDerivative, nameof(rightDerivative));
        return BuildCubicSpline(x, y, leftDerivative, rightDerivative);
    }

    /// <summary>
    /// Shared tridiagonal spline construction. Natural and clamped splines differ only in
    /// their endpoint equations, so keeping the interior solve in one implementation avoids
    /// two subtly diverging copies of the same numerical algorithm.
    /// </summary>
    private static CubicSpline BuildCubicSpline(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double? leftDerivative,
        double? rightDerivative)
    {
        var clamped = leftDerivative.HasValue && rightDerivative.HasValue;
        if (leftDerivative.HasValue != rightDerivative.HasValue)
        {
            throw new InvalidOperationException("Spline boundary derivatives must be supplied as a pair.");
        }

        var n = x.Count - 1;
        var a = y.ToArray();
        var b = new double[n];
        var c = new double[n + 1];
        var d = new double[n];
        var h = new double[n];
        var alpha = new double[n + 1];
        var l = new double[n + 1];
        var mu = new double[n + 1];
        var z = new double[n + 1];

        for (var i = 0; i < n; i++)
        {
            h[i] = FiniteDifference(x[i + 1], x[i], "Spline knot spacing");
            if (h[i] <= 0.0)
            {
                throw new ArithmeticException("Spline knot spacing must remain positive after subtraction.");
            }
        }

        for (var i = 1; i < n; i++)
        {
            alpha[i] =
                (3.0 / h[i] * (a[i + 1] - a[i])) -
                (3.0 / h[i - 1] * (a[i] - a[i - 1]));
            EnsureFiniteComputed(alpha[i], "Spline right-hand side");
        }

        if (clamped)
        {
            alpha[0] = (3.0 * (a[1] - a[0]) / h[0]) - (3.0 * leftDerivative!.Value);
            alpha[n] = (3.0 * rightDerivative!.Value) - (3.0 * (a[n] - a[n - 1]) / h[n - 1]);
            EnsureFiniteComputed(alpha[0], "Left clamped spline boundary equation");
            EnsureFiniteComputed(alpha[n], "Right clamped spline boundary equation");

            l[0] = 2.0 * h[0];
            EnsureUsablePivot(l[0]);
            mu[0] = 0.5;
            z[0] = alpha[0] / l[0];
        }
        else
        {
            // Natural boundary condition: S'' is zero at the first knot.
            l[0] = 1.0;
            mu[0] = 0.0;
            z[0] = 0.0;
        }

        for (var i = 1; i < n; i++)
        {
            l[i] = (2.0 * (x[i + 1] - x[i - 1])) - (h[i - 1] * mu[i - 1]);
            EnsureUsablePivot(l[i]);
            mu[i] = h[i] / l[i];
            z[i] = (alpha[i] - (h[i - 1] * z[i - 1])) / l[i];
            EnsureFiniteComputed(mu[i], "Spline elimination multiplier");
            EnsureFiniteComputed(z[i], "Spline elimination right-hand side");
        }

        if (clamped)
        {
            l[n] = h[n - 1] * (2.0 - mu[n - 1]);
            EnsureUsablePivot(l[n]);
            z[n] = (alpha[n] - (h[n - 1] * z[n - 1])) / l[n];
            EnsureFiniteComputed(z[n], "Right clamped spline solution");
            c[n] = z[n];
        }
        else
        {
            // Natural boundary condition: S'' is zero at the last knot.
            l[n] = 1.0;
            z[n] = 0.0;
            c[n] = 0.0;
        }

        for (var j = n - 1; j >= 0; j--)
        {
            c[j] = z[j] - (mu[j] * c[j + 1]);
            b[j] = ((a[j + 1] - a[j]) / h[j]) -
                   (h[j] * (c[j + 1] + (2.0 * c[j])) / 3.0);
            d[j] = (c[j + 1] - c[j]) / (3.0 * h[j]);

            EnsureFiniteComputed(c[j], "Spline quadratic coefficient");
            EnsureFiniteComputed(b[j], "Spline linear coefficient");
            EnsureFiniteComputed(d[j], "Spline cubic coefficient");
        }

        return new CubicSpline(x.ToArray(), a, b, c[..n], d);
    }

    private static void ValidatePoints(IReadOnlyList<double> x, IReadOnlyList<double> y, int minimumCount)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));

        if (x.Count < minimumCount)
        {
            throw new ArgumentException($"At least {minimumCount} data points are required.");
        }

        for (var i = 0; i < x.Count; i++)
        {
            if (!double.IsFinite(x[i]) || !double.IsFinite(y[i]))
            {
                throw new ArgumentException("Interpolation coordinates must be finite.");
            }
        }
    }

    private static void EnsureDistinct(IReadOnlyList<double> x)
    {
        var set = new HashSet<double>();
        foreach (var value in x)
        {
            if (!set.Add(value))
            {
                throw new ArgumentException("Interpolation x-values must be unique.", nameof(x));
            }
        }
    }

    private static void EnsureStrictlyIncreasing(IReadOnlyList<double> x)
    {
        for (var i = 1; i < x.Count; i++)
        {
            if (x[i] <= x[i - 1])
            {
                throw new ArgumentException("Spline x-values must be strictly increasing.", nameof(x));
            }
        }
    }

    private static double FiniteDifference(double left, double right, string operation)
    {
        var difference = left - right;
        EnsureFiniteComputed(difference, operation);
        return difference;
    }

    private static void EnsureUsablePivot(double pivot)
    {
        if (!double.IsFinite(pivot) || System.Math.Abs(pivot) <= NumericConstants.NearlyZero)
        {
            throw new ArithmeticException("Cubic-spline construction encountered a numerically singular pivot.");
        }
    }

    private static void EnsureFiniteComputed(double value, string operation)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException($"{operation} produced a non-finite intermediate value.");
        }
    }
}
