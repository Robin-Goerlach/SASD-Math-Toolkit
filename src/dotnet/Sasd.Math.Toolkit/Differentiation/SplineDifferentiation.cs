using Sasd.Numerics.Common;
using Sasd.Numerics.Interpolation;

namespace Sasd.Numerics.Differentiation;

/// <summary>
/// Convenience routines that differentiate cubic spline interpolants built from
/// tabular data.
/// </summary>
/// <remarks>
/// For repeated evaluations callers should construct a <see cref="CubicSpline"/>
/// once and call its derivative methods directly. These helpers are intentionally
/// small one-shot entry points that mirror the historical toolbox workflow.
/// </remarks>
public static class SplineDifferentiation
{
    /// <summary>
    /// Builds a natural cubic spline and evaluates its first derivative.
    /// </summary>
    public static double NaturalFirstDerivative(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double point)
    {
        return InterpolationAlgorithms.NaturalCubicSpline(x, y).FirstDerivative(point);
    }

    /// <summary>
    /// Builds a natural cubic spline and evaluates its second derivative.
    /// </summary>
    public static double NaturalSecondDerivative(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double point)
    {
        return InterpolationAlgorithms.NaturalCubicSpline(x, y).SecondDerivative(point);
    }

    /// <summary>
    /// Builds a clamped cubic spline and evaluates its first derivative.
    /// </summary>
    public static double ClampedFirstDerivative(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double leftDerivative,
        double rightDerivative,
        double point)
    {
        ValidateBoundaryDerivatives(leftDerivative, rightDerivative);
        return InterpolationAlgorithms.ClampedCubicSpline(x, y, leftDerivative, rightDerivative)
            .FirstDerivative(point);
    }

    /// <summary>
    /// Builds a clamped cubic spline and evaluates its second derivative.
    /// </summary>
    public static double ClampedSecondDerivative(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        double leftDerivative,
        double rightDerivative,
        double point)
    {
        ValidateBoundaryDerivatives(leftDerivative, rightDerivative);
        return InterpolationAlgorithms.ClampedCubicSpline(x, y, leftDerivative, rightDerivative)
            .SecondDerivative(point);
    }

    private static void ValidateBoundaryDerivatives(double leftDerivative, double rightDerivative)
    {
        NumericGuard.Finite(leftDerivative, nameof(leftDerivative));
        NumericGuard.Finite(rightDerivative, nameof(rightDerivative));
    }
}
