using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra;

namespace Sasd.Math.Toolkit.Tests;

public sealed class EigenvalueQualityTests
{
    [Fact]
    public void Eigenpair_CopiesCallerVectorAndRejectsNonFinitePublicValues()
    {
        var source = new[] { 1.0, 0.0 };
        var pair = new Eigenpair(2.0, source);

        source[0] = 99.0;
        Assert.Equal(1.0, pair[0]);
        Assert.Equal(2, pair.Dimension);

        var arrayCopy = pair.Eigenvector;
        arrayCopy[0] = -5.0;
        Assert.Equal(1.0, pair[0]);
        Assert.Equal(1.0, pair.Components[0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Eigenpair(double.NaN, new[] { 1.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Eigenpair(1.0, new[] { double.PositiveInfinity }));
    }

    [Fact]
    public void PowerMethod_NormalizesVeryLargeFiniteInitialVectorWithoutOverflow()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 3.0, 0.0 },
            { 0.0, 1.0 }
        });

        var result = EigenSolvers.PowerMethod(
            matrix,
            initialVector: new[] { 1.0e308, 1.0e308 },
            tolerance: 1e-12,
            maximumIterations: 500);

        Assert.True(result.Converged);
        Assert.InRange(result.Value.Eigenvalue, 3.0 - 1e-10, 3.0 + 1e-10);
        Assert.InRange(result.Residual, 0.0, 1e-10);
    }

    [Fact]
    public void PowerMethod_ValidatesInitialVectorAndReportsIterationLimit()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 2.0, 0.0 },
            { 0.0, 1.0 }
        });

        Assert.Throws<ArgumentException>(() =>
            EigenSolvers.PowerMethod(matrix, new[] { 0.0, 0.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EigenSolvers.PowerMethod(matrix, new[] { double.NaN, 1.0 }));
        Assert.Throws<ArgumentException>(() =>
            EigenSolvers.PowerMethod(matrix, new[] { 1.0 }));

        var limited = EigenSolvers.PowerMethod(
            matrix,
            initialVector: new[] { 1.0, 1.0 },
            tolerance: 1e-30,
            maximumIterations: 1);

        Assert.False(limited.Converged);
        Assert.Equal(IterationStatus.MaximumIterationsReached, limited.Status);
        Assert.Equal(1, limited.Iterations);
    }

    [Fact]
    public void InversePowerMethod_RespectsExplicitPivotTolerance()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 1e-12, 0.0 },
            { 0.0, 2.0 }
        });

        Assert.Throws<ArithmeticException>(() =>
            EigenSolvers.InversePowerMethod(
                matrix,
                initialVector: new[] { 1.0, 1.0 },
                tolerance: 1e-14,
                maximumIterations: 100,
                pivotTolerance: 1e-10));

        var result = EigenSolvers.InversePowerMethod(
            matrix,
            initialVector: new[] { 1.0, 1.0 },
            tolerance: 1e-14,
            maximumIterations: 100,
            pivotTolerance: 1e-15);

        Assert.True(result.Converged);
        Assert.InRange(result.Value.Eigenvalue, 1e-12 - 1e-14, 1e-12 + 1e-14);
        Assert.InRange(result.Residual, 0.0, 1e-14);
    }

    [Fact]
    public void EigenpairResidualNorm_MeasuresEquationDefectAndValidatesDimensions()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 2.0, 0.0 },
            { 0.0, 1.0 }
        });

        var exact = new Eigenpair(2.0, new[] { 1.0, 0.0 });
        var inexact = new Eigenpair(2.0, new[] { 1.0, 1.0 });

        Assert.Equal(0.0, EigenSolvers.EigenpairResidualNorm(matrix, exact), 12);
        Assert.Equal(1.0, EigenSolvers.EigenpairResidualNorm(matrix, inexact), 12);

        var wrongDimension = new Eigenpair(2.0, new[] { 1.0, 0.0, 0.0 });
        Assert.Throws<ArgumentException>(() => EigenSolvers.EigenpairResidualNorm(matrix, wrongDimension));
    }

    [Fact]
    public void WielandtDeflation_RejectsAnInaccurateClaimedEigenpair()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 3.0, 0.0 },
            { 0.0, 1.0 }
        });

        var inaccurate = new Eigenpair(3.0, new[] { 1.0, 1.0 });

        Assert.Throws<ArgumentException>(() =>
            WielandtDeflation.Create(matrix, inaccurate, residualTolerance: 1e-8));
    }

    [Fact]
    public void CyclicJacobi_DiagonalMatrixConvergesWithoutSweepsAndResultIsDefensive()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 3.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0 },
            { 0.0, 0.0, 2.0 }
        });

        var result = EigenSolvers.CyclicJacobi(matrix);

        Assert.True(result.Converged);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0.0, result.Residual, 12);
        Assert.Equal(3.0, result.Value.GetEigenvalue(0), 12);
        Assert.Equal(2.0, result.Value.GetEigenvalue(1), 12);
        Assert.Equal(1.0, result.Value.GetEigenvalue(2), 12);

        var values = result.Value.Eigenvalues;
        values[0] = 99.0;
        Assert.Equal(3.0, result.Value.GetEigenvalue(0), 12);

        var vector = result.Value.GetEigenvector(0);
        vector[0] = 0.0;
        Assert.Equal(1.0, result.Value.GetEigenvector(0)[0], 12);
    }

    [Fact]
    public void CyclicJacobi_ReportsMaximumSweepLimitWithoutPretendingConvergence()
    {
        var matrix = new DenseMatrix(new double[,]
        {
            { 4.0, 1.0, 1.0 },
            { 1.0, 3.0, 1.0 },
            { 1.0, 1.0, 2.0 }
        });

        var result = EigenSolvers.CyclicJacobi(
            matrix,
            tolerance: 1e-30,
            maximumSweeps: 1,
            symmetryTolerance: 1e-12);

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.MaximumIterationsReached, result.Status);
        Assert.Equal(1, result.Iterations);
        Assert.True(result.Residual > 0.0);
    }
}
