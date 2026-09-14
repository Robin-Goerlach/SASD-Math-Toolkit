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
    public void PowerMethod_FindsDominantEigenvalue()
    {
        var matrix = new DenseMatrix(new double[,] { { 2.0, 0.0 }, { 0.0, 1.0 } });
        var result = EigenSolvers.PowerMethod(matrix);
        Assert.True(result.Converged);
        Assert.InRange(result.Value.Eigenvalue, 2.0 - 1e-8, 2.0 + 1e-8);
    }
}
