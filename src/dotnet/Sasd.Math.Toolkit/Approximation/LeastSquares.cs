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
public static class LeastSquares
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
    /// Fits the positive-domain power model <c>y = a * x^b</c>.
    /// </summary>
    /// <remarks>
    /// The method applies the standard logarithmic transformation
    /// <c>ln(y) = ln(a) + b*ln(x)</c> and performs an ordinary linear least-squares
    /// fit in log space. Consequently every x and y sample must be strictly positive.
    /// The reported residual diagnostics are calculated afterwards in the original y domain.
    /// </remarks>
    public static PowerLawFitResult FitPowerLaw(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        NumericGuard.SameLength(x, y, nameof(x), nameof(y));
        if (x.Count < 2)
        {
            throw new ArgumentException("At least two data points are required to fit a power law.", nameof(x));
        }

        var logX = new double[x.Count];
        var logY = new double[y.Count];

        for (var i = 0; i < x.Count; i++)
        {
            if (!double.IsFinite(x[i]) || x[i] <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Power-law fitting requires finite x values greater than zero.");
            }

            if (!double.IsFinite(y[i]) || y[i] <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "Power-law fitting requires finite y values greater than zero.");
            }

            logX[i] = System.Math.Log(x[i]);
            logY[i] = System.Math.Log(y[i]);
        }

        var firstLogX = logX[0];
        var hasDistinctAbscissa = false;
        for (var i = 1; i < logX.Length; i++)
        {
            if (logX[i] != firstLogX)
            {
                hasDistinctAbscissa = true;
                break;
            }
        }

        if (!hasDistinctAbscissa)
        {
            throw new ArgumentException("At least two distinct x values are required to determine a power-law exponent.", nameof(x));
        }

        // In transformed coordinates this is simply a straight line:
        // log(y) = intercept + exponent * log(x).
        var transformedCoefficients = FitBasis(
            logX,
            logY,
            [static _ => 1.0, static value => value]);

        var scale = System.Math.Exp(transformedCoefficients[0]);
        var exponent = transformedCoefficients[1];
        if (!double.IsFinite(scale) || scale <= 0.0 || !double.IsFinite(exponent))
        {
            throw new ArithmeticException("Power-law fitting produced non-finite model parameters.");
        }

        var residualSumOfSquares = 0.0;
        for (var i = 0; i < x.Count; i++)
        {
            var predicted = scale * System.Math.Pow(x[i], exponent);
            if (!double.IsFinite(predicted))
            {
                throw new ArithmeticException("The fitted power law produced a non-finite prediction for an input sample.");
            }

            var residual = y[i] - predicted;
            residualSumOfSquares += residual * residual;
        }

        if (!double.IsFinite(residualSumOfSquares))
        {
            throw new ArithmeticException("Power-law residual calculation overflowed or became non-finite.");
        }

        var rootMeanSquareError = System.Math.Sqrt(residualSumOfSquares / x.Count);
        return new PowerLawFitResult(
            scale,
            exponent,
            x.Count,
            residualSumOfSquares,
            rootMeanSquareError);
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
