using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Numerics.Approximation;

/// <summary>
/// Linear least-squares approximation using normal equations with partial pivoting.
/// Suitable for educational and moderate-size problems; future backends may use QR/SVD.
/// </summary>
public static class LeastSquares
{
    public static double[] FitPolynomial(IReadOnlyList<double> x, IReadOnlyList<double> y, int degree)
    {
        if (degree < 0) throw new ArgumentOutOfRangeException(nameof(degree));
        return FitBasis(x, y, Enumerable.Range(0, degree + 1)
            .Select(power => (Func<double, double>)(value => System.Math.Pow(value, power)))
            .ToArray());
    }

    public static double[] FitBasis(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y,
        IReadOnlyList<Func<double, double>> basisFunctions)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        ArgumentNullException.ThrowIfNull(basisFunctions);
        if (x.Count == 0) throw new ArgumentException("At least one data point is required.");
        if (basisFunctions.Count == 0) throw new ArgumentException("At least one basis function is required.", nameof(basisFunctions));
        if (x.Count < basisFunctions.Count) throw new ArgumentException("Number of data points must be at least the number of basis functions.");

        var normal = new DenseMatrix(basisFunctions.Count, basisFunctions.Count);
        var rhs = new double[basisFunctions.Count];

        for (var row = 0; row < basisFunctions.Count; row++)
        {
            for (var column = 0; column < basisFunctions.Count; column++)
            {
                var sum = 0.0;
                for (var sample = 0; sample < x.Count; sample++)
                {
                    sum += basisFunctions[row](x[sample]) * basisFunctions[column](x[sample]);
                }
                normal[row, column] = sum;
            }

            var rhsSum = 0.0;
            for (var sample = 0; sample < x.Count; sample++)
            {
                rhsSum += y[sample] * basisFunctions[row](x[sample]);
            }
            rhs[row] = rhsSum;
        }

        return LinearSystemSolvers.SolveGaussian(normal, rhs, partialPivoting: true);
    }

    public static double EvaluatePolynomial(IReadOnlyList<double> coefficients, double x)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        var result = 0.0;
        for (var i = coefficients.Count - 1; i >= 0; i--) result = (result * x) + coefficients[i];
        return result;
    }
}
