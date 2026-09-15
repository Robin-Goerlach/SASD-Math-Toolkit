using Sasd.Numerics.LinearAlgebra;
using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

public sealed class SparseMatrixTests
{
    [Fact]
    public void FromEntries_CanonicalizesOrderCombinesDuplicatesAndRemovesCancellation()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(2, 1, 5.0),
                new SparseMatrixEntry(0, 2, 3.0),
                new SparseMatrixEntry(0, 0, 1.0),
                new SparseMatrixEntry(0, 2, -1.0),
                new SparseMatrixEntry(1, 1, 4.0),
                new SparseMatrixEntry(2, 1, -5.0),
                new SparseMatrixEntry(1, 2, 0.0)
            ]);

        Assert.Equal(3, matrix.NonZeroCount);
        Assert.Equal(1.0 / 3.0, matrix.Density, 12);
        Assert.Equal(new[] { 0, 2, 3, 3 }, matrix.CopyRowPointers());
        Assert.Equal(new[] { 0, 2, 1 }, matrix.CopyColumnIndices());
        Assert.Equal(new[] { 1.0, 2.0, 4.0 }, matrix.CopyValues());
        Assert.Equal(2.0, matrix.GetValue(0, 2));
        Assert.Equal(0.0, matrix.GetValue(2, 1));

        var row = matrix.GetRowEntries(0);
        Assert.Equal(new SparseMatrixEntry(0, 0, 1.0), row[0]);
        Assert.Equal(new SparseMatrixEntry(0, 2, 2.0), row[1]);
    }

    [Fact]
    public void FromCompressedRows_RequiresCanonicalDefensiveStorage()
    {
        int[] rowPointers = [0, 2, 3];
        int[] columns = [0, 2, 1];
        double[] values = [1.0, 2.0, 3.0];
        var matrix = CsrMatrix.FromCompressedRows(2, 3, rowPointers, columns, values);

        rowPointers[1] = 0;
        columns[0] = 2;
        values[0] = 99.0;

        Assert.Equal(new[] { 0, 2, 3 }, matrix.CopyRowPointers());
        Assert.Equal(new[] { 0, 2, 1 }, matrix.CopyColumnIndices());
        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, matrix.CopyValues());

        Assert.Throws<ArgumentException>(() =>
            CsrMatrix.FromCompressedRows(2, 3, [0, 2, 3], [2, 0, 1], [1.0, 2.0, 3.0]));
        Assert.Throws<ArgumentException>(() =>
            CsrMatrix.FromCompressedRows(2, 3, [0, 3, 2], [0, 1], [1.0, 2.0]));
        Assert.Throws<ArgumentException>(() =>
            CsrMatrix.FromCompressedRows(2, 3, [0, 1, 2], [0, 1], [1.0, 0.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsrMatrix.FromCompressedRows(2, 3, [0, 1, 2], [0, 3], [1.0, 2.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsrMatrix.FromCompressedRows(2, 3, [0, 1, 2], [0, 1], [1.0, double.NaN]));
    }

    [Fact]
    public void FromDense_RoundTripsAndUsesExplicitAbsoluteSparsificationThreshold()
    {
        var dense = new DenseMatrix(new double[,]
        {
            { 4.0, 1e-9, 0.0 },
            { -2.0, 0.0, 3.0 }
        });

        var exact = CsrMatrix.FromDense(dense);
        AssertMatrixClose(dense, exact.ToDense(), 0.0);
        Assert.Equal(4, exact.NonZeroCount);

        var thresholded = CsrMatrix.FromDense(dense, absoluteZeroTolerance: 1e-8);
        Assert.Equal(3, thresholded.NonZeroCount);
        Assert.Equal(0.0, thresholded.GetValue(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsrMatrix.FromDense(dense, absoluteZeroTolerance: double.NaN));
    }

    [Fact]
    public void Multiply_MatchesDenseAndSupportsAllocationFreeOverlappingBuffers()
    {
        var dense = new DenseMatrix(new double[,]
        {
            { 4.0, 0.0, -1.0 },
            { 0.0, 2.0, 0.0 },
            { 3.0, 0.0, 5.0 }
        });
        var sparse = CsrMatrix.FromDense(dense);
        double[] vector = [1.0, 2.0, -1.0];

        Assert.Equal(dense.Multiply(vector), sparse.Multiply(vector));

        double[] inPlace = [1.0, 2.0, -1.0];
        sparse.Multiply(inPlace.AsSpan(), inPlace.AsSpan());
        Assert.Equal(new[] { 5.0, 4.0, -2.0 }, inPlace);

        Assert.Throws<ArgumentException>(() => sparse.Multiply([1.0, 2.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => sparse.Multiply([1.0, double.NaN, 3.0]));

        var overflowing = CsrMatrix.FromEntries(1, 1, [new SparseMatrixEntry(0, 0, 1e308)]);
        Assert.Throws<ArithmeticException>(() => overflowing.Multiply([2.0]));
    }

    [Fact]
    public void Transpose_IsCanonicalAndRoundTripsWithoutDenseIntermediate()
    {
        var matrix = CsrMatrix.FromEntries(
            2,
            4,
            [
                new SparseMatrixEntry(1, 3, 7.0),
                new SparseMatrixEntry(0, 2, -2.0),
                new SparseMatrixEntry(1, 0, 5.0),
                new SparseMatrixEntry(0, 0, 1.0)
            ]);

        var transposed = matrix.Transpose();
        Assert.Equal(4, transposed.Rows);
        Assert.Equal(2, transposed.Columns);
        Assert.Equal(new[] { 0, 2, 2, 3, 4 }, transposed.CopyRowPointers());
        Assert.Equal(new[] { 0, 1, 0, 1 }, transposed.CopyColumnIndices());
        Assert.Equal(new[] { 1.0, 5.0, -2.0, 7.0 }, transposed.CopyValues());

        AssertMatrixClose(matrix.ToDense(), transposed.Transpose().ToDense(), 0.0);
    }

    [Fact]
    public void SparseNorms_MatchDenseDefinitionsAndAvoidPrematureFrobeniusOverflow()
    {
        var dense = new DenseMatrix(new double[,]
        {
            { 1.0, -2.0, 0.0 },
            { 0.0, 3.0, 4.0 }
        });
        var sparse = CsrMatrix.FromDense(dense);

        Assert.Equal(MatrixNorms.MaxAbsoluteEntry(dense), SparseMatrixNorms.MaxAbsoluteEntry(sparse), 12);
        Assert.Equal(MatrixNorms.OneNorm(dense), SparseMatrixNorms.OneNorm(sparse), 12);
        Assert.Equal(MatrixNorms.InfinityNorm(dense), SparseMatrixNorms.InfinityNorm(sparse), 12);
        Assert.Equal(MatrixNorms.FrobeniusNorm(dense), SparseMatrixNorms.FrobeniusNorm(sparse), 12);

        var large = CsrMatrix.FromEntries(
            2,
            2,
            [new SparseMatrixEntry(0, 0, 1e308), new SparseMatrixEntry(1, 1, 1e308)]);
        var frobenius = SparseMatrixNorms.FrobeniusNorm(large);
        Assert.True(double.IsFinite(frobenius));
        Assert.InRange(frobenius, 1.4142135623730947e308, 1.4142135623730954e308);
    }

    [Fact]
    public void ZeroMatrixAndInvalidCoordinateAssemblyHaveExplicitSemantics()
    {
        var zero = CsrMatrix.FromEntries(3, 5, Array.Empty<SparseMatrixEntry>());
        Assert.Equal(0, zero.NonZeroCount);
        Assert.Equal(0.0, zero.Density);
        Assert.Equal(new[] { 0, 0, 0, 0 }, zero.CopyRowPointers());
        Assert.Equal(new[] { 0.0, 0.0, 0.0 }, zero.Multiply([1.0, 2.0, 3.0, 4.0, 5.0]));
        Assert.Equal(0.0, SparseMatrixNorms.OneNorm(zero));
        Assert.Equal(0.0, SparseMatrixNorms.InfinityNorm(zero));
        Assert.Equal(0.0, SparseMatrixNorms.FrobeniusNorm(zero));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsrMatrix.FromEntries(2, 2, [new SparseMatrixEntry(2, 0, 1.0)]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CsrMatrix.FromEntries(2, 2, [new SparseMatrixEntry(0, 0, double.PositiveInfinity)]));
        Assert.Throws<ArithmeticException>(() =>
            CsrMatrix.FromEntries(
                1,
                1,
                [new SparseMatrixEntry(0, 0, 1e308), new SparseMatrixEntry(0, 0, 1e308)]));
    }

    private static void AssertMatrixClose(DenseMatrix expected, DenseMatrix actual, double tolerance)
    {
        Assert.Equal(expected.Rows, actual.Rows);
        Assert.Equal(expected.Columns, actual.Columns);

        for (var row = 0; row < expected.Rows; row++)
        {
            for (var column = 0; column < expected.Columns; column++)
            {
                Assert.InRange(actual[row, column], expected[row, column] - tolerance, expected[row, column] + tolerance);
            }
        }
    }
}
