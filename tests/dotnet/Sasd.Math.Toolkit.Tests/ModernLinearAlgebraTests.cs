using Sasd.Numerics.Approximation;
using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Math.Toolkit.Tests;

public sealed class ModernLinearAlgebraTests
{
    [Fact]
    public void QrFactorization_ReconstructsTallMatrixAndProducesOrthonormalThinQ()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1.0, 2.0, 3.0 },
            { 4.0, 5.0, 7.0 },
            { 2.0, 6.0, 4.0 },
            { 3.0, 1.0, 5.0 },
            { 5.0, 2.0, 1.0 }
        });

        var factorization = QrFactorization.Decompose(matrix);
        var q = factorization.ThinOrthogonalFactor;
        var r = factorization.UpperTriangularFactor;
        var reconstructed = q.Multiply(r);

        Assert.True(factorization.IsFullColumnRank);
        Assert.Equal(3, factorization.EstimatedRank);

        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                Assert.InRange(
                    reconstructed[row, column],
                    matrix[row, column] - 1e-10,
                    matrix[row, column] + 1e-10);
            }
        }

        for (var left = 0; left < q.Columns; left++)
        {
            for (var right = 0; right < q.Columns; right++)
            {
                var dot = 0.0;
                for (var row = 0; row < q.Rows; row++)
                {
                    dot += q[row, left] * q[row, right];
                }

                var expected = left == right ? 1.0 : 0.0;
                Assert.InRange(dot, expected - 1e-10, expected + 1e-10);
            }
        }
    }

    [Fact]
    public void QrFactorization_SolvesOverdeterminedLeastSquaresSystem()
    {
        var design = new DenseMatrix(new double[,]
        {
            { 1.0, 0.0 },
            { 1.0, 1.0 },
            { 1.0, 2.0 },
            { 1.0, 3.0 },
            { 1.0, 4.0 }
        });

        var factorization = QrFactorization.Decompose(design);
        var solution = factorization.SolveLeastSquares([1.0, 3.0, 5.0, 7.0, 9.0]);

        Assert.Equal(1.0, solution[0], 12);
        Assert.Equal(2.0, solution[1], 12);
    }

    [Fact]
    public void QrFactorization_ReportsRankDeficiencyAndValidatesModernSolverBoundary()
    {
        var rankDeficient = new DenseMatrix(new double[,]
        {
            { 1.0, 2.0 },
            { 2.0, 4.0 },
            { 3.0, 6.0 }
        });

        var factorization = QrFactorization.Decompose(rankDeficient);
        Assert.False(factorization.IsFullColumnRank);
        Assert.Equal(1, factorization.EstimatedRank);
        Assert.Throws<ArithmeticException>(() => factorization.SolveLeastSquares([1.0, 2.0, 3.0]));

        Assert.Throws<ArgumentException>(() =>
            QrFactorization.Decompose(new DenseMatrix(2, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QrFactorization.Decompose(DenseMatrix.Identity(2), relativeRankTolerance: 1.0));
        Assert.Throws<ArgumentException>(() =>
            QrFactorization.Decompose(DenseMatrix.Identity(2)).SolveLeastSquares([1.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            QrFactorization.Decompose(DenseMatrix.Identity(2)).SolveLeastSquares([1.0, double.NaN]));
    }

    [Fact]
    public void LeastSquares_GeneralBasisUsesQrAndRejectsRankDeficientDesign()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var y = x.Select(value => 3.0 - (0.5 * value)).ToArray();

        var coefficients = LeastSquares.FitBasis(
            x,
            y,
            [static _ => 1.0, static value => value]);

        Assert.Equal(3.0, coefficients[0], 12);
        Assert.Equal(-0.5, coefficients[1], 12);

        Assert.Throws<ArithmeticException>(() => LeastSquares.FitBasis(
            x,
            y,
            [static _ => 1.0, static _ => 2.0]));
    }
}
