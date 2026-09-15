using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Math.Toolkit.Tests;

public sealed class CholeskyFactorizationTests
{
    [Fact]
    public void Decompose_ReconstructsKnownSpdMatrixAndProtectsFactorState()
    {
        var matrix = ReferenceMatrix();
        var factorization = CholeskyFactorization.Decompose(matrix);
        var lower = factorization.LowerTriangularFactor;

        Assert.Equal(2.0, lower[0, 0], 12);
        Assert.Equal(6.0, lower[1, 0], 12);
        Assert.Equal(1.0, lower[1, 1], 12);
        Assert.Equal(-8.0, lower[2, 0], 12);
        Assert.Equal(5.0, lower[2, 1], 12);
        Assert.Equal(3.0, lower[2, 2], 12);

        lower[0, 0] = 999.0;
        Assert.Equal(2.0, factorization.LowerTriangularFactor[0, 0], 12);
    }

    [Fact]
    public void Solve_ReusesFactorizationForVectorAndMultipleRightHandSides()
    {
        var matrix = ReferenceMatrix();
        var factorization = CholeskyFactorization.Decompose(matrix);
        var expected = new[] { 1.0, 2.0, 3.0 };

        var solved = factorization.Solve(matrix.Multiply(expected));
        Assert.Equal(expected[0], solved[0], 11);
        Assert.Equal(expected[1], solved[1], 11);
        Assert.Equal(expected[2], solved[2], 11);

        var inverse = factorization.Solve(DenseMatrix.Identity(3));
        AssertIdentity(matrix.Multiply(inverse), 1e-10);
    }

    [Fact]
    public void Decompose_RejectsInvalidStructureAndNumericalBreakdown()
    {
        var nonSymmetric = new DenseMatrix(new double[,] { { 2.0, 1.0 }, { 0.0, 2.0 } });
        var indefinite = new DenseMatrix(new double[,] { { 1.0, 2.0 }, { 2.0, 1.0 } });
        var semidefinite = new DenseMatrix(new double[,] { { 1.0, 1.0 }, { 1.0, 1.0 } });

        Assert.Throws<ArgumentException>(() => CholeskyFactorization.Decompose(nonSymmetric));
        Assert.Throws<ArithmeticException>(() => CholeskyFactorization.Decompose(indefinite));
        Assert.Throws<ArithmeticException>(() => CholeskyFactorization.Decompose(semidefinite));
        Assert.Throws<ArgumentException>(() => CholeskyFactorization.Decompose(new DenseMatrix(2, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CholeskyFactorization.Decompose(DenseMatrix.Identity(2), relativeSymmetryTolerance: 1.0));
    }

    [Fact]
    public void Determinant_LogDeterminantAndInverseUseSameFactorization()
    {
        var matrix = ReferenceMatrix();
        var factorization = CholeskyFactorization.Decompose(matrix);

        Assert.Equal(36.0, factorization.Determinant(), 10);
        Assert.Equal(System.Math.Log(36.0), factorization.LogDeterminant(), 12);
        AssertIdentity(matrix.Multiply(factorization.Inverse()), 1e-10);
    }

    [Fact]
    public void PositiveDefiniteTolerance_IsScaleAwareAndConfigurable()
    {
        var matrix = new DenseMatrix(new double[,] { { 1e-20, 0.0 }, { 0.0, 1.0 } });
        Assert.Throws<ArithmeticException>(() => CholeskyFactorization.Decompose(matrix));

        var relaxed = CholeskyFactorization.Decompose(
            matrix,
            relativePositiveDefiniteTolerance: 1e-24);
        var solved = relaxed.Solve([1e-20, 1.0]);

        Assert.Equal(1.0, solved[0], 10);
        Assert.Equal(1.0, solved[1], 12);
        Assert.Equal(1e-24, relaxed.RelativePositiveDefiniteTolerance);
    }

    private static DenseMatrix ReferenceMatrix() => new(new double[,]
    {
        { 4.0, 12.0, -16.0 },
        { 12.0, 37.0, -43.0 },
        { -16.0, -43.0, 98.0 }
    });

    private static void AssertIdentity(DenseMatrix matrix, double tolerance)
    {
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
