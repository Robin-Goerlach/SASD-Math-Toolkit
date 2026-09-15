using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Math.Toolkit.Tests;

public sealed class MatrixDiagnosticsTests
{
    [Fact]
    public void MatrixNorms_MatchStandardDefinitions()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1.0, -2.0 },
            { 3.0, 4.0 },
            { -5.0, 6.0 }
        });

        Assert.Equal(6.0, MatrixNorms.MaxAbsoluteEntry(matrix));
        Assert.Equal(12.0, MatrixNorms.OneNorm(matrix));
        Assert.Equal(11.0, MatrixNorms.InfinityNorm(matrix));
        Assert.InRange(
            MatrixNorms.FrobeniusNorm(matrix),
            System.Math.Sqrt(91.0) - 1e-12,
            System.Math.Sqrt(91.0) + 1e-12);
    }

    [Fact]
    public void FrobeniusNorm_UsesScaledAccumulationForLargeFiniteEntries()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1e308, 0.0 },
            { 0.0, 1e308 }
        });

        var norm = MatrixNorms.FrobeniusNorm(matrix);
        var expected = System.Math.Sqrt(2.0) * 1e308;

        Assert.True(double.IsFinite(norm));
        Assert.InRange(norm, expected * (1.0 - 1e-14), expected * (1.0 + 1e-14));

        // The mathematically correct column sum is outside the finite double range. The norm
        // helper reports that range failure instead of silently returning positive infinity.
        var overflowingColumn = new DenseMatrix(new double[,]
        {
            { double.MaxValue },
            { double.MaxValue }
        });
        Assert.Throws<ArithmeticException>(() => MatrixNorms.OneNorm(overflowingColumn));
    }

    [Fact]
    public void SpectralNorm_EqualsLargestSingularValue()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 4.0, 0.0 },
            { 0.0, 2.0 },
            { 0.0, 0.0 }
        });

        var spectralNorm = MatrixNorms.SpectralNorm(matrix);
        var svd = SingularValueDecomposition.Decompose(matrix);

        Assert.Equal(4.0, spectralNorm, 12);
        Assert.Equal(svd.LargestSingularValue, spectralNorm, 12);
    }

    [Fact]
    public void ConditionDiagnostics_ExposeRankNullityAndSharedNorms()
    {
        var rankDeficient = new DenseMatrix(new double[,]
        {
            { 1.0, 2.0 },
            { 2.0, 4.0 },
            { 3.0, 6.0 }
        });

        var diagnostics = MatrixConditionDiagnostics.Analyze(rankDeficient);

        Assert.Equal(3, diagnostics.Rows);
        Assert.Equal(2, diagnostics.Columns);
        Assert.Equal(1, diagnostics.EstimatedRank);
        Assert.False(diagnostics.IsFullRank);
        Assert.Equal(2, diagnostics.LeftNullity);
        Assert.Equal(1, diagnostics.RightNullity);
        Assert.True(double.IsPositiveInfinity(diagnostics.ConditionNumber2));
        Assert.Equal(0.0, diagnostics.ReciprocalConditionNumber2);
        Assert.Equal(MatrixNorms.OneNorm(rankDeficient), diagnostics.OneNorm, 12);
        Assert.Equal(MatrixNorms.InfinityNorm(rankDeficient), diagnostics.InfinityNorm, 12);
        Assert.Equal(MatrixNorms.FrobeniusNorm(rankDeficient), diagnostics.FrobeniusNorm, 12);
        Assert.Equal(SingularValueDecomposition.Decompose(rankDeficient).LargestSingularValue, diagnostics.SpectralNorm, 12);
    }

    [Fact]
    public void ConditionDiagnostics_DistinguishFullRowRankFromRightNullSpace()
    {
        var wide = new DenseMatrix(new double[,]
        {
            { 1.0, 0.0, 1.0 },
            { 0.0, 1.0, 1.0 }
        });

        var diagnostics = MatrixConditionDiagnostics.Analyze(wide);

        Assert.True(diagnostics.IsFullRank);
        Assert.Equal(2, diagnostics.EstimatedRank);
        Assert.Equal(0, diagnostics.LeftNullity);
        Assert.Equal(1, diagnostics.RightNullity);
        Assert.InRange(
            diagnostics.ConditionNumber2,
            System.Math.Sqrt(3.0) - 1e-10,
            System.Math.Sqrt(3.0) + 1e-10);
        Assert.InRange(
            diagnostics.ReciprocalConditionNumber2,
            (1.0 / System.Math.Sqrt(3.0)) - 1e-10,
            (1.0 / System.Math.Sqrt(3.0)) + 1e-10);
    }
}
