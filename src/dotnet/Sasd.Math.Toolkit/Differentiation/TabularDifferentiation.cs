using Sasd.Numerics.Common;

namespace Sasd.Numerics.Differentiation;

/// <summary>
/// Numerical differentiation for ordered tabular data using local two-, three-
/// and five-point interpolation stencils.
/// </summary>
/// <remarks>
/// The implementation derives finite-difference weights from the derivative of
/// the local Lagrange interpolating polynomial. This keeps the textbook meaning
/// of the historical two/three/five-point routines while also supporting
/// non-uniformly spaced x-values. On an equally spaced grid the generated weights
/// reduce to the familiar forward/backward/central finite-difference formulas.
/// </remarks>
public static class TabularDifferentiation
{
    /// <summary>
    /// Estimates the first derivative at a table entry from two adjacent points.
    /// </summary>
    public static double FirstDerivativeTwoPoint(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int index) => Differentiate(x, y, index, derivativeOrder: 1, pointCount: 2);

    /// <summary>
    /// Estimates the first derivative at a table entry from a local three-point stencil.
    /// </summary>
    public static double FirstDerivativeThreePoint(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int index) => Differentiate(x, y, index, derivativeOrder: 1, pointCount: 3);

    /// <summary>
    /// Estimates the first derivative at a table entry from a local five-point stencil.
    /// </summary>
    public static double FirstDerivativeFivePoint(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int index) => Differentiate(x, y, index, derivativeOrder: 1, pointCount: 5);

    /// <summary>
    /// Estimates the second derivative at a table entry from a local three-point stencil.
    /// </summary>
    public static double SecondDerivativeThreePoint(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int index) => Differentiate(x, y, index, derivativeOrder: 2, pointCount: 3);

    /// <summary>
    /// Estimates the second derivative at a table entry from a local five-point stencil.
    /// </summary>
    public static double SecondDerivativeFivePoint(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int index) => Differentiate(x, y, index, derivativeOrder: 2, pointCount: 5);

    private static double Differentiate(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int index,
        int derivativeOrder,
        int pointCount)
    {
        ValidateTable(x, y, pointCount, index);

        var start = SelectStencilStart(x.Count, index, pointCount);
        var nodes = new double[pointCount];
        var values = new double[pointCount];

        for (var i = 0; i < pointCount; i++)
        {
            nodes[i] = x[start + i];
            values[i] = y[start + i];
        }

        var target = x[index];
        var weights = BuildDerivativeWeights(nodes, target, derivativeOrder);
        var result = 0.0;

        for (var i = 0; i < pointCount; i++)
        {
            result += weights[i] * values[i];
        }

        if (!double.IsFinite(result))
        {
            throw new ArithmeticException("Tabular differentiation produced a non-finite result.");
        }

        return result;
    }

    private static int SelectStencilStart(int dataCount, int index, int pointCount)
    {
        // Prefer a stencil centered around the requested entry. Near either table
        // boundary the stencil is shifted as a whole and naturally becomes one-sided.
        // With two points there is no true center; interior entries therefore use
        // the requested entry and its predecessor, while index zero uses the first pair.
        var start = index - (pointCount / 2);
        if (start < 0)
        {
            return 0;
        }

        if (start + pointCount > dataCount)
        {
            return dataCount - pointCount;
        }

        return start;
    }

    private static double[] BuildDerivativeWeights(
        IReadOnlyList<double> nodes,
        double target,
        int derivativeOrder)
    {
        var weights = new double[nodes.Count];

        // For each Lagrange basis polynomial L_j(x), build its ordinary polynomial
        // coefficients, differentiate them analytically and evaluate L_j^(d)(target).
        // Stencils contain at most five points, so the deliberately straightforward
        // O(n^3) construction is tiny, easy to review and more useful here than an
        // optimized but opaque coefficient generator.
        for (var basisIndex = 0; basisIndex < nodes.Count; basisIndex++)
        {
            var coefficients = new[] { 1.0 }; // ascending powers: c0 + c1*x + ...
            var denominator = 1.0;

            for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                if (nodeIndex == basisIndex)
                {
                    continue;
                }

                coefficients = MultiplyByLinearFactor(coefficients, nodes[nodeIndex]);
                denominator *= nodes[basisIndex] - nodes[nodeIndex];
            }

            if (!double.IsFinite(denominator) || denominator == 0.0)
            {
                throw new ArithmeticException("The selected differentiation stencil is numerically singular.");
            }

            for (var order = 0; order < derivativeOrder; order++)
            {
                coefficients = DifferentiateCoefficients(coefficients);
            }

            weights[basisIndex] = EvaluatePolynomial(coefficients, target) / denominator;
        }

        return weights;
    }

    private static double[] MultiplyByLinearFactor(IReadOnlyList<double> coefficients, double root)
    {
        // Multiply p(x) by (x - root), keeping coefficients in ascending order.
        var result = new double[coefficients.Count + 1];
        for (var power = 0; power < coefficients.Count; power++)
        {
            result[power] -= root * coefficients[power];
            result[power + 1] += coefficients[power];
        }

        return result;
    }

    private static double[] DifferentiateCoefficients(IReadOnlyList<double> coefficients)
    {
        if (coefficients.Count <= 1)
        {
            return new[] { 0.0 };
        }

        var derivative = new double[coefficients.Count - 1];
        for (var power = 1; power < coefficients.Count; power++)
        {
            derivative[power - 1] = power * coefficients[power];
        }

        return derivative;
    }

    private static double EvaluatePolynomial(IReadOnlyList<double> coefficients, double x)
    {
        var result = 0.0;
        for (var power = coefficients.Count - 1; power >= 0; power--)
        {
            result = (result * x) + coefficients[power];
        }

        return result;
    }

    private static void ValidateTable(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        int minimumCount,
        int index)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));

        if (x.Count < minimumCount)
        {
            throw new ArgumentException($"At least {minimumCount} tabular points are required for this formula.");
        }

        if (index < 0 || index >= x.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index must refer to an existing table entry.");
        }

        for (var i = 0; i < x.Count; i++)
        {
            if (!double.IsFinite(x[i]) || !double.IsFinite(y[i]))
            {
                throw new ArgumentException("Tabular x- and y-values must be finite.");
            }

            if (i > 0 && x[i] <= x[i - 1])
            {
                throw new ArgumentException("Tabular x-values must be strictly increasing.", nameof(x));
            }
        }
    }
}
