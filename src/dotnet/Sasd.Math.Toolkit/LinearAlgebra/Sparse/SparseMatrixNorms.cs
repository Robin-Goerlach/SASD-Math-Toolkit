namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Single-pass and low-workspace norms for canonical CSR matrices.
/// </summary>
/// <remarks>
/// These methods mirror the inexpensive dense norm conventions without materializing a dense
/// matrix. A sparse spectral norm is intentionally not included here: computing it efficiently is
/// an iterative sparse-linear-algebra problem and belongs with the forthcoming sparse solver layer.
/// </remarks>
public static class SparseMatrixNorms
{
    /// <summary>Returns the largest absolute stored entry, or zero for the zero matrix.</summary>
    public static double MaxAbsoluteEntry(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var maximum = 0.0;
        var values = matrix.ValuesSpan;
        for (var i = 0; i < values.Length; i++)
        {
            maximum = System.Math.Max(maximum, System.Math.Abs(values[i]));
        }

        return maximum;
    }

    /// <summary>Returns the induced matrix 1-norm: the maximum absolute column sum.</summary>
    /// <remarks>
    /// Workspace is proportional to the number of non-empty columns rather than the declared
    /// column count, preserving the memory advantage for extremely wide but very sparse matrices.
    /// </remarks>
    public static double OneNorm(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var columnAccumulators = new Dictionary<int, ScaledAbsoluteAccumulator>();
        var columnIndices = matrix.ColumnIndicesSpan;
        var values = matrix.ValuesSpan;

        for (var position = 0; position < values.Length; position++)
        {
            var column = columnIndices[position];
            columnAccumulators.TryGetValue(column, out var accumulator);
            AccumulateAbsoluteValue(values[position], ref accumulator.Scale, ref accumulator.ScaledSum);
            columnAccumulators[column] = accumulator;
        }

        var maximum = 0.0;
        foreach (var accumulator in columnAccumulators.Values)
        {
            maximum = System.Math.Max(
                maximum,
                FinishScaledAbsoluteSum(accumulator.Scale, accumulator.ScaledSum, "Sparse matrix 1-norm overflowed."));
        }

        return maximum;
    }

    /// <summary>Returns the induced matrix infinity-norm: the maximum absolute row sum.</summary>
    public static double InfinityNorm(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var maximum = 0.0;
        var rowPointers = matrix.RowPointersSpan;
        var values = matrix.ValuesSpan;

        for (var row = 0; row < matrix.Rows; row++)
        {
            var scale = 0.0;
            var scaledSum = 0.0;
            for (var position = rowPointers[row]; position < rowPointers[row + 1]; position++)
            {
                AccumulateAbsoluteValue(values[position], ref scale, ref scaledSum);
            }

            maximum = System.Math.Max(
                maximum,
                FinishScaledAbsoluteSum(scale, scaledSum, "Sparse matrix infinity-norm overflowed."));
        }

        return maximum;
    }

    /// <summary>
    /// Returns the Frobenius norm using scaled sum-of-squares accumulation over stored entries.
    /// </summary>
    public static double FrobeniusNorm(CsrMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var scale = 0.0;
        var scaledSquares = 1.0;
        var hasNonZeroEntry = false;
        var values = matrix.ValuesSpan;

        for (var i = 0; i < values.Length; i++)
        {
            var absolute = System.Math.Abs(values[i]);
            if (absolute == 0.0)
            {
                continue;
            }

            hasNonZeroEntry = true;
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

        if (!hasNonZeroEntry)
        {
            return 0.0;
        }

        var result = scale * System.Math.Sqrt(scaledSquares);
        EnsureFinite(result, "Sparse Frobenius norm overflowed.");
        return result;
    }

    private static void AccumulateAbsoluteValue(double value, ref double scale, ref double scaledSum)
    {
        var absolute = System.Math.Abs(value);
        if (absolute == 0.0)
        {
            return;
        }

        if (scale < absolute)
        {
            scaledSum = (scaledSum * (scale / absolute)) + 1.0;
            scale = absolute;
        }
        else
        {
            scaledSum += absolute / scale;
        }
    }

    private static double FinishScaledAbsoluteSum(double scale, double scaledSum, string overflowMessage)
    {
        if (scale == 0.0)
        {
            return 0.0;
        }

        var result = scale * scaledSum;
        EnsureFinite(result, overflowMessage);
        return result;
    }

    private static void EnsureFinite(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }

    private struct ScaledAbsoluteAccumulator
    {
        public double Scale;
        public double ScaledSum;
    }
}
