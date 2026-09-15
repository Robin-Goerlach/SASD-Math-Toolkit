namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Structural diagnostics for canonical sparse matrices.
/// </summary>
/// <remarks>
/// These checks answer inexpensive structural questions that are useful before choosing an
/// iterative solver. They do not attempt expensive spectral proofs. In particular, a positive
/// diagonal and numerical symmetry are necessary but not sufficient to prove positive definiteness.
/// </remarks>
public static class SparseMatrixDiagnostics
{
    /// <summary>
    /// Tests whether a square CSR matrix is numerically symmetric.
    /// </summary>
    /// <param name="matrix">Sparse matrix to inspect.</param>
    /// <param name="relativeTolerance">
    /// Relative coefficient tolerance in the interval [0,1). Zero requires exact symmetry.
    /// </param>
    /// <remarks>
    /// Each stored coefficient is compared with its transposed counterpart. The comparison is
    /// scale-relative to the larger absolute coefficient, so tiny coefficients are not silently
    /// accepted merely because an unrelated absolute floor is large compared with them.
    /// </remarks>
    public static bool IsSymmetric(CsrMatrix matrix, double relativeTolerance = 1e-12)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ValidateRelativeTolerance(relativeTolerance);
        if (!matrix.IsSquare)
        {
            return false;
        }

        var rowPointers = matrix.RowPointersSpan;
        var columns = matrix.ColumnIndicesSpan;
        var values = matrix.ValuesSpan;

        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var position = rowPointers[row]; position < rowPointers[row + 1]; position++)
            {
                var column = columns[position];
                if (column == row)
                {
                    continue;
                }

                var value = values[position];
                var transposed = matrix.GetValue(column, row);
                var scale = System.Math.Max(System.Math.Abs(value), System.Math.Abs(transposed));
                if (scale == 0.0)
                {
                    continue;
                }

                if (System.Math.Abs(value - transposed) > relativeTolerance * scale)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Tests whether every diagonal entry of a square sparse matrix is strictly positive.
    /// </summary>
    /// <remarks>
    /// A strictly positive diagonal is necessary for a symmetric positive-definite matrix but does
    /// not by itself prove positive definiteness.
    /// </remarks>
    public static bool HasStrictlyPositiveDiagonal(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (!matrix.IsSquare)
        {
            return false;
        }

        for (var index = 0; index < matrix.Rows; index++)
        {
            if (!(matrix.GetValue(index, index) > 0.0))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateRelativeTolerance(double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0.0 || tolerance >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Relative tolerance must be finite and in the interval [0, 1).");
        }
    }
}
