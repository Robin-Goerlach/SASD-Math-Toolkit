using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

public sealed class SparseDiagnosticsTests
{
    [Fact]
    public void ResidualEuclideanNorm_MatchesKnownRectangularResidual()
    {
        var matrix = CsrMatrix.FromEntries(
            2,
            3,
            [
                new SparseMatrixEntry(0, 0, 1.0),
                new SparseMatrixEntry(0, 1, 2.0),
                new SparseMatrixEntry(1, 1, -1.0),
                new SparseMatrixEntry(1, 2, 3.0)
            ]);

        var norm = SparseMatrixDiagnostics.ResidualEuclideanNorm(
            matrix,
            [1.0, 2.0, 3.0],
            [6.0, 5.0]);

        Assert.InRange(norm, System.Math.Sqrt(5.0) - 1e-14, System.Math.Sqrt(5.0) + 1e-14);
    }

    [Fact]
    public void ResidualEuclideanNorm_UsesScaledAccumulationForLargeFiniteResiduals()
    {
        var zero = CsrMatrix.FromEntries(2, 2, Array.Empty<SparseMatrixEntry>());

        var norm = SparseMatrixDiagnostics.ResidualEuclideanNorm(
            zero,
            [0.0, 0.0],
            [1e308, 1e308]);

        Assert.True(double.IsFinite(norm));
        Assert.InRange(norm / 1e308, System.Math.Sqrt(2.0) - 1e-14, System.Math.Sqrt(2.0) + 1e-14);
    }

    [Fact]
    public void ResidualEuclideanNorm_ValidatesDimensionsFiniteInputsAndArithmeticRange()
    {
        var identity = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 1.0),
                new SparseMatrixEntry(1, 1, 1.0)
            ]);
        var negativeIdentity = CsrMatrix.FromEntries(
            1,
            1,
            [new SparseMatrixEntry(0, 0, -1.0)]);

        Assert.Throws<ArgumentException>(() =>
            SparseMatrixDiagnostics.ResidualEuclideanNorm(identity, [1.0], [1.0, 2.0]));
        Assert.Throws<ArgumentException>(() =>
            SparseMatrixDiagnostics.ResidualEuclideanNorm(identity, [1.0, 2.0], [1.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SparseMatrixDiagnostics.ResidualEuclideanNorm(identity, [1.0, 2.0], [1.0, double.NaN]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SparseMatrixDiagnostics.ResidualEuclideanNorm(identity, [1.0, double.PositiveInfinity], [1.0, 2.0]));
        Assert.Throws<ArithmeticException>(() =>
            SparseMatrixDiagnostics.ResidualEuclideanNorm(negativeIdentity, [1e308], [1e308]));
    }
}
