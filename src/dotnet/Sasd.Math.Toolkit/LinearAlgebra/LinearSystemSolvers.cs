using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>Classical direct and iterative solvers for small dense linear systems.</summary>
/// <remarks>
/// Public behavior remains textbook-oriented and diagnostic, while the dense hot loops use
/// validated row spans so repeated indexer checks do not dominate O(n^3) elimination work.
/// For repeated solves with one matrix, prefer <see cref="FactorizeLu(DenseMatrix, double)"/>.
/// </remarks>
public static class LinearSystemSolvers
{
    /// <summary>Computes the determinant by Gaussian elimination with partial pivoting.</summary>
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
            if (System.Math.Abs(work.GetRowSpan(best)[pivot]) <= pivotTolerance)
            {
                return 0.0;
            }

            if (best != pivot)
            {
                work.SwapRows(best, pivot);
                sign = -sign;
            }

            var pivotRow = work.GetRowSpan(pivot);
            var pivotValue = pivotRow[pivot];
            determinant *= pivotValue;
            EnsureFiniteComputation(determinant, "Determinant computation overflowed.");

            for (var row = pivot + 1; row < work.Rows; row++)
            {
                var rowValues = work.GetMutableRowSpan(row);
                var factor = rowValues[pivot] / pivotValue;
                EnsureFiniteComputation(factor, "Determinant elimination produced a non-finite multiplier.");

                // The eliminated pivot-column entry is not needed again for determinant-only
                // elimination. Updating only the trailing row keeps the inner loop contiguous.
                for (var column = pivot + 1; column < work.Columns; column++)
                {
                    var updated = rowValues[column] - (factor * pivotRow[column]);
                    EnsureFiniteComputation(updated, "Determinant elimination produced a non-finite matrix entry.");
                    rowValues[column] = updated;
                }
            }
        }

        var result = sign * determinant;
        EnsureFiniteComputation(result, "Determinant computation overflowed.");
        return result;
    }

    /// <summary>Solves <c>A*x=b</c> with Gaussian elimination and back substitution.</summary>
    /// <remarks>
    /// Partial pivoting should normally remain enabled. The unpivoted mode is retained for the
    /// historical/educational catalog. The implementation clones the matrix and RHS, so caller
    /// inputs are never modified.
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
            var pivotRowIndex = partialPivoting ? FindPivotRow(work, pivot) : pivot;
            if (System.Math.Abs(work.GetRowSpan(pivotRowIndex)[pivot]) <= pivotTolerance)
            {
                throw new ArithmeticException("Matrix is singular, numerically singular, or requires pivoting that was disabled.");
            }

            if (pivotRowIndex != pivot)
            {
                work.SwapRows(pivotRowIndex, pivot);
                (rhs[pivotRowIndex], rhs[pivot]) = (rhs[pivot], rhs[pivotRowIndex]);
            }

            var pivotRow = work.GetRowSpan(pivot);
            var pivotValue = pivotRow[pivot];
            for (var row = pivot + 1; row < n; row++)
            {
                var rowValues = work.GetMutableRowSpan(row);
                var factor = rowValues[pivot] / pivotValue;
                EnsureFiniteComputation(factor, "Gaussian elimination produced a non-finite multiplier.");
                rowValues[pivot] = 0.0;

                for (var column = pivot + 1; column < n; column++)
                {
                    var updated = rowValues[column] - (factor * pivotRow[column]);
                    EnsureFiniteComputation(updated, "Gaussian elimination produced a non-finite matrix entry.");
                    rowValues[column] = updated;
                }

                rhs[row] -= factor * rhs[pivot];
                EnsureFiniteComputation(rhs[row], "Gaussian elimination produced a non-finite right-hand side.");
            }
        }

        var solution = new double[n];
        for (var row = n - 1; row >= 0; row--)
        {
            var rowValues = work.GetRowSpan(row);
            var sum = rhs[row];
            for (var column = row + 1; column < n; column++)
            {
                sum -= rowValues[column] * solution[column];
                EnsureFiniteComputation(sum, "Back substitution produced a non-finite intermediate value.");
            }

            solution[row] = sum / rowValues[row];
            EnsureFiniteComputation(solution[row], "Back substitution produced a non-finite solution value.");
        }

        return solution;
    }

    /// <summary>Creates a reusable LU factorization with partial pivoting.</summary>
    public static LuFactorization FactorizeLu(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero) =>
        LuFactorization.Decompose(matrix, pivotTolerance);

    /// <summary>Computes the inverse using one reusable LU factorization.</summary>
    /// <remarks>Prefer solving <c>A*x=b</c> directly when the inverse matrix itself is not required.</remarks>
    public static DenseMatrix Inverse(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(pivotTolerance, nameof(pivotTolerance));
        return FactorizeLu(matrix, pivotTolerance).Inverse();
    }

    /// <summary>Solves a square system iteratively with Gauss-Seidel.</summary>
    /// <remarks>
    /// Convergence is reported only when both the largest component update and infinity residual
    /// satisfy the tolerance. Row spans remove checked matrix indexing from the repeated iteration
    /// without changing the method's convergence semantics.
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

        var rhs = rightHandSide as double[] ?? rightHandSide.ToArray();
        var x = initialGuess?.ToArray() ?? new double[n];
        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var maxChange = 0.0;
            for (var row = 0; row < n; row++)
            {
                var matrixRow = matrix.GetRowSpan(row);
                var diagonal = matrixRow[row];
                if (System.Math.Abs(diagonal) <= NumericConstants.NearlyZero)
                {
                    return new IterativeResult<double[]>(x, iteration - 1, IterationStatus.NumericalBreakdown,
                        Message: "Zero or near-zero diagonal encountered.");
                }

                var sum = rhs[row];
                for (var column = 0; column < n; column++)
                {
                    if (column == row) continue;

                    var product = matrixRow[column] * x[column];
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

            if (!TryResidualInfinityNorm(matrix, x, rhs, out var residual))
            {
                return NumericalBreakdown(x, iteration, "Gauss-Seidel residual computation became non-finite.");
            }

            if (maxChange <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<double[]>(x, iteration, IterationStatus.Converged, residual);
            }
        }

        if (!TryResidualInfinityNorm(matrix, x, rhs, out var finalResidual))
        {
            return NumericalBreakdown(x, maximumIterations, "Final Gauss-Seidel residual computation became non-finite.");
        }

        return new IterativeResult<double[]>(x, maximumIterations, IterationStatus.MaximumIterationsReached,
            finalResidual, "Maximum number of iterations reached.");
    }

    /// <summary>Computes <c>||A*x-b||_infinity</c>.</summary>
    public static double ResidualInfinityNorm(DenseMatrix matrix, IReadOnlyList<double> x, IReadOnlyList<double> b)
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

    private static IterativeResult<double[]> NumericalBreakdown(double[] current, int iteration, string message) =>
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
            var matrixRow = matrix.GetRowSpan(row);
            var sum = 0.0;
            for (var column = 0; column < matrix.Columns; column++)
            {
                var product = matrixRow[column] * x[column];
                if (!double.IsFinite(product)) return false;
                sum += product;
                if (!double.IsFinite(sum)) return false;
            }

            var difference = sum - b[row];
            if (!double.IsFinite(difference)) return false;
            residual = System.Math.Max(residual, System.Math.Abs(difference));
        }

        return true;
    }

    private static int FindPivotRow(DenseMatrix matrix, int pivotColumn)
    {
        var best = pivotColumn;
        var bestValue = System.Math.Abs(matrix.GetRowSpan(pivotColumn)[pivotColumn]);
        for (var row = pivotColumn + 1; row < matrix.Rows; row++)
        {
            var candidate = System.Math.Abs(matrix.GetRowSpan(row)[pivotColumn]);
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
