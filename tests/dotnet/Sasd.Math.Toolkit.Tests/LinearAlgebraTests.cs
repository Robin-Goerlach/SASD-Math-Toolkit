using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Math.Toolkit.Tests;

public sealed class LinearAlgebraTests
{
    [Fact]
    public void Determinant_IsCorrect()
    {
        var matrix = new DenseMatrix(new double[,] { { 4.0, 7.0 }, { 2.0, 6.0 } });
        Assert.Equal(10.0, LinearSystemSolvers.Determinant(matrix), 10);
    }

    [Fact]
    public void GaussianSolve_IsCorrect()
    {
        var matrix = new DenseMatrix(new double[,] { { 2.0, 1.0 }, { 5.0, 7.0 } });
        var result = LinearSystemSolvers.SolveGaussian(matrix, new[] { 11.0, 13.0 });
        Assert.Equal(64.0 / 9.0, result[0], 10);
        Assert.Equal(-29.0 / 9.0, result[1], 10);
    }

    [Fact]
    public void LuFactorization_ReconstructsPermutedMatrix()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 0.0, 2.0, 1.0 },
            { 1.0, -2.0, -3.0 },
            { 2.0, 3.0, 1.0 }
        });

        var factorization = LinearSystemSolvers.FactorizeLu(matrix);
        var reconstructed = factorization.LowerTriangular.Multiply(factorization.UpperTriangular);
        var permutation = factorization.Permutation;

        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                Assert.Equal(matrix[permutation[row], column], reconstructed[row, column], 10);
            }
        }
    }

    [Fact]
    public void LuFactorization_SolvesSystemThatRequiresPivoting()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 0.0, 2.0, 1.0 },
            { 1.0, -2.0, -3.0 },
            { 2.0, 3.0, 1.0 }
        });

        // The right-hand side was generated from the known solution [1, 2, -1].
        var factorization = LuFactorization.Decompose(matrix);
        var solution = factorization.Solve(new[] { 3.0, 0.0, 7.0 });

        Assert.Equal(1.0, solution[0], 10);
        Assert.Equal(2.0, solution[1], 10);
        Assert.Equal(-1.0, solution[2], 10);
        Assert.InRange(LinearSystemSolvers.ResidualInfinityNorm(matrix, solution, new[] { 3.0, 0.0, 7.0 }), 0.0, 1e-10);
    }

    [Fact]
    public void LuFactorization_ComputesDeterminantAndInverse()
    {
        var matrix = new DenseMatrix(new double[,] { { 4.0, 7.0 }, { 2.0, 6.0 } });
        var factorization = LuFactorization.Decompose(matrix);

        Assert.Equal(10.0, factorization.Determinant(), 10);

        var product = matrix.Multiply(factorization.Inverse());
        AssertIdentity(product, 1e-10);

        // The public convenience API should use the same reusable factorization path.
        AssertIdentity(matrix.Multiply(LinearSystemSolvers.Inverse(matrix)), 1e-10);
    }

    [Fact]
    public void LuFactorization_RejectsSingularMatrix()
    {
        var singular = new DenseMatrix(new double[,] { { 1.0, 2.0 }, { 2.0, 4.0 } });
        Assert.Throws<ArithmeticException>(() => LuFactorization.Decompose(singular));
    }

    [Fact]
    public void PowerMethod_FindsDominantEigenvalue()
    {
        var matrix = new DenseMatrix(new double[,] { { 2.0, 0.0 }, { 0.0, 1.0 } });
        var result = EigenSolvers.PowerMethod(matrix);
        Assert.True(result.Converged);
        Assert.InRange(result.Value.Eigenvalue, 2.0 - 1e-8, 2.0 + 1e-8);
    }

    [Fact]
    public void InversePowerMethod_ReusesLuAndFindsSmallestEigenvalue()
    {
        var matrix = new DenseMatrix(new double[,] { { 4.0, 0.0 }, { 0.0, 1.0 } });
        var result = EigenSolvers.InversePowerMethod(matrix);

        Assert.True(result.Converged);
        Assert.InRange(result.Value.Eigenvalue, 1.0 - 1e-8, 1.0 + 1e-8);
        Assert.InRange(result.Residual, 0.0, 1e-8);
    }

    [Fact]
    public void WielandtDeflation_ReplacesKnownEigenvalueByZero()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 4.0, 1.0, 0.0 },
            { 1.0, 3.0, 0.0 },
            { 0.0, 0.0, 1.0 }
        });

        var dominant = EigenSolvers.PowerMethod(matrix, tolerance: 1e-12);
        Assert.True(dominant.Converged);

        var deflation = WielandtDeflation.Create(matrix, dominant.Value);
        var mapped = deflation.DeflatedMatrix.Multiply(deflation.RemovedEigenpair.Eigenvector);

        Assert.InRange(EuclideanNorm(mapped), 0.0, 1e-9);
    }

    [Fact]
    public void WielandtSecondEigenpair_FindsSecondEigenvalueAndOriginalEigenvector()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 4.0, 1.0, 0.0 },
            { 1.0, 3.0, 0.0 },
            { 0.0, 0.0, 1.0 }
        });

        var result = EigenSolvers.WielandtSecondEigenpair(matrix, tolerance: 1e-11);
        var expected = (7.0 - System.Math.Sqrt(5.0)) / 2.0;

        Assert.True(result.Converged);
        Assert.InRange(result.Value.Eigenvalue, expected - 1e-8, expected + 1e-8);
        Assert.InRange(result.Residual, 0.0, 1e-8);
    }

    [Fact]
    public void CyclicJacobi_FindsCompleteSymmetricEigensystem()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 4.0, 1.0, 0.0 },
            { 1.0, 3.0, 0.0 },
            { 0.0, 0.0, 1.0 }
        });

        var result = EigenSolvers.CyclicJacobi(matrix);
        Assert.True(result.Converged);

        var expectedLargest = (7.0 + System.Math.Sqrt(5.0)) / 2.0;
        var expectedMiddle = (7.0 - System.Math.Sqrt(5.0)) / 2.0;
        var values = result.Value.Eigenvalues;

        Assert.InRange(values[0], expectedLargest - 1e-10, expectedLargest + 1e-10);
        Assert.InRange(values[1], expectedMiddle - 1e-10, expectedMiddle + 1e-10);
        Assert.InRange(values[2], 1.0 - 1e-10, 1.0 + 1e-10);

        for (var index = 0; index < result.Value.Size; index++)
        {
            var pair = result.Value.GetEigenpair(index);
            Assert.InRange(EigenResidual(matrix, pair), 0.0, 1e-9);
        }

        AssertOrthonormalColumns(result.Value.Eigenvectors, 1e-10);
    }

    [Fact]
    public void CyclicJacobi_RejectsNonSymmetricMatrix()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1.0, 2.0 },
            { 0.0, 1.0 }
        });

        Assert.Throws<ArgumentException>(() => EigenSolvers.CyclicJacobi(matrix));
    }

    private static double EigenResidual(DenseMatrix matrix, Eigenpair eigenpair)
    {
        var multiplied = matrix.Multiply(eigenpair.Eigenvector);
        var residual = new double[multiplied.Length];
        for (var i = 0; i < residual.Length; i++)
        {
            residual[i] = multiplied[i] - (eigenpair.Eigenvalue * eigenpair.Eigenvector[i]);
        }

        return EuclideanNorm(residual);
    }

    private static double EuclideanNorm(IReadOnlyList<double> vector)
    {
        var sum = 0.0;
        for (var i = 0; i < vector.Count; i++)
        {
            sum += vector[i] * vector[i];
        }

        return System.Math.Sqrt(sum);
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

    private static void AssertIdentity(DenseMatrix matrix, double tolerance)
    {
        Assert.Equal(matrix.Rows, matrix.Columns);
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                var expected = row == column ? 1.0 : 0.0;
                Assert.InRange(matrix[row, column], expected - tolerance, expected + tolerance);
            }
        }
    }
}
