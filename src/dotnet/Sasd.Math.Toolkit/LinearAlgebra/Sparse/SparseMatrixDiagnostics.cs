namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Structural and residual diagnostics for canonical sparse matrices.
/// </summary>
/// <remarks>
/// Structural checks answer inexpensive questions that are useful before choosing an iterative
/// solver. They do not attempt expensive spectral proofs. In particular, a positive diagonal and
/// numerical symmetry are necessary but not sufficient to prove positive definiteness.
///
/// Residual diagnostics are intentionally independent from any solver recurrence. They can therefore
/// be used by applications, samples and release tests to verify a returned solution against the
/// original physical system.
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

    /// <summary>
    /// Computes the Euclidean norm of the true residual <c>b-A*x</c> for a sparse linear system.
    /// </summary>
    /// <param name="matrix">Sparse matrix <c>A</c>.</param>
    /// <param name="solution">Candidate solution vector <c>x</c>; its length must equal the column count.</param>
    /// <param name="rightHandSide">Right-hand-side vector <c>b</c>; its length must equal the row count.</param>
    /// <returns>The finite Euclidean norm <c>||b-A*x||2</c>.</returns>
    /// <remarks>
    /// <para>
    /// This method deliberately recomputes the physical residual instead of exposing any internal
    /// recursive or projected residual maintained by a Krylov solver. It is therefore suitable for
    /// independent post-solve verification and also works for rectangular matrices.
    /// </para>
    /// <para>
    /// The norm uses scaled accumulation so large finite residual components do not overflow merely
    /// because an implementation squared them naively. If the matrix-vector product or residual
    /// itself is outside the finite <see cref="double"/> range, an <see cref="ArithmeticException"/>
    /// is thrown rather than returning a misleading infinity.
    /// </para>
    /// </remarks>
    public static double ResidualEuclideanNorm(
        CsrMatrix matrix,
        IReadOnlyList<double> solution,
        IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(rightHandSide);

        if (solution.Count != matrix.Columns)
        {
            throw new ArgumentException(
                "Solution length must match sparse matrix column count.",
                nameof(solution));
        }

        if (rightHandSide.Count != matrix.Rows)
        {
            throw new ArgumentException(
                "Right-hand-side length must match sparse matrix row count.",
                nameof(rightHandSide));
        }

        for (var index = 0; index < rightHandSide.Count; index++)
        {
            if (!double.IsFinite(rightHandSide[index]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rightHandSide),
                    "Right-hand-side values must be finite.");
            }
        }

        // CsrMatrix.Multiply performs the corresponding finite-value validation for the solution.
        // This diagnostic is not a solver hot loop, so allocating one matrix-vector result keeps the
        // public API compact and avoids leaking work-buffer management into ordinary application code.
        var matrixVector = matrix.Multiply(solution);

        var scale = 0.0;
        var scaledSquares = 1.0;
        var hasNonZero = false;

        for (var row = 0; row < matrix.Rows; row++)
        {
            var residual = rightHandSide[row] - matrixVector[row];
            if (!double.IsFinite(residual))
            {
                throw new ArithmeticException(
                    "Sparse residual computation produced a non-finite component.");
            }

            var absolute = System.Math.Abs(residual);
            if (absolute == 0.0)
            {
                continue;
            }

            hasNonZero = true;
            if (scale < absolute)
            {
                var ratio = scale / absolute;
                scaledSquares = 1.0 + (scaledSquares * ratio * ratio);
                scale = absolute;
            }
            else
            {
                var ratio = absolute / scale;
                scaledSquares += ratio * ratio;
            }
        }

        if (!hasNonZero)
        {
            return 0.0;
        }

        var norm = scale * System.Math.Sqrt(scaledSquares);
        if (!double.IsFinite(norm))
        {
            throw new ArithmeticException("Sparse residual norm is outside the finite double range.");
        }

        return norm;
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
