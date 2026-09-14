using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Classical dense-matrix routines matching the educational scope of the original toolbox.
/// </summary>
public static class LinearSystemSolvers
{
    public static double Determinant(DenseMatrix matrix, double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        var work = matrix.Clone();
        var sign = 1.0;
        var determinant = 1.0;

        for (var pivot = 0; pivot < work.Rows; pivot++)
        {
            var best = FindPivotRow(work, pivot);
            if (System.Math.Abs(work[best, pivot]) <= pivotTolerance) return 0.0;
            if (best != pivot)
            {
                work.SwapRows(best, pivot);
                sign = -sign;
            }

            var pivotValue = work[pivot, pivot];
            determinant *= pivotValue;
            for (var row = pivot + 1; row < work.Rows; row++)
            {
                var factor = work[row, pivot] / pivotValue;
                for (var column = pivot + 1; column < work.Columns; column++)
                {
                    work[row, column] -= factor * work[pivot, column];
                }
            }
        }

        return sign * determinant;
    }

    public static double[] SolveGaussian(
        DenseMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        bool partialPivoting = true,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (rightHandSide.Count != matrix.Rows) throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));

        var work = matrix.Clone();
        var rhs = rightHandSide.ToArray();
        var n = work.Rows;

        for (var pivot = 0; pivot < n; pivot++)
        {
            var pivotRow = partialPivoting ? FindPivotRow(work, pivot) : pivot;
            if (System.Math.Abs(work[pivotRow, pivot]) <= pivotTolerance)
            {
                throw new ArithmeticException("Matrix is singular or numerically singular.");
            }

            if (pivotRow != pivot)
            {
                work.SwapRows(pivotRow, pivot);
                (rhs[pivotRow], rhs[pivot]) = (rhs[pivot], rhs[pivotRow]);
            }

            for (var row = pivot + 1; row < n; row++)
            {
                var factor = work[row, pivot] / work[pivot, pivot];
                work[row, pivot] = 0.0;
                for (var column = pivot + 1; column < n; column++)
                {
                    work[row, column] -= factor * work[pivot, column];
                }
                rhs[row] -= factor * rhs[pivot];
            }
        }

        var solution = new double[n];
        for (var row = n - 1; row >= 0; row--)
        {
            var sum = rhs[row];
            for (var column = row + 1; column < n; column++)
            {
                sum -= work[row, column] * solution[column];
            }
            solution[row] = sum / work[row, row];
        }

        return solution;
    }

    /// <summary>
    /// Creates a reusable LU factorization with partial pivoting.
    /// </summary>
    /// <remarks>
    /// Use this API when several systems share the same coefficient matrix. The
    /// decomposition is performed once and each subsequent solve only performs
    /// triangular substitutions.
    /// </remarks>
    public static LuFactorization FactorizeLu(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero) =>
        LuFactorization.Decompose(matrix, pivotTolerance);

    public static DenseMatrix Inverse(DenseMatrix matrix)
    {
        EnsureSquare(matrix);

        // Reusing one LU decomposition is both clearer and more appropriate than
        // repeating Gaussian elimination independently for every identity column.
        return FactorizeLu(matrix).Inverse();
    }

    public static IterativeResult<double[]> GaussSeidel(
        DenseMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        IReadOnlyList<double>? initialGuess = null,
        double tolerance = 1e-10,
        int maximumIterations = 500)
    {
        EnsureSquare(matrix);
        ArgumentNullException.ThrowIfNull(rightHandSide);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));
        var n = matrix.Rows;
        if (rightHandSide.Count != n) throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));
        if (initialGuess is not null && initialGuess.Count != n) throw new ArgumentException("Initial guess length must match matrix size.", nameof(initialGuess));

        var x = initialGuess?.ToArray() ?? new double[n];
        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var maxChange = 0.0;
            for (var row = 0; row < n; row++)
            {
                var diagonal = matrix[row, row];
                if (System.Math.Abs(diagonal) <= NumericConstants.NearlyZero)
                {
                    return new IterativeResult<double[]>(x, iteration - 1, IterationStatus.NumericalBreakdown,
                        Message: "Zero or near-zero diagonal encountered.");
                }

                var sum = rightHandSide[row];
                for (var column = 0; column < n; column++)
                {
                    if (column != row) sum -= matrix[row, column] * x[column];
                }

                var next = sum / diagonal;
                maxChange = System.Math.Max(maxChange, System.Math.Abs(next - x[row]));
                x[row] = next;
            }

            var residual = ResidualInfinityNorm(matrix, x, rightHandSide);
            if (maxChange <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<double[]>(x, iteration, IterationStatus.Converged, residual);
            }
        }

        return new IterativeResult<double[]>(x, maximumIterations, IterationStatus.MaximumIterationsReached,
            ResidualInfinityNorm(matrix, x, rightHandSide), "Maximum number of iterations reached.");
    }

    public static double ResidualInfinityNorm(DenseMatrix matrix, IReadOnlyList<double> x, IReadOnlyList<double> b)
    {
        var ax = matrix.Multiply(x);
        var maximum = 0.0;
        for (var i = 0; i < ax.Length; i++) maximum = System.Math.Max(maximum, System.Math.Abs(ax[i] - b[i]));
        return maximum;
    }

    private static int FindPivotRow(DenseMatrix matrix, int pivotColumn)
    {
        var best = pivotColumn;
        var bestValue = System.Math.Abs(matrix[pivotColumn, pivotColumn]);
        for (var row = pivotColumn + 1; row < matrix.Rows; row++)
        {
            var candidate = System.Math.Abs(matrix[row, pivotColumn]);
            if (candidate > bestValue)
            {
                best = row;
                bestValue = candidate;
            }
        }
        return best;
    }

    private static void EnsureSquare(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Rows != matrix.Columns) throw new ArgumentException("Matrix must be square.", nameof(matrix));
    }
}
