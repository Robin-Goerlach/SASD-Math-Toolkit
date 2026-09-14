namespace Sasd.Numerics.Interpolation;

/// <summary>
/// Piecewise cubic spline represented as <c>S_i(x)=A+B*dx+C*dx^2+D*dx^3</c>.
/// </summary>
public sealed class CubicSpline
{
    private readonly double[] _x;
    private readonly double[] _a;
    private readonly double[] _b;
    private readonly double[] _c;
    private readonly double[] _d;

    internal CubicSpline(double[] x, double[] a, double[] b, double[] c, double[] d)
    {
        _x = x;
        _a = a;
        _b = b;
        _c = c;
        _d = d;
    }

    public IReadOnlyList<double> Knots => _x;

    /// <summary>
    /// Evaluates the spline inside its interpolation interval.
    /// </summary>
    public double Evaluate(double x)
    {
        EnsureInsideInterval(x);
        var i = FindSegment(x);
        var dx = x - _x[i];
        return _a[i] + (_b[i] * dx) + (_c[i] * dx * dx) + (_d[i] * dx * dx * dx);
    }

    /// <summary>
    /// Evaluates the first derivative of the piecewise cubic spline.
    /// </summary>
    public double FirstDerivative(double x)
    {
        EnsureInsideInterval(x);
        var i = FindSegment(x);
        var dx = x - _x[i];
        return _b[i] + (2.0 * _c[i] * dx) + (3.0 * _d[i] * dx * dx);
    }

    /// <summary>
    /// Evaluates the second derivative of the piecewise cubic spline.
    /// </summary>
    public double SecondDerivative(double x)
    {
        EnsureInsideInterval(x);
        var i = FindSegment(x);
        var dx = x - _x[i];
        return (2.0 * _c[i]) + (6.0 * _d[i] * dx);
    }

    private void EnsureInsideInterval(double x)
    {
        if (!double.IsFinite(x) || x < _x[0] || x > _x[^1])
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                "Spline evaluation requires a finite point inside the interpolation interval.");
        }
    }

    private int FindSegment(double x)
    {
        if (x == _x[^1])
        {
            return _x.Length - 2;
        }

        var index = Array.BinarySearch(_x, x);
        if (index >= 0)
        {
            return System.Math.Min(index, _x.Length - 2);
        }

        return ~index - 1;
    }
}
