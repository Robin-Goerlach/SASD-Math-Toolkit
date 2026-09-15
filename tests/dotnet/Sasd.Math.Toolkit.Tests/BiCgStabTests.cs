using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

public sealed class BiCgStabTests
{
    [Fact]
    public void Solve_ConvergesForNonsymmetricSparseSystemAndReportsTrueResidual()
    {
        var matrix = BuildReferenceMatrix();
        var rightHandSide = new[] { 2.0, -1.0, 4.0 };

        var result = BiCgStabSolver.Solve(
            matrix,
            rightHandSide,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-13,
                RelativeTolerance = 1e-12,
                MaximumIterations = 50
            });

        Assert.True(result.Converged);
        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.InRange(result.Iterations, 1, 3);
        Assert.True(result.ResidualNorm <= result.ConvergenceThreshold);

        Assert.Equal(1.0, result.GetSolutionValue(0), 10);
        Assert.Equal(-2.0, result.GetSolutionValue(1), 10);
        Assert.Equal(3.0, result.GetSolutionValue(2), 10);

        var solution = result.Solution;
        var product = matrix.Multiply(solution);
        var residualNorm = EuclideanNorm(
            rightHandSide.Zip(product, static (b, ax) => b - ax).ToArray());
        Assert.Equal(residualNorm, result.ResidualNorm, 12);
    }

    [Fact]
    public void SolvePreconditioned_JacobiSolvesScaledDiagonalSystemInOneIteration()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 1e-6),
                new SparseMatrixEntry(1, 1, 2.0),
                new SparseMatrixEntry(2, 2, 1e6)
            ]);
        var expected = new[] { 2.0, -3.0, 4.0 };
        var rightHandSide = matrix.Multiply(expected);
        var preconditioner = JacobiPreconditioner.Create(matrix);

        var result = BiCgStabSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            preconditioner,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-12,
                RelativeTolerance = 1e-12,
                MaximumIterations = 10
            });

        Assert.True(result.Converged);
        Assert.Equal(1, result.Iterations);
        Assert.Equal(expected[0], result.GetSolutionValue(0), 12);
        Assert.Equal(expected[1], result.GetSolutionValue(1), 12);
        Assert.Equal(expected[2], result.GetSolutionValue(2), 12);
    }

    [Fact]
    public void Solve_ExactInitialGuessConvergesWithoutIterationAndResultIsDefensive()
    {
        var matrix = BuildReferenceMatrix();
        var exact = new[] { 1.0, -2.0, 3.0 };
        var rightHandSide = matrix.Multiply(exact);

        var result = BiCgStabSolver.Solve(matrix, rightHandSide, exact);

        Assert.True(result.Converged);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0.0, result.ResidualNorm);

        var copy = result.Solution;
        copy[0] = 99.0;
        Assert.Equal(1.0, result.GetSolutionValue(0));
    }

    [Fact]
    public void Solve_ReportsIterationLimitWithFiniteBestResidual()
    {
        var matrix = BuildReferenceMatrix();
        var rightHandSide = new[] { 2.0, -1.0, 4.0 };

        var result = BiCgStabSolver.Solve(
            matrix,
            rightHandSide,
            options: new SparseIterativeSolverOptions
            {
                AbsoluteTolerance = 1e-30,
                RelativeTolerance = 0.0,
                MaximumIterations = 1
            });

        Assert.Equal(IterationStatus.MaximumIterationsReached, result.Status);
        Assert.False(result.Converged);
        Assert.Equal(1, result.Iterations);
        Assert.True(result.HasFiniteResidual);
        Assert.True(result.ResidualNorm < result.InitialResidualNorm);
    }

    [Fact]
    public void Solve_ReportsRecurrenceBreakdownWithoutPretendingConvergence()
    {
        // For the initial residual r=[1,0], A*r=[0,-1], so rHat^T*A*p is exactly zero.
        // That is a genuine BiCGSTAB alpha-denominator breakdown, not convergence.
        var matrix = CsrMatrix.FromEntries(
            2,
            2,
            [
                new SparseMatrixEntry(0, 1, 1.0),
                new SparseMatrixEntry(1, 0, -1.0)
            ]);

        var result = BiCgStabSolver.Solve(matrix, new[] { 1.0, 0.0 });

        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.False(result.Converged);
        Assert.Equal(0, result.Iterations);
        Assert.Contains("alpha denominator", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solve_ValidatesDimensionsVectorsOptionsAndPreconditionerSize()
    {
        var square = BuildReferenceMatrix();
        var rectangular = CsrMatrix.FromEntries(
            2,
            3,
            [new SparseMatrixEntry(0, 0, 1.0), new SparseMatrixEntry(1, 1, 1.0)]);

        Assert.Throws<ArgumentException>(() =>
            BiCgStabSolver.Solve(rectangular, new[] { 1.0, 2.0 }));
        Assert.Throws<ArgumentException>(() =>
            BiCgStabSolver.Solve(square, new[] { 1.0, 2.0 }));
        Assert.Throws<ArgumentException>(() =>
            BiCgStabSolver.Solve(square, new[] { 1.0, 2.0, 3.0 }, new[] { 0.0, 0.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BiCgStabSolver.Solve(square, new[] { 1.0, double.NaN, 3.0 }));
        Assert.Throws<ArgumentException>(() =>
            BiCgStabSolver.Solve(
                square,
                new[] { 1.0, 2.0, 3.0 },
                options: new SparseIterativeSolverOptions
                {
                    AbsoluteTolerance = 0.0,
                    RelativeTolerance = 0.0
                }));

        var wrongSizePreconditioner = JacobiPreconditioner.Create(
            CsrMatrix.FromEntries(
                2,
                2,
                [new SparseMatrixEntry(0, 0, 1.0), new SparseMatrixEntry(1, 1, 1.0)]));

        Assert.Throws<ArgumentException>(() =>
            BiCgStabSolver.SolvePreconditioned(
                square,
                new[] { 1.0, 2.0, 3.0 },
                wrongSizePreconditioner));
    }

    private static CsrMatrix BuildReferenceMatrix() =>
        CsrMatrix.FromEntries(
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

    private static double EuclideanNorm(IEnumerable<double> values)
    {
        var sum = 0.0;
        foreach (var value in values)
        {
            sum += value * value;
        }

        return System.Math.Sqrt(sum);
    }
}
