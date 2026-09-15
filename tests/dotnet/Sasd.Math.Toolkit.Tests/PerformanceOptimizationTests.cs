using System.Numerics;
using Sasd.Numerics.Approximation;
using Sasd.Numerics.LinearAlgebra;
using Sasd.Numerics.Transforms;

namespace Sasd.Math.Toolkit.Tests;

/// <summary>
/// Behavioral regression tests for optimized hot paths. These deliberately avoid wall-clock
/// assertions, which are too noisy for shared CI runners. Instead they verify mathematical
/// equivalence, input ownership and observable reductions such as callback invocation counts.
/// </summary>
public sealed class PerformanceOptimizationTests
{
    [Fact]
    public void DenseMatrix_OptimizedMultiplyHandlesRectangularInputs()
    {
        var left = new DenseMatrix(new double[,] { { 1.0, 2.0, 3.0 }, { 4.0, 5.0, 6.0 } });
        var right = new DenseMatrix(new double[,] { { 7.0, 8.0 }, { 9.0, 10.0 }, { 11.0, 12.0 } });

        Assert.Equal(new[] { 14.0, 32.0 }, left.Multiply(new[] { 1.0, 2.0, 3.0 }));
        var product = left.Multiply(right);
        Assert.Equal(58.0, product[0, 0]);
        Assert.Equal(64.0, product[0, 1]);
        Assert.Equal(139.0, product[1, 0]);
        Assert.Equal(154.0, product[1, 1]);
    }

    [Fact]
    public void LuFactorization_BatchedSolveMatchesIndependentColumnSolves()
    {
        var matrix = new DenseMatrix(new double[,] { { 4.0, 1.0, 2.0 }, { 1.0, 5.0, 1.0 }, { 2.0, 1.0, 6.0 } });
        var rhs = new DenseMatrix(new double[,] { { 7.0, 1.0 }, { 8.0, 2.0 }, { 9.0, 3.0 } });
        var factorization = LuFactorization.Decompose(matrix);
        var batched = factorization.Solve(rhs);

        for (var column = 0; column < rhs.Columns; column++)
        {
            var vector = new double[rhs.Rows];
            for (var row = 0; row < vector.Length; row++) vector[row] = rhs[row, column];
            var scalar = factorization.Solve(vector);
            for (var row = 0; row < scalar.Length; row++) Assert.Equal(scalar[row], batched[row, column], 12);
        }
    }

    [Fact]
    public void LeastSquares_FitBasisEvaluatesEachBasisExactlyOncePerSample()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        var y = x.Select(value => 1.0 + (2.0 * value)).ToArray();
        var constantCalls = 0;
        var linearCalls = 0;

        double Constant(double value) { _ = value; constantCalls++; return 1.0; }
        double Linear(double value) { linearCalls++; return value; }

        var coefficients = LeastSquares.FitBasis(x, y, [Constant, Linear]);
        Assert.Equal(1.0, coefficients[0], 12);
        Assert.Equal(2.0, coefficients[1], 12);
        Assert.Equal(x.Length, constantCalls);
        Assert.Equal(x.Length, linearCalls);
    }

    [Fact]
    public void FftOptimizedPathsPreserveCallerOwnedInputs()
    {
        Complex[] complex = [Complex.One, new(2.0, -1.0), new(-0.5, 0.25), new(3.0, 0.0)];
        var complexSnapshot = (Complex[])complex.Clone();
        var spectrum = FastFourierTransform.Forward(complex);
        var restored = FastFourierTransform.Inverse(spectrum);

        Assert.Equal(complexSnapshot, complex);
        for (var i = 0; i < complex.Length; i++)
        {
            Assert.InRange(Complex.Abs(restored[i] - complex[i]), 0.0, 1e-10);
        }

        double[] left = [1.0, 2.0, 3.0];
        double[] right = [0.5, -1.0];
        var leftSnapshot = (double[])left.Clone();
        var rightSnapshot = (double[])right.Clone();
        _ = FastFourierTransform.ConvolveReal(left, right);
        _ = FastFourierTransform.CrossCorrelateReal(left, right);
        Assert.Equal(leftSnapshot, left);
        Assert.Equal(rightSnapshot, right);
    }
}
