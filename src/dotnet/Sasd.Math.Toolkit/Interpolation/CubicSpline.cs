namespace Sasd.Numerics.Interpolation;

/// <summary>
/// Piecewise cubic spline represented as S_i(x)=A+B*dx+C*dx^2+D*dx^3.
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

    public double Evaluate(double x)
    {
        if (x < _x[0] || x > _x[^1])
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Spline evaluation is restricted to the interpolation interval.");
        }

        var i = FindSegment(x);
        var dx = x - _x[i];
        return _a[i] + (_b[i] * dx) + (_c[i] * dx * dx) + (_d[i] * dx * dx * dx);
    }

    public double FirstDerivative(double x)
    {
        if (x < _x[0] || x > _x[^1])
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Spline evaluation is restricted to the interpolation interval.");
        }

        var i = FindSegment(x);
        var dx = x - _x[i];
        return _b[i] + (2.0 * _c[i] * dx) + (3.0 * _d[i] * dx * dx);
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
