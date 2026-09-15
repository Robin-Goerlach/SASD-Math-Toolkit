using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

public sealed class ConjugateGradientTests
{
    [Fact]
    public void Solve_ConvergesForSparseSpdSystemAndReportsTrueResidual()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 4.0),
                new SparseMatrixEntry(0, 1, -1.0),
                new SparseMatrixEntry(1, 0, -1.0),
                new SparseMatrixEntry(1, 1, 4.0),
                new SparseMatrixEntry(1, 2, -1.0),
                new SparseMatrixEntry(2, 1, -1.0),
                new SparseMatrixEntry(2, 2, 3.0)
            ]);

        var result = ConjugateGradientSolver.Solve(matrix, [2.0, 4.0, 7.0]);

        Assert.True(result.Converged);
        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.InRange(result.Iterations, 1, 3);
        Assert.InRange(result.GetSolutionValue(0), 1.0 - 1e-10, 1.0 + 1e-10);
        Assert.InRange(result.GetSolutionValue(1), 2.0 - 1e-10, 2.0 + 1e-10);
        Assert.InRange(result.GetSolutionValue(2), 3.0 - 1e-10, 3.0 + 1e-10);
        Assert.True(result.HasFiniteResidual);
        Assert.True(result.ResidualNorm <= result.ConvergenceThreshold);
        Assert.True(result.InitialResidualNorm > result.ResidualNorm);
        Assert.True(result.RelativeResidualNorm <= 1e-10);
    }

    [Fact]
    public void Solve_ExactInitialGuessConvergesWithoutIterationAndResultIsDefensive()
    {
        var matrix = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 2.0),
                new SparseMatrixEntry(0, 1, -1.0),
                new SparseMatrixEntry(1, 0, -1.0),
                new SparseMatrixEntry(1, 1, 2.0)
            ]);

        var result = ConjugateGradientSolver.Solve(matrix, [0.0, 3.0], [1.0, 2.0]);

        Assert.True(result.Converged);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0.0, result.ResidualNorm);

        var firstCopy = result.Solution;
        firstCopy[0] = 99.0;
        Assert.Equal(1.0, result.GetSolutionValue(0));

        var destination = new double[2];
        result.CopySolutionTo(destination);
        Assert.Equal(new[] { 1.0, 2.0 }, destination);
        Assert.Throws<ArgumentException>(() => result.CopySolutionTo(new double[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetSolutionValue(2));
    }

    [Fact]
    public void Solve_ReportsIterationLimitWithFiniteBestResidual()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 4.0),
                new SparseMatrixEntry(0, 1, -1.0),
                new SparseMatrixEntry(1, 0, -1.0),
                new SparseMatrixEntry(1, 1, 4.0),
                new SparseMatrixEntry(1, 2, -1.0),
                new SparseMatrixEntry(2, 1, -1.0),
                new SparseMatrixEntry(2, 2, 3.0)
            ]);

        var result = ConjugateGradientSolver.Solve(
            matrix,
            [2.0, 4.0, 7.0],
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 0.0,
                RelativeTolerance = 1e-15,
                MaximumIterations = 1
            });

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.MaximumIterationsReached, result.Status);
        Assert.Equal(1, result.Iterations);
        Assert.True(result.HasFiniteResidual);
        Assert.True(result.ResidualNorm > result.ConvergenceThreshold);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Solve_ReportsNumericalBreakdownForSymmetricIndefiniteCurvature()
    {
        var matrix = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 1.0),
                new SparseMatrixEntry(0, 1, 2.0),
                new SparseMatrixEntry(1, 0, 2.0),
                new SparseMatrixEntry(1, 1, 1.0)
            ]);

        var result = ConjugateGradientSolver.Solve(matrix, [1.0, -1.0]);

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.Equal(0, result.Iterations);
        Assert.True(result.HasFiniteResidual);
        Assert.Contains("positive", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solve_ValidatesSymmetryDimensionsVectorsAndOptions()
    {
        var nonsymmetric = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 2.0),
                new SparseMatrixEntry(0, 1, 1.0),
                new SparseMatrixEntry(1, 1, 2.0)
            ]);

        Assert.Throws<ArgumentException>(() =>
            ConjugateGradientSolver.Solve(nonsymmetric, [1.0, 1.0]));

        var rectangular = CsrMatrix.FromEntries(2, 3, Array.Empty<SparseMatrixEntry>());
        Assert.Throws<ArgumentException>(() =>
            ConjugateGradientSolver.Solve(rectangular, [1.0, 1.0]));

        var identity = CsrMatrix.FromEntries(
            2,
            2,
            [new SparseMatrixEntry(0, 0, 1.0), new SparseMatrixEntry(1, 1, 1.0)]);

        Assert.Throws<ArgumentException>(() => ConjugateGradientSolver.Solve(identity, [1.0]));
        Assert.Throws<ArgumentException>(() => ConjugateGradientSolver.Solve(identity, [1.0, 2.0], [0.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConjugateGradientSolver.Solve(identity, [1.0, double.NaN]));
        Assert.Throws<ArgumentException>(() => ConjugateGradientSolver.Solve(
            identity,
            [1.0, 2.0],
            options: new SparseIterativeSolverOptions { AbsoluteTolerance = 0.0, RelativeTolerance = 0.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConjugateGradientSolver.Solve(
            identity,
            [1.0, 2.0],
            options: new SparseIterativeSolverOptions { MaximumIterations = 0 }));
    }

    [Fact]
    public void SparseMatrixDiagnostics_DistinguishSymmetryToleranceAndNecessarySpdDiagonal()
    {
        var nearlySymmetric = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 3.0),
                new SparseMatrixEntry(0, 1, 2.0),
                new SparseMatrixEntry(1, 0, 2.0 + 1e-13),
                new SparseMatrixEntry(1, 1, 4.0)
            ]);

        Assert.True(SparseMatrixDiagnostics.IsSymmetric(nearlySymmetric, 1e-12));
        Assert.False(SparseMatrixDiagnostics.IsSymmetric(nearlySymmetric, 1e-15));
        Assert.True(SparseMatrixDiagnostics.HasStrictlyPositiveDiagonal(nearlySymmetric));

        var missingDiagonal = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 1.0),
                new SparseMatrixEntry(0, 1, 1.0),
                new SparseMatrixEntry(1, 0, 1.0)
            ]);
        Assert.False(SparseMatrixDiagnostics.HasStrictlyPositiveDiagonal(missingDiagonal));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SparseMatrixDiagnostics.IsSymmetric(nearlySymmetric, -1.0));
    }

    [Fact]
    public void Solve_ZeroRightHandSideUsesAbsoluteResidualSemantics()
    {
        var identity = CsrMatrix.FromEntries(
            2,
            2,
            [new SparseMatrixEntry(0, 0, 1.0), new SparseMatrixEntry(1, 1, 1.0)]);

        var result = ConjugateGradientSolver.Solve(identity, [0.0, 0.0]);

        Assert.True(result.Converged);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0.0, result.RightHandSideNorm);
        Assert.Equal(0.0, result.ResidualNorm);
        Assert.Equal(0.0, result.RelativeResidualNorm);
        Assert.Equal(new[] { 0.0, 0.0 }, result.Solution);
    }
}
