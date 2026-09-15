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

        AssertOrthonormalColumns(q, 1e-10);
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
    public void SingularValueDecomposition_ReconstructsTallMatrixAndOrdersSingularValues()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1.0, 0.0 },
            { 0.0, 1.0 },
            { 1.0, 1.0 }
        });

        var svd = SingularValueDecomposition.Decompose(matrix);
        var singularValues = svd.SingularValues;
        var u = svd.LeftSingularVectors;
        var v = svd.RightSingularVectors;

        Assert.Equal(2, svd.Components);
        Assert.Equal(2, svd.EstimatedRank);
        Assert.True(svd.IsFullRank);
        Assert.InRange(singularValues[0], System.Math.Sqrt(3.0) - 1e-10, System.Math.Sqrt(3.0) + 1e-10);
        Assert.InRange(singularValues[1], 1.0 - 1e-10, 1.0 + 1e-10);
        Assert.InRange(svd.ConditionNumber, System.Math.Sqrt(3.0) - 1e-10, System.Math.Sqrt(3.0) + 1e-10);
        Assert.Equal(singularValues[0], svd.GetSingularValue(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => svd.GetSingularValue(2));

        AssertOrthonormalColumns(u, 1e-10);
        AssertOrthonormalColumns(v, 1e-10);
        AssertSvdReconstructs(matrix, u, singularValues, v, 1e-10);

        // Public factor data must be defensive.
        singularValues[0] = 999.0;
        Assert.NotEqual(999.0, svd.GetSingularValue(0));
        u[0, 0] = 999.0;
        Assert.NotEqual(999.0, svd.LeftSingularVectors[0, 0]);
    }

    [Fact]
    public void SingularValueDecomposition_WideSystemReturnsMinimumNormSolution()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1.0, 0.0, 1.0 },
            { 0.0, 1.0, 1.0 }
        });

        var svd = SingularValueDecomposition.Decompose(matrix);
        var solution = svd.SolveLeastSquares([1.0, 1.0]);

        Assert.Equal(2, svd.EstimatedRank);
        Assert.InRange(solution[0], (1.0 / 3.0) - 1e-10, (1.0 / 3.0) + 1e-10);
        Assert.InRange(solution[1], (1.0 / 3.0) - 1e-10, (1.0 / 3.0) + 1e-10);
        Assert.InRange(solution[2], (2.0 / 3.0) - 1e-10, (2.0 / 3.0) + 1e-10);

        var reconstructedRightHandSide = matrix.Multiply(solution);
        Assert.InRange(reconstructedRightHandSide[0], 1.0 - 1e-10, 1.0 + 1e-10);
        Assert.InRange(reconstructedRightHandSide[1], 1.0 - 1e-10, 1.0 + 1e-10);
    }

    [Fact]
    public void SingularValueDecomposition_RankDeficientSolveAndPseudoInverseUseSameThreshold()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1.0, 2.0 },
            { 2.0, 4.0 },
            { 3.0, 6.0 }
        });

        var svd = SingularValueDecomposition.Decompose(matrix);
        var solution = svd.SolveLeastSquares([1.0, 2.0, 3.0]);
        var pseudoInverse = svd.PseudoInverse();
        var reconstructed = matrix.Multiply(pseudoInverse).Multiply(matrix);

        Assert.Equal(1, svd.EstimatedRank);
        Assert.False(svd.IsFullRank);
        Assert.True(double.IsPositiveInfinity(svd.ConditionNumber));
        Assert.Equal(0.0, svd.ReciprocalConditionNumber);
        Assert.InRange(solution[0], 0.2 - 1e-10, 0.2 + 1e-10);
        Assert.InRange(solution[1], 0.4 - 1e-10, 0.4 + 1e-10);
        AssertMatrixClose(matrix, reconstructed, 1e-9);
    }

    [Fact]
    public void SingularValueDecomposition_HandlesZeroMatrixAndConfigurableRankThreshold()
    {
        var zero = SingularValueDecomposition.Decompose(new DenseMatrix(2, 3));
        Assert.Equal(0, zero.EstimatedRank);
        Assert.Equal(0.0, zero.LargestSingularValue);
        Assert.True(double.IsPositiveInfinity(zero.ConditionNumber));
        Assert.Equal(new[] { 0.0, 0.0, 0.0 }, zero.SolveLeastSquares([4.0, -2.0]));
        AssertMatrixClose(new DenseMatrix(3, 2), zero.PseudoInverse(), 0.0);

        var scaled = new DenseMatrix(new double[,]
        {
            { 10.0, 0.0 },
            { 0.0, 1e-3 }
        });
        var truncated = SingularValueDecomposition.Decompose(scaled, relativeRankTolerance: 1e-2);
        Assert.Equal(1, truncated.EstimatedRank);
        Assert.InRange(truncated.RankThreshold, 0.1 - 1e-14, 0.1 + 1e-14);
        Assert.True(double.IsPositiveInfinity(truncated.ConditionNumber));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SingularValueDecomposition.Decompose(scaled, relativeRankTolerance: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SingularValueDecomposition.Decompose(scaled, relativeOrthogonalityTolerance: 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SingularValueDecomposition.Decompose(scaled, maximumSweeps: 0));
        Assert.Throws<ArgumentException>(() => truncated.SolveLeastSquares([1.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => truncated.SolveLeastSquares([1.0, double.NaN]));
    }

    [Fact]
    public void LeastSquares_GeneralBasisUsesQrAndSvdFallbacks()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var y = x.Select(value => 3.0 - (0.5 * value)).ToArray();

        var coefficients = LeastSquares.FitBasis(
            x,
            y,
            [static _ => 1.0, static value => value]);

        Assert.Equal(3.0, coefficients[0], 12);
        Assert.Equal(-0.5, coefficients[1], 12);

        // Two dependent basis functions define infinitely many coefficient vectors. The SVD
        // fallback returns the minimum-norm vector satisfying c0 + 2*c1 = 3.
        var rankDeficient = LeastSquares.FitBasis(
            x,
            Enumerable.Repeat(3.0, x.Length).ToArray(),
            [static _ => 1.0, static _ => 2.0]);

        Assert.InRange(rankDeficient[0], 0.6 - 1e-10, 0.6 + 1e-10);
        Assert.InRange(rankDeficient[1], 1.2 - 1e-10, 1.2 + 1e-10);

        // A wide one-sample/two-basis design is also well-defined through the minimum-norm SVD path.
        var underdetermined = LeastSquares.FitBasis(
            [0.0],
            [2.0],
            [static _ => 1.0, static _ => 1.0]);

        Assert.InRange(underdetermined[0], 1.0 - 1e-10, 1.0 + 1e-10);
        Assert.InRange(underdetermined[1], 1.0 - 1e-10, 1.0 + 1e-10);
    }

    private static void AssertSvdReconstructs(
        DenseMatrix original,
        DenseMatrix u,
        IReadOnlyList<double> singularValues,
        DenseMatrix v,
        double tolerance)
    {
        for (var row = 0; row < original.Rows; row++)
        {
            for (var column = 0; column < original.Columns; column++)
            {
                var reconstructed = 0.0;
                for (var component = 0; component < singularValues.Count; component++)
                {
                    reconstructed += u[row, component]
                        * singularValues[component]
                        * v[column, component];
                }

                Assert.InRange(
                    reconstructed,
                    original[row, column] - tolerance,
                    original[row, column] + tolerance);
            }
        }
    }

    private static void AssertOrthonormalColumns(DenseMatrix matrix, double tolerance)
    {
        for (var left = 0; left < matrix.Columns; left++)
        {
            for (var right = 0; right < matrix.Columns; right++)
            {
                var dot = 0.0;
                for (var row = 0; row < matrix.Rows; row++)
                {
                    dot += matrix[row, left] * matrix[row, right];
                }

                var expected = left == right ? 1.0 : 0.0;
                Assert.InRange(dot, expected - tolerance, expected + tolerance);
            }
        }
    }

    private static void AssertMatrixClose(DenseMatrix expected, DenseMatrix actual, double tolerance)
    {
        Assert.Equal(expected.Rows, actual.Rows);
        Assert.Equal(expected.Columns, actual.Columns);

        for (var row = 0; row < expected.Rows; row++)
        {
            for (var column = 0; column < expected.Columns; column++)
            {
                Assert.InRange(
                    actual[row, column],
                    expected[row, column] - tolerance,
                    expected[row, column] + tolerance);
            }
        }
    }
}
