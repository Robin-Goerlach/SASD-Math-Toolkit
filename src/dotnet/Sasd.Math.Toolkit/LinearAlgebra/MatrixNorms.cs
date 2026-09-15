namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Standard norms and scale diagnostics for finite dense matrices.
/// </summary>
/// <remarks>
/// <para>
/// These helpers centralize the norm conventions used by modern decomposition and conditioning
/// diagnostics. The implementation deliberately uses scaled accumulation where simple squaring or
/// summation can overflow even though the final norm is still representable as a <see cref="double"/>.
/// </para>
/// <para>
/// All methods operate on the dependency-free <see cref="DenseMatrix"/> reference type. They do
/// not modify caller-owned data and they fail explicitly if the mathematically requested result is
/// outside the finite <see cref="double"/> range.
/// </para>
/// </remarks>
public static class MatrixNorms
{
    /// <summary>Returns the largest absolute matrix entry.</summary>
    public static double MaxAbsoluteEntry(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var maximum = 0.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            var values = matrix.GetRowSpan(row);
            for (var column = 0; column < values.Length; column++)
            {
                maximum = System.Math.Max(maximum, System.Math.Abs(values[column]));
            }
        }

        return maximum;
    }

    /// <summary>
    /// Returns the induced matrix 1-norm: the maximum absolute column sum.
    /// </summary>
    public static double OneNorm(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var maximum = 0.0;
        for (var column = 0; column < matrix.Columns; column++)
        {
            var scale = 0.0;
            var scaledSum = 0.0;

            for (var row = 0; row < matrix.Rows; row++)
            {
                AccumulateAbsoluteValue(matrix.GetRowSpan(row)[column], ref scale, ref scaledSum);
            }

            maximum = System.Math.Max(maximum, FinishScaledAbsoluteSum(scale, scaledSum, "Matrix 1-norm overflowed."));
        }

        return maximum;
    }

    /// <summary>
    /// Returns the induced matrix infinity-norm: the maximum absolute row sum.
    /// </summary>
    public static double InfinityNorm(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var maximum = 0.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            var scale = 0.0;
            var scaledSum = 0.0;
            var values = matrix.GetRowSpan(row);

            for (var column = 0; column < values.Length; column++)
            {
                AccumulateAbsoluteValue(values[column], ref scale, ref scaledSum);
            }

            maximum = System.Math.Max(
                maximum,
                FinishScaledAbsoluteSum(scale, scaledSum, "Matrix infinity-norm overflowed."));
        }

        return maximum;
    }

    /// <summary>
    /// Returns the Frobenius norm <c>sqrt(sum(a_ij^2))</c> using scaled sum-of-squares accumulation.
    /// </summary>
    /// <remarks>
    /// Directly squaring very large or very small entries can overflow or underflow prematurely.
    /// The LAPACK-style scaled accumulation used here keeps the intermediate sum dimensionless and
    /// applies the dominant scale only once at the end.
    /// </remarks>
    public static double FrobeniusNorm(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var scale = 0.0;
        var scaledSquares = 1.0;
        var hasNonZeroEntry = false;

        for (var row = 0; row < matrix.Rows; row++)
        {
            var values = matrix.GetRowSpan(row);
            for (var column = 0; column < values.Length; column++)
            {
                var absolute = System.Math.Abs(values[column]);
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
        }

        if (!hasNonZeroEntry)
        {
            return 0.0;
        }

        var result = scale * System.Math.Sqrt(scaledSquares);
        EnsureFinite(result, "Frobenius norm overflowed.");
        return result;
    }

    /// <summary>
    /// Returns the spectral (induced 2-) norm, equal to the largest singular value.
    /// </summary>
    /// <remarks>
    /// Unlike the other norms in this type, the spectral norm requires an SVD and is therefore an
    /// O(m*n*min(m,n)) style decomposition operation rather than a single pass over the entries.
    /// Use <see cref="SingularValueDecomposition"/> directly when singular vectors, rank or
    /// conditioning diagnostics are also required so the factorization is not repeated.
    /// </remarks>
    public static double SpectralNorm(
        DenseMatrix matrix,
        double relativeOrthogonalityTolerance = SingularValueDecomposition.DefaultRelativeOrthogonalityTolerance,
        int maximumSweeps = SingularValueDecomposition.DefaultMaximumSweeps)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        // Rank truncation does not affect sigma_max, so use the SVD default rank tolerance and
        // expose only the iteration controls that can change decomposition convergence.
        return SingularValueDecomposition.Decompose(
            matrix,
            relativeRankTolerance: SingularValueDecomposition.DefaultRelativeRankTolerance,
            relativeOrthogonalityTolerance,
            maximumSweeps).LargestSingularValue;
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
}
