namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Immutable dense-matrix scale, numerical-rank and 2-norm conditioning report.
/// </summary>
/// <remarks>
/// <para>
/// Conditioning is reported from a singular value decomposition because the singular values give
/// a direct and interpretable 2-norm condition diagnostic for square and rectangular matrices.
/// The same numerical-rank threshold is used for rank, nullity and condition decisions.
/// </para>
/// <para>
/// A large condition number indicates sensitivity of the mathematical problem; it is not by itself
/// evidence of an implementation defect. Conversely, a small residual does not guarantee a small
/// forward error when the problem is ill-conditioned.
/// </para>
/// </remarks>
public sealed class MatrixConditionDiagnostics
{
    private MatrixConditionDiagnostics(
        int rows,
        int columns,
        double maxAbsoluteEntry,
        double oneNorm,
        double infinityNorm,
        double frobeniusNorm,
        double spectralNorm,
        double relativeRankTolerance,
        double rankThreshold,
        int estimatedRank,
        double conditionNumber2,
        double reciprocalConditionNumber2)
    {
        Rows = rows;
        Columns = columns;
        MaxAbsoluteEntry = maxAbsoluteEntry;
        OneNorm = oneNorm;
        InfinityNorm = infinityNorm;
        FrobeniusNorm = frobeniusNorm;
        SpectralNorm = spectralNorm;
        RelativeRankTolerance = relativeRankTolerance;
        RankThreshold = rankThreshold;
        EstimatedRank = estimatedRank;
        ConditionNumber2 = conditionNumber2;
        ReciprocalConditionNumber2 = reciprocalConditionNumber2;
    }

    /// <summary>Gets the original row count.</summary>
    public int Rows { get; }

    /// <summary>Gets the original column count.</summary>
    public int Columns { get; }

    /// <summary>Gets the largest absolute matrix entry.</summary>
    public double MaxAbsoluteEntry { get; }

    /// <summary>Gets the induced matrix 1-norm.</summary>
    public double OneNorm { get; }

    /// <summary>Gets the induced matrix infinity-norm.</summary>
    public double InfinityNorm { get; }

    /// <summary>Gets the Frobenius norm.</summary>
    public double FrobeniusNorm { get; }

    /// <summary>Gets the spectral norm, equal to the largest singular value.</summary>
    public double SpectralNorm { get; }

    /// <summary>Gets the relative singular-value threshold requested by the caller.</summary>
    public double RelativeRankTolerance { get; }

    /// <summary>Gets the resulting absolute singular-value threshold.</summary>
    public double RankThreshold { get; }

    /// <summary>Gets the estimated numerical rank.</summary>
    public int EstimatedRank { get; }

    /// <summary>
    /// Gets whether the matrix has numerical rank <c>min(Rows, Columns)</c> at the configured threshold.
    /// </summary>
    public bool IsFullRank => EstimatedRank == System.Math.Min(Rows, Columns);

    /// <summary>
    /// Gets the dimension of the left null space, <c>Rows - EstimatedRank</c>.
    /// </summary>
    public int LeftNullity => Rows - EstimatedRank;

    /// <summary>
    /// Gets the dimension of the right null space, <c>Columns - EstimatedRank</c>.
    /// </summary>
    /// <remarks>
    /// A wide matrix can be full rank in the rectangular sense while still having a non-zero
    /// right nullity. This distinction is particularly important for underdetermined systems.
    /// </remarks>
    public int RightNullity => Columns - EstimatedRank;

    /// <summary>
    /// Gets the numerical 2-norm condition number, or positive infinity when rank deficient at the
    /// configured threshold.
    /// </summary>
    public double ConditionNumber2 { get; }

    /// <summary>
    /// Gets the reciprocal 2-norm condition number, or zero when rank deficient at the configured
    /// threshold.
    /// </summary>
    public double ReciprocalConditionNumber2 { get; }

    /// <summary>
    /// Analyzes matrix scale, numerical rank and 2-norm conditioning with one shared SVD.
    /// </summary>
    /// <param name="matrix">Finite matrix to analyze.</param>
    /// <param name="relativeRankTolerance">Relative singular-value threshold for numerical rank.</param>
    /// <param name="relativeOrthogonalityTolerance">Jacobi-SVD orthogonality target.</param>
    /// <param name="maximumSweeps">Maximum SVD sweep count.</param>
    /// <remarks>
    /// The four inexpensive entry-based norms are computed directly. Spectral norm, numerical rank
    /// and conditioning reuse a single SVD so callers do not accidentally pay for several identical
    /// decompositions when they need a complete diagnostic report.
    /// </remarks>
    public static MatrixConditionDiagnostics Analyze(
        DenseMatrix matrix,
        double relativeRankTolerance = SingularValueDecomposition.DefaultRelativeRankTolerance,
        double relativeOrthogonalityTolerance = SingularValueDecomposition.DefaultRelativeOrthogonalityTolerance,
        int maximumSweeps = SingularValueDecomposition.DefaultMaximumSweeps)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        var maxAbsoluteEntry = MatrixNorms.MaxAbsoluteEntry(matrix);
        var oneNorm = MatrixNorms.OneNorm(matrix);
        var infinityNorm = MatrixNorms.InfinityNorm(matrix);
        var frobeniusNorm = MatrixNorms.FrobeniusNorm(matrix);

        var svd = SingularValueDecomposition.Decompose(
            matrix,
            relativeRankTolerance,
            relativeOrthogonalityTolerance,
            maximumSweeps);

        return new MatrixConditionDiagnostics(
            matrix.Rows,
            matrix.Columns,
            maxAbsoluteEntry,
            oneNorm,
            infinityNorm,
            frobeniusNorm,
            svd.LargestSingularValue,
            svd.RelativeRankTolerance,
            svd.RankThreshold,
            svd.EstimatedRank,
            svd.ConditionNumber,
            svd.ReciprocalConditionNumber);
    }
}
