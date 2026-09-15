using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Least-squares approximation helpers implemented with small, transparent reference algorithms.
/// </summary>
/// <remarks>
/// General linear least-squares fits currently use normal equations with partial pivoting. This keeps
/// the implementation dependency-free and easy to inspect for the V1 reference implementation.
/// Future high-performance or ill-conditioned workloads may use QR/SVD backends behind the same
/// higher-level SASD APIs.
/// </remarks>
public static partial class LeastSquares
{
    /// <summary>
    /// Fits a polynomial <c>c0 + c1*x + ... + cn*x^n</c> in the least-squares sense.
    /// </summary>
    public static double[] FitPolynomial(IReadOnlyList<double> x, IReadOnlyList<double> y, int degree)
    {
        if (degree < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degree));
        }

        return FitBasis(
            x,
            y,
            Enumerable.Range(0, degree + 1)
                .Select(power => (Func<double, double>)(value => System.Math.Pow(value, power)))
                .ToArray());
    }

    /// <summary>
    /// Fits a model that is linear in the supplied basis functions.
    /// </summary>
    public static double[] FitBasis(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        IReadOnlyList<Func<double, double>> basisFunctions)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        ArgumentNullException.ThrowIfNull(basisFunctions);
        if (x.Count == 0)
        {
            throw new ArgumentException("At least one data point is required.");
        }

        if (basisFunctions.Count == 0)
        {
            throw new ArgumentException("At least one basis function is required.", nameof(basisFunctions));
        }

        if (x.Count < basisFunctions.Count)
        {
            throw new ArgumentException("Number of data points must be at least the number of basis functions.");
        }

        var normal = new DenseMatrix(basisFunctions.Count, basisFunctions.Count);
        var rhs = new double[basisFunctions.Count];

        for (var row = 0; row < basisFunctions.Count; row++)
        {
            var rowBasis = basisFunctions[row]
                ?? throw new ArgumentException("Basis functions must not contain null entries.", nameof(basisFunctions));

            for (var column = 0; column < basisFunctions.Count; column++)
            {
                var columnBasis = basisFunctions[column]
                    ?? throw new ArgumentException("Basis functions must not contain null entries.", nameof(basisFunctions));

                var sum = 0.0;
                for (var sample = 0; sample < x.Count; sample++)
                {
                    EnsureFiniteSample(x[sample], y[sample], sample);
                    var rowValue = EvaluateBasis(rowBasis, x[sample]);
                    var columnValue = EvaluateBasis(columnBasis, x[sample]);
                    sum += rowValue * columnValue;
                }

                if (!double.IsFinite(sum))
                {
                    throw new ArithmeticException("Least-squares normal equations produced a non-finite matrix value.");
                }

                normal[row, column] = sum;
            }

            var rhsSum = 0.0;
            for (var sample = 0; sample < x.Count; sample++)
            {
                EnsureFiniteSample(x[sample], y[sample], sample);
                rhsSum += y[sample] * EvaluateBasis(rowBasis, x[sample]);
            }

            if (!double.IsFinite(rhsSum))
            {
                throw new ArithmeticException("Least-squares normal equations produced a non-finite right-hand side.");
            }

            rhs[row] = rhsSum;
        }

        return LinearSystemSolvers.SolveGaussian(normal, rhs, partialPivoting: true);
    }

    /// <summary>
    /// Evaluates polynomial coefficients ordered from constant term to highest degree.
    /// </summary>
    public static double EvaluatePolynomial(IReadOnlyList<double> coefficients, double x)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        NumericGuard.Finite(x, nameof(x));

        var result = 0.0;
        for (var i = coefficients.Count - 1; i >= 0; i--)
        {
            if (!double.IsFinite(coefficients[i]))
            {
                throw new ArgumentOutOfRangeException(nameof(coefficients), "Polynomial coefficients must be finite.");
            }

            result = (result * x) + coefficients[i];
        }

        if (!double.IsFinite(result))
        {
            throw new ArithmeticException("Polynomial evaluation produced a non-finite value.");
        }

        return result;
    }

    private static double EvaluateBasis(Func<double, double> basis, double x)
    {
        var value = basis(x);
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException("A least-squares basis function returned a non-finite value.");
        }

        return value;
    }

    private static void EnsureFiniteSample(double x, double y, int sampleIndex)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                $"Least-squares sample {sampleIndex} contains a non-finite x or y value.");
        }
    }
}
