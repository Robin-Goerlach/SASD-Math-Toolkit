using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

public sealed class GmresTests
{
    [Fact]
    public void Solve_ConvergesForNonsymmetricSparseSystemAndReportsTrueResidual()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 4.0),
                new SparseMatrixEntry(0, 1, 1.0),
                new SparseMatrixEntry(1, 0, 2.0),
                new SparseMatrixEntry(1, 1, 3.0),
                new SparseMatrixEntry(1, 2, 1.0),
                new SparseMatrixEntry(2, 1, 1.0),
                new SparseMatrixEntry(2, 2, 2.0)
            ]);

        var result = GmresSolver.Solve(
            matrix,
            [6.0, 11.0, 8.0],
            restartLength: 3,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-13,
                RelativeTolerance = 1e-12,
                MaximumIterations = 20
            });

        Assert.True(result.Converged);
        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.InRange(result.Iterations, 1, 3);
        Assert.InRange(result.GetSolutionValue(0), 1.0 - 1e-10, 1.0 + 1e-10);
        Assert.InRange(result.GetSolutionValue(1), 2.0 - 1e-10, 2.0 + 1e-10);
        Assert.InRange(result.GetSolutionValue(2), 3.0 - 1e-10, 3.0 + 1e-10);
        Assert.True(result.HasFiniteResidual);
        Assert.True(result.ResidualNorm <= result.ConvergenceThreshold);
        Assert.True(result.InitialResidualNorm > result.ResidualNorm);
    }

    [Fact]
    public void Solve_RestartLengthOneCanConvergeAcrossMultipleCycles()
    {
        var matrix = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 0, 4.0),
                new SparseMatrixEntry(0, 1, 1.0),
                new SparseMatrixEntry(1, 0, 2.0),
                new SparseMatrixEntry(1, 1, 3.0)
            ]);

        var result = GmresSolver.Solve(
            matrix,
            [6.0, 8.0],
            restartLength: 1,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-12,
                RelativeTolerance = 1e-10,
                MaximumIterations = 100
            });

        Assert.True(result.Converged);
        Assert.True(result.Iterations > 1);
        Assert.InRange(result.GetSolutionValue(0), 1.0 - 1e-8, 1.0 + 1e-8);
        Assert.InRange(result.GetSolutionValue(1), 2.0 - 1e-8, 2.0 + 1e-8);
        Assert.True(result.ResidualNorm <= result.ConvergenceThreshold);
    }

    [Fact]
    public void SolvePreconditioned_JacobiRightPreconditioningSolvesScaledDiagonalSystemInOneIteration()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 1e-6),
                new SparseMatrixEntry(1, 1, 1.0),
                new SparseMatrixEntry(2, 2, 1e6)
            ]);
        var preconditioner = JacobiPreconditioner.Create(matrix);

        var result = GmresSolver.SolvePreconditioned(
            matrix,
            [1e-6, 2.0, 3e6],
            preconditioner,
            restartLength: 3,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-12,
                RelativeTolerance = 1e-12,
                MaximumIterations = 10
            });

        Assert.True(result.Converged);
        Assert.Equal(1, result.Iterations);
        Assert.InRange(result.GetSolutionValue(0), 1.0 - 1e-10, 1.0 + 1e-10);
        Assert.InRange(result.GetSolutionValue(1), 2.0 - 1e-10, 2.0 + 1e-10);
        Assert.InRange(result.GetSolutionValue(2), 3.0 - 1e-10, 3.0 + 1e-10);
        Assert.True(result.ResidualNorm <= result.ConvergenceThreshold);
    }

    [Fact]
    public void Solve_ReportsIterationLimitWithFiniteBestResidual()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 4.0),
                new SparseMatrixEntry(0, 1, 1.0),
                new SparseMatrixEntry(1, 0, 2.0),
                new SparseMatrixEntry(1, 1, 3.0),
                new SparseMatrixEntry(1, 2, 1.0),
                new SparseMatrixEntry(2, 1, 1.0),
                new SparseMatrixEntry(2, 2, 2.0)
            ]);

        var result = GmresSolver.Solve(
            matrix,
            [6.0, 11.0, 8.0],
            restartLength: 1,
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
    }

    [Fact]
    public void Solve_ReportsArnoldiBreakdownWhenKrylovSpaceCannotReduceResidual()
    {
        var zero = CsrMatrix.FromEntries(2, 2, Array.Empty<SparseMatrixEntry>());

        var result = GmresSolver.Solve(
            zero,
            [1.0, -1.0],
            restartLength: 2,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-14,
                RelativeTolerance = 1e-14,
                MaximumIterations = 10
            });

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.True(result.HasFiniteResidual);
        Assert.Contains("GMRES", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solve_ValidatesDimensionsRestartLengthVectorsAndPreconditionerSize()
    {
        var identity = CsrMatrix.FromEntries(
            2,
            2,
            [new SparseMatrixEntry(0, 0, 1.0), new SparseMatrixEntry(1, 1, 1.0)]);
        var rectangular = CsrMatrix.FromEntries(2, 3, Array.Empty<SparseMatrixEntry>());

        Assert.Throws<ArgumentException>(() => GmresSolver.Solve(rectangular, [1.0, 1.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => GmresSolver.Solve(identity, [1.0, 2.0], restartLength: 0));
        Assert.Throws<ArgumentException>(() => GmresSolver.Solve(identity, [1.0]));
        Assert.Throws<ArgumentException>(() => GmresSolver.Solve(identity, [1.0, 2.0], initialGuess: [0.0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => GmresSolver.Solve(identity, [1.0, double.NaN]));

        var wrongSize = new PassthroughPreconditioner(3);
        Assert.Throws<ArgumentException>(() =>
            GmresSolver.SolvePreconditioned(identity, [1.0, 2.0], wrongSize));
    }

    private sealed class PassthroughPreconditioner(int size) : ISparsePreconditioner
    {
        public int Size { get; } = size;

        public void Apply(ReadOnlySpan<double> source, Span<double> destination) => source.CopyTo(destination);
    }
}
