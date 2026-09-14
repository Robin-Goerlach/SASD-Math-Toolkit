using Sasd.Numerics.Common;

namespace Sasd.Numerics.Interpolation;

/// <summary>
/// Polynomial and cubic-spline interpolation algorithms.
/// </summary>
public static class InterpolationAlgorithms
{
    public static double Lagrange(IReadOnlyList<double> x, IReadOnlyList<double> y, double point)
    {
        ValidatePoints(x, y, 1);
        EnsureDistinct(x);

        var result = 0.0;
        for (var i = 0; i < x.Count; i++)
        {
            var basis = 1.0;
            for (var j = 0; j < x.Count; j++)
            {
                if (i != j)
                {
                    basis *= (point - x[j]) / (x[i] - x[j]);
                }
            }

            result += y[i] * basis;
        }

        return result;
    }

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
                coefficients[i] = (coefficients[i] - coefficients[i - 1]) / (x[i] - x[i - order]);
            }
        }

        return coefficients;
    }

    public static double NewtonDividedDifference(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double point)
    {
        var coefficients = NewtonDividedDifferenceCoefficients(x, y);
        var result = coefficients[^1];

        for (var i = coefficients.Length - 2; i >= 0; i--)
        {
            result = coefficients[i] + ((point - x[i]) * result);
        }

        return result;
    }

    public static CubicSpline NaturalCubicSpline(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        ValidatePoints(x, y, 2);
        EnsureStrictlyIncreasing(x);

        var n = x.Count - 1;
        var a = y.ToArray();
        var b = new double[n];
        var c = new double[n + 1];
        var d = new double[n];
        var h = new double[n];
        var alpha = new double[n];
        var l = new double[n + 1];
        var mu = new double[n + 1];
        var z = new double[n + 1];

        for (var i = 0; i < n; i++)
        {
            h[i] = x[i + 1] - x[i];
        }

        for (var i = 1; i < n; i++)
        {
            alpha[i] = (3.0 / h[i] * (a[i + 1] - a[i])) - (3.0 / h[i - 1] * (a[i] - a[i - 1]));
        }

        l[0] = 1.0;
        for (var i = 1; i < n; i++)
        {
            l[i] = (2.0 * (x[i + 1] - x[i - 1])) - (h[i - 1] * mu[i - 1]);
            mu[i] = h[i] / l[i];
            z[i] = (alpha[i] - (h[i - 1] * z[i - 1])) / l[i];
        }

        l[n] = 1.0;
        for (var j = n - 1; j >= 0; j--)
        {
            c[j] = z[j] - (mu[j] * c[j + 1]);
            b[j] = ((a[j + 1] - a[j]) / h[j]) - (h[j] * (c[j + 1] + (2.0 * c[j])) / 3.0);
            d[j] = (c[j + 1] - c[j]) / (3.0 * h[j]);
        }

        return new CubicSpline(x.ToArray(), a, b, c[..n], d);
    }

    public static CubicSpline ClampedCubicSpline(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double leftDerivative,
        double rightDerivative)
    {
        ValidatePoints(x, y, 2);
        EnsureStrictlyIncreasing(x);

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
            h[i] = x[i + 1] - x[i];
        }

        alpha[0] = (3.0 * (a[1] - a[0]) / h[0]) - (3.0 * leftDerivative);
        alpha[n] = (3.0 * rightDerivative) - (3.0 * (a[n] - a[n - 1]) / h[n - 1]);
        for (var i = 1; i < n; i++)
        {
            alpha[i] = (3.0 / h[i] * (a[i + 1] - a[i])) - (3.0 / h[i - 1] * (a[i] - a[i - 1]));
        }

        l[0] = 2.0 * h[0];
        mu[0] = 0.5;
        z[0] = alpha[0] / l[0];

        for (var i = 1; i < n; i++)
        {
            l[i] = (2.0 * (x[i + 1] - x[i - 1])) - (h[i - 1] * mu[i - 1]);
            mu[i] = h[i] / l[i];
            z[i] = (alpha[i] - (h[i - 1] * z[i - 1])) / l[i];
        }

        l[n] = h[n - 1] * (2.0 - mu[n - 1]);
        z[n] = (alpha[n] - (h[n - 1] * z[n - 1])) / l[n];
        c[n] = z[n];

        for (var j = n - 1; j >= 0; j--)
        {
            c[j] = z[j] - (mu[j] * c[j + 1]);
            b[j] = ((a[j + 1] - a[j]) / h[j]) - (h[j] * (c[j + 1] + (2.0 * c[j])) / 3.0);
            d[j] = (c[j + 1] - c[j]) / (3.0 * h[j]);
        }

        return new CubicSpline(x.ToArray(), a, b, c[..n], d);
    }

    private static void ValidatePoints(IReadOnlyList<double> x, IReadOnlyList<double> y, int minimumCount)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        if (x.Count < minimumCount)
        {
            throw new ArgumentException($"At least {minimumCount} data points are required.");
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
}
