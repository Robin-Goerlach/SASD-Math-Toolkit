namespace Sasd.Numerics.Interpolation;

/// <summary>
/// Immutable piecewise cubic spline represented on each segment as
/// <c>S_i(x)=A+B*dx+C*dx^2+D*dx^3</c>.
/// </summary>
/// <remarks>
/// Instances are created by <see cref="InterpolationAlgorithms.NaturalCubicSpline"/> or
/// <see cref="InterpolationAlgorithms.ClampedCubicSpline"/>. Evaluation is intentionally
/// restricted to the interpolation interval; this avoids silently treating a spline as an
/// extrapolation model outside the data for which it was constructed.
/// </remarks>
public sealed class CubicSpline
{
    private readonly double[] _x;
    private readonly double[] _a;
    private readonly double[] _b;
    private readonly double[] _c;
    private readonly double[] _d;
    private readonly IReadOnlyList<double> _knotView;

    internal CubicSpline(double[] x, double[] a, double[] b, double[] c, double[] d)
    {
        // Copy even though the builder currently passes private work arrays. Keeping the
        // representation independently owned makes the immutability contract explicit and
        // protects it from future refactors that might otherwise share mutable buffers.
        _x = (double[])x.Clone();
        _a = (double[])a.Clone();
        _b = (double[])b.Clone();
        _c = (double[])c.Clone();
        _d = (double[])d.Clone();
        _knotView = Array.AsReadOnly(_x);
    }

    /// <summary>
    /// Gets the interpolation knots as a read-only view.
    /// </summary>
    public IReadOnlyList<double> Knots => _knotView;

    /// <summary>
    /// Gets the first knot and lower bound of the valid evaluation interval.
    /// </summary>
    public double IntervalStart => _x[0];

    /// <summary>
    /// Gets the last knot and upper bound of the valid evaluation interval.
    /// </summary>
    public double IntervalEnd => _x[^1];

    /// <summary>
    /// Gets the number of cubic polynomial segments.
    /// </summary>
    public int SegmentCount => _x.Length - 1;

    /// <summary>
    /// Evaluates the spline inside its closed interpolation interval.
    /// </summary>
    /// <param name="x">Finite point between <see cref="IntervalStart"/> and <see cref="IntervalEnd"/>.</param>
    /// <returns>The interpolated value.</returns>
    public double Evaluate(double x)
    {
        EnsureInsideInterval(x);
        var i = FindSegment(x);
        var dx = x - _x[i];
        var value = _a[i] + (_b[i] * dx) + (_c[i] * dx * dx) + (_d[i] * dx * dx * dx);
        return EnsureFiniteResult(value, "Spline evaluation");
    }

    /// <summary>
    /// Evaluates the first derivative of the piecewise cubic spline.
    /// </summary>
    /// <param name="x">Finite point inside the interpolation interval.</param>
    /// <returns>The first derivative of the active spline segment.</returns>
    public double FirstDerivative(double x)
    {
        EnsureInsideInterval(x);
        var i = FindSegment(x);
        var dx = x - _x[i];
        var value = _b[i] + (2.0 * _c[i] * dx) + (3.0 * _d[i] * dx * dx);
        return EnsureFiniteResult(value, "Spline first-derivative evaluation");
    }

    /// <summary>
    /// Evaluates the second derivative of the piecewise cubic spline.
    /// </summary>
    /// <param name="x">Finite point inside the interpolation interval.</param>
    /// <returns>The second derivative of the active spline segment.</returns>
    public double SecondDerivative(double x)
    {
        EnsureInsideInterval(x);
        var i = FindSegment(x);
        var dx = x - _x[i];
        var value = (2.0 * _c[i]) + (6.0 * _d[i] * dx);
        return EnsureFiniteResult(value, "Spline second-derivative evaluation");
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

    private static double EnsureFiniteResult(double value, string operation)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException($"{operation} produced a non-finite value.");
        }

        return value;
    }
}
