using Sasd.Numerics.Approximation;
using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Math.Toolkit.Tests;

/// <summary>
/// Behavioral regression tests for optimized hot paths. These deliberately avoid wall-clock
/// assertions, which are too noisy for shared CI runners. Instead they verify mathematical
/// equivalence and observable reductions such as basis-callback invocation counts.
/// </summary>
public sealed class PerformanceOptimizationTests
{
    [Fact]
    public void DenseMatrix_OptimizedMultiplyHandlesRectangularInputs()
    {
        var left = new DenseMatrix(new double[,]
        {
            { 1.0, 2.0, 3.0 },
            { 4.0, 5.0, 6.0 }
        });
        var right = new DenseMatrix(new double[,]
        {
            { 7.0, 8.0 },
            { 9.0, 10.0 },
            { 11.0, 12.0 }
        });

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
        var matrix = new DenseMatrix(new double[,]
        {
            { 4.0, 1.0, 2.0 },
            { 1.0, 5.0, 1.0 },
            { 2.0, 1.0, 6.0 }
        });
        var rightHandSides = new DenseMatrix(new double[,]
        {
            { 7.0, 1.0 },
            { 8.0, 2.0 },
            { 9.0, 3.0 }
        });
        var factorization = LuFactorization.Decompose(matrix);
        var batched = factorization.Solve(rightHandSides);

        for (var column = 0; column < rightHandSides.Columns; column++)
        {
            var rhs = new double[rightHandSides.Rows];
            for (var row = 0; row < rhs.Length; row++)
            {
                rhs[row] = rightHandSides[row, column];
            }

            var scalar = factorization.Solve(rhs);
            for (var row = 0; row < scalar.Length; row++)
            {
                Assert.Equal(scalar[row], batched[row, column], 12);
            }
        }
    }

    [Fact]
    public void LeastSquares_FitBasisEvaluatesEachBasisExactlyOncePerSample()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        var y = x.Select(value => 1.0 + (2.0 * value)).ToArray();
        var constantCalls = 0;
        var linearCalls = 0;

        double Constant(double value)
        {
            _ = value;
            constantCalls++;
            return 1.0;
        }

        double Linear(double value)
        {
            linearCalls++;
            return value;
        }

        var coefficients = LeastSquares.FitBasis(x, y, [Constant, Linear]);

        Assert.Equal(1.0, coefficients[0], 12);
        Assert.Equal(2.0, coefficients[1], 12);
        Assert.Equal(x.Length, constantCalls);
        Assert.Equal(x.Length, linearCalls);
    }
}
