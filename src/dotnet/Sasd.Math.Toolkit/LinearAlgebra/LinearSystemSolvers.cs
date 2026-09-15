using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Classical direct and iterative solvers for small dense linear systems.
/// </summary>
/// <remarks>
/// The implementation favors explicit textbook steps and diagnostic behavior over premature
/// optimization. For repeated solves with the same coefficient matrix, prefer
/// <see cref="FactorizeLu(DenseMatrix, double)"/> so the elimination work is performed once.
/// </remarks>
public static class LinearSystemSolvers
{
    /// <summary>
    /// Computes the determinant of a square matrix by Gaussian elimination with partial pivoting.
    /// </summary>
    /// <remarks>
    /// A pivot whose magnitude does not exceed <paramref name="pivotTolerance"/> is treated as
    /// numerically zero, in which case the method returns zero. The tolerance is a practical
    /// singularity threshold; it is not a condition-number estimate.
    /// </remarks>
    public static double Determinant(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(pivotTolerance, nameof(pivotTolerance));

        var work = matrix.Clone();
        var sign = 1.0;
        var determinant = 1.0;

        for (var pivot = 0; pivot < work.Rows; pivot++)
        {
            var best = FindPivotRow(work, pivot);
            if (System.Math.Abs(work[best, pivot]) <= pivotTolerance)
            {
                return 0.0;
            }

            if (best != pivot)
            {
                work.SwapRows(best, pivot);
                sign = -sign;
            }

            var pivotValue = work[pivot, pivot];
            determinant *= pivotValue;
            EnsureFiniteComputation(determinant, "Determinant computation overflowed.");

            for (var row = pivot + 1; row < work.Rows; row++)
            {
                var factor = work[row, pivot] / pivotValue;
                EnsureFiniteComputation(factor, "Determinant elimination produced a non-finite multiplier.");

                for (var column = pivot + 1; column < work.Columns; column++)
                {
                    var updated = work[row, column] - (factor * work[pivot, column]);
                    EnsureFiniteComputation(updated, "Determinant elimination produced a non-finite matrix entry.");
                    work[row, column] = updated;
                }
            }
        }

        var result = sign * determinant;
        EnsureFiniteComputation(result, "Determinant computation overflowed.");
        return result;
    }

    /// <summary>
    /// Solves <c>A*x=b</c> with classical Gaussian elimination and back substitution.
    /// </summary>
    /// <param name="matrix">Square coefficient matrix. It is not modified.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="partialPivoting">Whether to choose the largest available pivot magnitude in each column.</param>
    /// <param name="pivotTolerance">Absolute pivot magnitude treated as numerically zero.</param>
    /// <returns>The solution vector.</returns>
    /// <remarks>
    /// Partial pivoting should normally remain enabled. The unpivoted mode is retained because it
    /// belongs to the historical/educational algorithm catalog, but it can fail on a nonsingular
    /// matrix whose current diagonal entry is zero or too small.
    /// </remarks>
    public static double[] SolveGaussian(
        DenseMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        bool partialPivoting = true,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        ArgumentNullException.ThrowIfNull(rightHandSide);
        NumericGuard.Positive(pivotTolerance, nameof(pivotTolerance));

        if (rightHandSide.Count != matrix.Rows)
        {
            throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));
        }

        ValidateFiniteVector(rightHandSide, nameof(rightHandSide));

        var work = matrix.Clone();
        var rhs = rightHandSide.ToArray();
        var n = work.Rows;

        for (var pivot = 0; pivot < n; pivot++)
        {
            var pivotRow = partialPivoting ? FindPivotRow(work, pivot) : pivot;
            if (System.Math.Abs(work[pivotRow, pivot]) <= pivotTolerance)
            {
                throw new ArithmeticException("Matrix is singular, numerically singular, or requires pivoting that was disabled.");
            }

            if (pivotRow != pivot)
            {
                work.SwapRows(pivotRow, pivot);
                (rhs[pivotRow], rhs[pivot]) = (rhs[pivot], rhs[pivotRow]);
            }

            var pivotValue = work[pivot, pivot];
            for (var row = pivot + 1; row < n; row++)
            {
                var factor = work[row, pivot] / pivotValue;
                EnsureFiniteComputation(factor, "Gaussian elimination produced a non-finite multiplier.");
                work[row, pivot] = 0.0;

                for (var column = pivot + 1; column < n; column++)
                {
                    var updated = work[row, column] - (factor * work[pivot, column]);
                    EnsureFiniteComputation(updated, "Gaussian elimination produced a non-finite matrix entry.");
                    work[row, column] = updated;
                }

                rhs[row] -= factor * rhs[pivot];
                EnsureFiniteComputation(rhs[row], "Gaussian elimination produced a non-finite right-hand side.");
            }
        }

        var solution = new double[n];
        for (var row = n - 1; row >= 0; row--)
        {
            var sum = rhs[row];
            for (var column = row + 1; column < n; column++)
            {
                sum -= work[row, column] * solution[column];
                EnsureFiniteComputation(sum, "Back substitution produced a non-finite intermediate value.");
            }

            solution[row] = sum / work[row, row];
            EnsureFiniteComputation(solution[row], "Back substitution produced a non-finite solution value.");
        }

        return solution;
    }

    /// <summary>
    /// Creates a reusable LU factorization with partial pivoting.
    /// </summary>
    /// <remarks>
    /// Use this API when several systems share the same coefficient matrix. The decomposition is
    /// performed once and each subsequent solve only performs triangular substitutions.
    /// </remarks>
    public static LuFactorization FactorizeLu(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero) =>
        LuFactorization.Decompose(matrix, pivotTolerance);

    /// <summary>
    /// Computes the inverse of a square matrix using one reusable LU factorization.
    /// </summary>
    /// <remarks>
    /// If the actual goal is to solve <c>A*x=b</c>, solving the system directly is normally better
    /// than constructing <c>A^-1</c>. The inverse API is provided for algorithms that genuinely need
    /// the matrix itself and for compatibility with the historical toolbox scope.
    /// </remarks>
    public static DenseMatrix Inverse(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(pivotTolerance, nameof(pivotTolerance));
        return FactorizeLu(matrix, pivotTolerance).Inverse();
    }

    /// <summary>
    /// Solves a square system iteratively with the Gauss-Seidel method.
    /// </summary>
    /// <remarks>
    /// Convergence is not guaranteed for arbitrary matrices. The method reports convergence only
    /// when both the largest component update and the infinity norm of the residual are within the
    /// requested tolerance. Numerical divergence/overflow is returned as
    /// <see cref="IterationStatus.NumericalBreakdown"/> rather than disguised as convergence.
    /// </remarks>
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
        if (rightHandSide.Count != n)
        {
            throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));
        }

        if (initialGuess is not null && initialGuess.Count != n)
        {
            throw new ArgumentException("Initial guess length must match matrix size.", nameof(initialGuess));
        }

        ValidateFiniteVector(rightHandSide, nameof(rightHandSide));
        if (initialGuess is not null)
        {
            ValidateFiniteVector(initialGuess, nameof(initialGuess));
        }

        var x = initialGuess?.ToArray() ?? new double[n];
        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var maxChange = 0.0;
            for (var row = 0; row < n; row++)
            {
                var diagonal = matrix[row, row];
                if (System.Math.Abs(diagonal) <= NumericConstants.NearlyZero)
                {
                    return new IterativeResult<double[]>(
                        x,
                        iteration - 1,
                        IterationStatus.NumericalBreakdown,
                        Message: "Zero or near-zero diagonal encountered.");
                }

                var sum = rightHandSide[row];
                for (var column = 0; column < n; column++)
                {
                    if (column == row)
                    {
                        continue;
                    }

                    var product = matrix[row, column] * x[column];
                    if (!double.IsFinite(product))
                    {
                        return NumericalBreakdown(x, iteration, "Gauss-Seidel multiplication overflowed.");
                    }

                    sum -= product;
                    if (!double.IsFinite(sum))
                    {
                        return NumericalBreakdown(x, iteration, "Gauss-Seidel accumulation became non-finite.");
                    }
                }

                var next = sum / diagonal;
                if (!double.IsFinite(next))
                {
                    return NumericalBreakdown(x, iteration, "Gauss-Seidel produced a non-finite iterate.");
                }

                var change = System.Math.Abs(next - x[row]);
                if (!double.IsFinite(change))
                {
                    return NumericalBreakdown(x, iteration, "Gauss-Seidel update magnitude overflowed.");
                }

                maxChange = System.Math.Max(maxChange, change);
                x[row] = next;
            }

            if (!TryResidualInfinityNorm(matrix, x, rightHandSide, out var residual))
            {
                return NumericalBreakdown(x, iteration, "Gauss-Seidel residual computation became non-finite.");
            }

            if (maxChange <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<double[]>(x, iteration, IterationStatus.Converged, residual);
            }
        }

        if (!TryResidualInfinityNorm(matrix, x, rightHandSide, out var finalResidual))
        {
            return NumericalBreakdown(x, maximumIterations, "Final Gauss-Seidel residual computation became non-finite.");
        }

        return new IterativeResult<double[]>(
            x,
            maximumIterations,
            IterationStatus.MaximumIterationsReached,
            finalResidual,
            "Maximum number of iterations reached.");
    }

    /// <summary>
    /// Computes <c>||A*x-b||_infinity</c>.
    /// </summary>
    public static double ResidualInfinityNorm(
        DenseMatrix matrix,
        IReadOnlyList<double> x,
        IReadOnlyList<double> b)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(b);

        if (x.Count != matrix.Columns)
        {
            throw new ArgumentException("Solution vector length must match matrix column count.", nameof(x));
        }

        if (b.Count != matrix.Rows)
        {
            throw new ArgumentException("Right-hand side length must match matrix row count.", nameof(b));
        }

        ValidateFiniteVector(x, nameof(x));
        ValidateFiniteVector(b, nameof(b));

        if (!TryResidualInfinityNorm(matrix, x, b, out var residual))
        {
            throw new ArithmeticException("Residual computation produced a non-finite value.");
        }

        return residual;
    }

    private static IterativeResult<double[]> NumericalBreakdown(
        double[] current,
        int iteration,
        string message) =>
        new(current, iteration, IterationStatus.NumericalBreakdown, Message: message);

    private static bool TryResidualInfinityNorm(
        DenseMatrix matrix,
        IReadOnlyList<double> x,
        IReadOnlyList<double> b,
        out double residual)
    {
        residual = 0.0;

        for (var row = 0; row < matrix.Rows; row++)
        {
            var sum = 0.0;
            for (var column = 0; column < matrix.Columns; column++)
            {
                var product = matrix[row, column] * x[column];
                if (!double.IsFinite(product))
                {
                    return false;
                }

                sum += product;
                if (!double.IsFinite(sum))
                {
                    return false;
                }
            }

            var difference = sum - b[row];
            if (!double.IsFinite(difference))
            {
                return false;
            }

            residual = System.Math.Max(residual, System.Math.Abs(difference));
        }

        return true;
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
        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Matrix must be square.", nameof(matrix));
        }
    }

    private static void ValidateFiniteVector(IReadOnlyList<double> vector, string parameterName)
    {
        for (var i = 0; i < vector.Count; i++)
        {
            if (!double.IsFinite(vector[i]))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Vector values must be finite.");
            }
        }
    }

    private static void EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
