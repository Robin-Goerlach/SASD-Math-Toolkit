using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Stores a reusable LU factorization with partial pivoting for a square dense matrix.
/// </summary>
/// <remarks>
/// The factorization follows <c>P * A = L * U</c>. L and U share one compact row-major matrix.
/// Public factor properties remain defensive; internally, row spans avoid repeated indexer checks
/// inside elimination and substitution loops.
/// </remarks>
public sealed class LuFactorization
{
    private readonly DenseMatrix _lu;
    private readonly int[] _permutation;

    private LuFactorization(DenseMatrix lu, int[] permutation, int pivotSign, double pivotTolerance)
    {
        _lu = lu;
        _permutation = permutation;
        PivotSign = pivotSign;
        PivotTolerance = pivotTolerance;
    }

    public int Size => _lu.Rows;
    public int PivotSign { get; }
    public double PivotTolerance { get; }

    /// <summary>Gets a defensive copy of the row permutation.</summary>
    public int[] Permutation => (int[])_permutation.Clone();

    /// <summary>Gets the unit-lower-triangular factor.</summary>
    public DenseMatrix LowerTriangular
    {
        get
        {
            var lower = new DenseMatrix(Size, Size);
            for (var row = 0; row < Size; row++)
            {
                var source = _lu.GetRowSpan(row);
                var destination = lower.GetMutableRowSpan(row);
                destination[row] = 1.0;
                source[..row].CopyTo(destination);
            }

            return lower;
        }
    }

    /// <summary>Gets the upper-triangular factor.</summary>
    public DenseMatrix UpperTriangular
    {
        get
        {
            var upper = new DenseMatrix(Size, Size);
            for (var row = 0; row < Size; row++)
            {
                var source = _lu.GetRowSpan(row);
                var destination = upper.GetMutableRowSpan(row);
                source[row..].CopyTo(destination[row..]);
            }

            return upper;
        }
    }

    /// <summary>Computes an LU decomposition with partial row pivoting.</summary>
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
        var permutation = new int[size];
        for (var i = 0; i < size; i++)
        {
            permutation[i] = i;
        }

        var pivotSign = 1;
        for (var pivotColumn = 0; pivotColumn < size; pivotColumn++)
        {
            var pivotRow = pivotColumn;
            var pivotMagnitude = System.Math.Abs(lu.GetRowSpan(pivotColumn)[pivotColumn]);
            for (var row = pivotColumn + 1; row < size; row++)
            {
                var candidateMagnitude = System.Math.Abs(lu.GetRowSpan(row)[pivotColumn]);
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

            // Once the pivot row has been selected it stays unchanged for the rest of this
            // column elimination. Keeping a row span removes millions of checked indexer calls
            // for medium dense matrices without using unsafe memory access.
            var pivotRowValues = lu.GetRowSpan(pivotColumn);
            var pivot = pivotRowValues[pivotColumn];
            for (var row = pivotColumn + 1; row < size; row++)
            {
                var rowValues = lu.GetMutableRowSpan(row);
                var multiplier = rowValues[pivotColumn] / pivot;
                EnsureFiniteComputation(multiplier, "LU factorization produced a non-finite multiplier.");
                rowValues[pivotColumn] = multiplier;

                for (var column = pivotColumn + 1; column < size; column++)
                {
                    var updated = rowValues[column] - (multiplier * pivotRowValues[column]);
                    EnsureFiniteComputation(updated, "LU factorization produced a non-finite matrix entry.");
                    rowValues[column] = updated;
                }
            }
        }

        return new LuFactorization(lu, permutation, pivotSign, pivotTolerance);
    }

    /// <summary>Solves <c>A*x=b</c> using the stored factorization.</summary>
    /// <remarks>
    /// Forward and back substitution are deliberately performed in the same working array. This
    /// removes one full-size temporary allocation per solve while retaining straightforward code.
    /// </remarks>
    public double[] Solve(IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (rightHandSide.Count != Size)
        {
            throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));
        }

        var solution = new double[Size];
        for (var row = 0; row < Size; row++)
        {
            var value = rightHandSide[_permutation[row]];
            if (!double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(rightHandSide), "Right-hand side values must be finite.");
            }

            var luRow = _lu.GetRowSpan(row);
            for (var column = 0; column < row; column++)
            {
                value -= luRow[column] * solution[column];
                EnsureFiniteComputation(value, "LU forward substitution produced a non-finite value.");
            }

            solution[row] = value;
        }

        for (var row = Size - 1; row >= 0; row--)
        {
            var luRow = _lu.GetRowSpan(row);
            var value = solution[row];
            for (var column = row + 1; column < Size; column++)
            {
                value -= luRow[column] * solution[column];
                EnsureFiniteComputation(value, "LU back substitution produced a non-finite value.");
            }

            solution[row] = value / luRow[row];
            EnsureFiniteComputation(solution[row], "LU back substitution produced a non-finite solution value.");
        }

        return solution;
    }

    /// <summary>Solves <c>A*X=B</c> for several right-hand sides stored as columns.</summary>
    /// <remarks>
    /// All right-hand sides are substituted together. The previous implementation extracted every
    /// column and invoked the scalar solver separately, allocating two or three temporary vectors per
    /// column. This batched path traverses rows contiguously and allocates only the result matrix.
    /// </remarks>
    public DenseMatrix Solve(DenseMatrix rightHandSides)
    {
        ArgumentNullException.ThrowIfNull(rightHandSides);
        if (rightHandSides.Rows != Size)
        {
            throw new ArgumentException("Right-hand side row count must match matrix size.", nameof(rightHandSides));
        }

        var solution = new DenseMatrix(Size, rightHandSides.Columns);

        // Apply P to every right-hand-side column in one row copy.
        for (var row = 0; row < Size; row++)
        {
            rightHandSides.GetRowSpan(_permutation[row]).CopyTo(solution.GetMutableRowSpan(row));
        }

        // L has a unit diagonal, so forward substitution only subtracts earlier solved rows.
        for (var row = 0; row < Size; row++)
        {
            var current = solution.GetMutableRowSpan(row);
            var luRow = _lu.GetRowSpan(row);
            for (var lowerRow = 0; lowerRow < row; lowerRow++)
            {
                var multiplier = luRow[lowerRow];
                var previous = solution.GetRowSpan(lowerRow);
                for (var rhsColumn = 0; rhsColumn < current.Length; rhsColumn++)
                {
                    var updated = current[rhsColumn] - (multiplier * previous[rhsColumn]);
                    EnsureFiniteComputation(updated, "Batched LU forward substitution produced a non-finite value.");
                    current[rhsColumn] = updated;
                }
            }
        }

        for (var row = Size - 1; row >= 0; row--)
        {
            var current = solution.GetMutableRowSpan(row);
            var luRow = _lu.GetRowSpan(row);
            for (var upperRow = row + 1; upperRow < Size; upperRow++)
            {
                var multiplier = luRow[upperRow];
                var solved = solution.GetRowSpan(upperRow);
                for (var rhsColumn = 0; rhsColumn < current.Length; rhsColumn++)
                {
                    var updated = current[rhsColumn] - (multiplier * solved[rhsColumn]);
                    EnsureFiniteComputation(updated, "Batched LU back substitution produced a non-finite value.");
                    current[rhsColumn] = updated;
                }
            }

            var pivot = luRow[row];
            for (var rhsColumn = 0; rhsColumn < current.Length; rhsColumn++)
            {
                current[rhsColumn] /= pivot;
                EnsureFiniteComputation(current[rhsColumn], "Batched LU back substitution produced a non-finite solution value.");
            }
        }

        return solution;
    }

    /// <summary>Computes the determinant from the diagonal of <c>U</c> and permutation parity.</summary>
    public double Determinant()
    {
        var determinant = (double)PivotSign;
        for (var i = 0; i < Size; i++)
        {
            determinant *= _lu.GetRowSpan(i)[i];
            EnsureFiniteComputation(determinant, "LU determinant computation overflowed.");
        }

        return determinant;
    }

    /// <summary>Computes the inverse by solving once for all columns of the identity matrix.</summary>
    public DenseMatrix Inverse() => Solve(DenseMatrix.Identity(Size));

    private static void EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
