using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Stores a reusable LU factorization with partial pivoting for a square dense matrix.
/// </summary>
/// <remarks>
/// The factorization follows the convention <c>P * A = L * U</c>, where <c>P</c> is represented
/// by <see cref="Permutation"/>. The lower and upper triangular factors share one internal matrix
/// in compact form; public factor properties return independent matrices so callers cannot mutate
/// the stored decomposition.
/// </remarks>
public sealed class LuFactorization
{
    private readonly DenseMatrix _lu;
    private readonly int[] _permutation;

    private LuFactorization(
        DenseMatrix lu,
        int[] permutation,
        int pivotSign,
        double pivotTolerance)
    {
        _lu = lu;
        _permutation = permutation;
        PivotSign = pivotSign;
        PivotTolerance = pivotTolerance;
    }

    /// <summary>
    /// Gets the order of the factored square matrix.
    /// </summary>
    public int Size => _lu.Rows;

    /// <summary>
    /// Gets +1 or -1 according to the parity of the row permutations.
    /// </summary>
    public int PivotSign { get; }

    /// <summary>
    /// Gets the absolute pivot threshold that was used when creating this factorization.
    /// </summary>
    /// <remarks>
    /// This records the decomposition policy for diagnostics. It must not be interpreted as a
    /// condition-number estimate.
    /// </remarks>
    public double PivotTolerance { get; }

    /// <summary>
    /// Gets a copy of the row permutation used by the factorization.
    /// </summary>
    /// <remarks>
    /// Row <c>i</c> of <c>P * A</c> is row <c>Permutation[i]</c> of the original matrix. A copy is
    /// returned deliberately to keep the factorization immutable.
    /// </remarks>
    public int[] Permutation => (int[])_permutation.Clone();

    /// <summary>
    /// Gets the unit-lower-triangular factor <c>L</c>.
    /// </summary>
    public DenseMatrix LowerTriangular
    {
        get
        {
            var lower = new DenseMatrix(Size, Size);
            for (var row = 0; row < Size; row++)
            {
                lower[row, row] = 1.0;
                for (var column = 0; column < row; column++)
                {
                    lower[row, column] = _lu[row, column];
                }
            }

            return lower;
        }
    }

    /// <summary>
    /// Gets the upper-triangular factor <c>U</c>.
    /// </summary>
    public DenseMatrix UpperTriangular
    {
        get
        {
            var upper = new DenseMatrix(Size, Size);
            for (var row = 0; row < Size; row++)
            {
                for (var column = row; column < Size; column++)
                {
                    upper[row, column] = _lu[row, column];
                }
            }

            return upper;
        }
    }

    /// <summary>
    /// Computes an LU decomposition with partial row pivoting.
    /// </summary>
    /// <param name="matrix">Square matrix to factor. The input matrix is not modified.</param>
    /// <param name="pivotTolerance">Positive absolute threshold below which a pivot is treated as singular.</param>
    /// <returns>A reusable factorization of <paramref name="matrix"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the matrix is not square.</exception>
    /// <exception cref="ArithmeticException">Thrown when the matrix is singular, numerically singular, or arithmetic overflows.</exception>
    public static LuFactorization Decompose(
        DenseMatrix matrix,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        NumericGuard.Positive(pivotTolerance, nameof(pivotTolerance));

        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Matrix must be square.", nameof(matrix));
        }

        var size = matrix.Rows;
        var lu = matrix.Clone();
        var permutation = Enumerable.Range(0, size).ToArray();
        var pivotSign = 1;

        for (var pivotColumn = 0; pivotColumn < size; pivotColumn++)
        {
            // Partial pivoting chooses the largest available magnitude in the current column.
            // This is intentionally the classical, easy-to-audit strategy; scaled pivoting or
            // condition estimation can be added later without changing the public solve model.
            var pivotRow = pivotColumn;
            var pivotMagnitude = System.Math.Abs(lu[pivotColumn, pivotColumn]);
            for (var row = pivotColumn + 1; row < size; row++)
            {
                var candidateMagnitude = System.Math.Abs(lu[row, pivotColumn]);
                if (candidateMagnitude > pivotMagnitude)
                {
                    pivotRow = row;
                    pivotMagnitude = candidateMagnitude;
                }
            }

            if (pivotMagnitude <= pivotTolerance)
            {
                throw new ArithmeticException("Matrix is singular or numerically singular and cannot be LU-factorized.");
            }

            if (pivotRow != pivotColumn)
            {
                lu.SwapRows(pivotRow, pivotColumn);
                (permutation[pivotRow], permutation[pivotColumn]) =
                    (permutation[pivotColumn], permutation[pivotRow]);
                pivotSign = -pivotSign;
            }

            // Store L below the diagonal and U on/above the diagonal in the same matrix.
            var pivot = lu[pivotColumn, pivotColumn];
            for (var row = pivotColumn + 1; row < size; row++)
            {
                var multiplier = lu[row, pivotColumn] / pivot;
                EnsureFiniteComputation(multiplier, "LU factorization produced a non-finite multiplier.");
                lu[row, pivotColumn] = multiplier;

                for (var column = pivotColumn + 1; column < size; column++)
                {
                    var updated = lu[row, column] - (multiplier * lu[pivotColumn, column]);
                    EnsureFiniteComputation(updated, "LU factorization produced a non-finite matrix entry.");
                    lu[row, column] = updated;
                }
            }
        }

        return new LuFactorization(lu, permutation, pivotSign, pivotTolerance);
    }

    /// <summary>
    /// Solves <c>A*x=b</c> using the stored factorization.
    /// </summary>
    public double[] Solve(IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (rightHandSide.Count != Size)
        {
            throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));
        }

        for (var i = 0; i < rightHandSide.Count; i++)
        {
            if (!double.IsFinite(rightHandSide[i]))
            {
                throw new ArgumentOutOfRangeException(nameof(rightHandSide), "Right-hand side values must be finite.");
            }
        }

        var forward = new double[Size];

        // Because P*A=L*U, b must be permuted exactly like the rows of A before
        // forward substitution through the unit-lower-triangular factor.
        for (var row = 0; row < Size; row++)
        {
            var value = rightHandSide[_permutation[row]];
            for (var column = 0; column < row; column++)
            {
                value -= _lu[row, column] * forward[column];
                EnsureFiniteComputation(value, "LU forward substitution produced a non-finite value.");
            }

            forward[row] = value;
        }

        var solution = new double[Size];
        for (var row = Size - 1; row >= 0; row--)
        {
            var value = forward[row];
            for (var column = row + 1; column < Size; column++)
            {
                value -= _lu[row, column] * solution[column];
                EnsureFiniteComputation(value, "LU back substitution produced a non-finite value.");
            }

            solution[row] = value / _lu[row, row];
            EnsureFiniteComputation(solution[row], "LU back substitution produced a non-finite solution value.");
        }

        return solution;
    }

    /// <summary>
    /// Solves <c>A*X=B</c> for several right-hand sides stored as columns.
    /// </summary>
    public DenseMatrix Solve(DenseMatrix rightHandSides)
    {
        ArgumentNullException.ThrowIfNull(rightHandSides);
        if (rightHandSides.Rows != Size)
        {
            throw new ArgumentException("Right-hand side row count must match matrix size.", nameof(rightHandSides));
        }

        var solution = new DenseMatrix(Size, rightHandSides.Columns);
        var columnValues = new double[Size];

        for (var column = 0; column < rightHandSides.Columns; column++)
        {
            for (var row = 0; row < Size; row++)
            {
                columnValues[row] = rightHandSides[row, column];
            }

            var solvedColumn = Solve(columnValues);
            for (var row = 0; row < Size; row++)
            {
                solution[row, column] = solvedColumn[row];
            }
        }

        return solution;
    }

    /// <summary>
    /// Computes the determinant from the diagonal of <c>U</c> and the permutation parity.
    /// </summary>
    public double Determinant()
    {
        var determinant = (double)PivotSign;
        for (var i = 0; i < Size; i++)
        {
            determinant *= _lu[i, i];
            EnsureFiniteComputation(determinant, "LU determinant computation overflowed.");
        }

        return determinant;
    }

    /// <summary>
    /// Computes the inverse by solving once for all columns of the identity matrix.
    /// </summary>
    public DenseMatrix Inverse() => Solve(DenseMatrix.Identity(Size));

    private static void EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
