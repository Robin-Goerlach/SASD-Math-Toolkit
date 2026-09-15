using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

/// <summary>
/// Cross-solver regression cases for the modern sparse linear-algebra layer.
/// </summary>
/// <remarks>
/// These tests are deliberately larger than the small algorithm-specific unit tests. They are not
/// performance benchmarks. Their purpose is to verify that the public solver families agree on
/// deterministic structured systems, preserve the shared true-residual contract and remain usable
/// across restart/preconditioning boundaries.
/// </remarks>
public sealed class SparseSolverReferenceTests
{
    [Fact]
    public void StructuredSpdSystem_AllSparseSolverFamiliesRecoverKnownSolution()
    {
        const int size = 64;
        var matrix = BuildTridiagonal(size, lower: -1.0, diagonal: 4.0, upper: -1.0);
        var expected = BuildReferenceSolution(size);
        var rightHandSide = matrix.Multiply(expected);
        var options = StrictOptions(maximumIterations: 800);
        var jacobi = JacobiPreconditioner.Create(matrix);

        var cg = ConjugateGradientSolver.Solve(matrix, rightHandSide, options: options);
        var pcg = ConjugateGradientSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            jacobi,
            options: options);
        var gmres = GmresSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            jacobi,
            restartLength: 16,
            options: options);
        var bicgstab = BiCgStabSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            jacobi,
            options: options);

        AssertConvergedAndAccurate(matrix, rightHandSide, expected, cg, 2e-8);
        AssertConvergedAndAccurate(matrix, rightHandSide, expected, pcg, 2e-8);
        AssertConvergedAndAccurate(matrix, rightHandSide, expected, gmres, 2e-8);
        AssertConvergedAndAccurate(matrix, rightHandSide, expected, bicgstab, 2e-8);
    }

    [Fact]
    public void NonsymmetricSystem_GmresAndBiCgStabAgreeFromDifferentInitialGuesses()
    {
        const int size = 48;
        var matrix = BuildTridiagonal(size, lower: -1.35, diagonal: 4.5, upper: 0.55);
        var expected = BuildReferenceSolution(size);
        var rightHandSide = matrix.Multiply(expected);
        var options = StrictOptions(maximumIterations: 1200);
        var jacobi = JacobiPreconditioner.Create(matrix);
        var positiveGuess = Enumerable.Repeat(3.0, size).ToArray();
        var negativeGuess = Enumerable.Repeat(-2.0, size).ToArray();

        var gmres = GmresSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            jacobi,
            restartLength: 12,
            initialGuess: positiveGuess,
            options: options);
        var bicgstab = BiCgStabSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            jacobi,
            initialGuess: negativeGuess,
            options: options);

        AssertConvergedAndAccurate(matrix, rightHandSide, expected, gmres, 3e-8);
        AssertConvergedAndAccurate(matrix, rightHandSide, expected, bicgstab, 3e-8);

        for (var index = 0; index < size; index++)
        {
            Assert.InRange(
                System.Math.Abs(gmres.GetSolutionValue(index) - bicgstab.GetSolutionValue(index)),
                0.0,
                5e-8);
        }
    }

    [Fact]
    public void ReportedResidualNorm_MatchesIndependentTrueResidualRecomputation()
    {
        const int size = 40;
        var matrix = BuildTridiagonal(size, lower: -0.8, diagonal: 3.75, upper: 1.1);
        var expected = BuildReferenceSolution(size);
        var rightHandSide = matrix.Multiply(expected);
        var options = StrictOptions(maximumIterations: 1000);

        var gmres = GmresSolver.Solve(
            matrix,
            rightHandSide,
            restartLength: 10,
            options: options);
        var bicgstab = BiCgStabSolver.Solve(matrix, rightHandSide, options: options);

        AssertResidualMatches(matrix, rightHandSide, gmres);
        AssertResidualMatches(matrix, rightHandSide, bicgstab);
    }

    private static SparseIterativeSolverOptions StrictOptions(int maximumIterations) =>
        new()
        {
            AbsoluteTolerance = 1e-12,
            RelativeTolerance = 1e-10,
            MaximumIterations = maximumIterations
        };

    private static CsrMatrix BuildTridiagonal(
        int size,
        double lower,
        double diagonal,
        double upper)
    {
        var entries = new List<SparseMatrixEntry>((3 * size) - 2);
        for (var row = 0; row < size; row++)
        {
            if (row > 0)
            {
                entries.Add(new SparseMatrixEntry(row, row - 1, lower));
            }

            entries.Add(new SparseMatrixEntry(row, row, diagonal));

            if (row + 1 < size)
            {
                entries.Add(new SparseMatrixEntry(row, row + 1, upper));
            }
        }

        return CsrMatrix.FromEntries(size, size, entries);
    }

    private static double[] BuildReferenceSolution(int size)
    {
        var solution = new double[size];
        for (var index = 0; index < size; index++)
        {
            var position = index + 1.0;
            solution[index] = 0.25 + (0.015 * position) + System.Math.Sin(0.17 * position);
        }

        return solution;
    }

    private static void AssertConvergedAndAccurate(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        IReadOnlyList<double> expected,
        SparseLinearSolveResult result,
        double componentTolerance)
    {
        Assert.True(result.Converged, result.Message ?? "Sparse solver did not converge.");
        Assert.True(result.HasFiniteResidual);
        Assert.True(result.ResidualNorm <= result.ConvergenceThreshold);

        for (var index = 0; index < expected.Count; index++)
        {
            Assert.InRange(
                System.Math.Abs(result.GetSolutionValue(index) - expected[index]),
                0.0,
                componentTolerance);
        }

        AssertResidualMatches(matrix, rightHandSide, result);
    }

    private static void AssertResidualMatches(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        SparseLinearSolveResult result)
    {
        Assert.True(result.Converged, result.Message ?? "Sparse solver did not converge.");
        var solution = result.Solution;
        var matrixVector = matrix.Multiply(solution);
        var residual = new double[rightHandSide.Count];

        for (var index = 0; index < residual.Length; index++)
        {
            residual[index] = rightHandSide[index] - matrixVector[index];
        }

        var independentNorm = EuclideanNorm(residual);
        var comparisonScale = System.Math.Max(1.0, independentNorm);
        Assert.InRange(
            System.Math.Abs(independentNorm - result.ResidualNorm),
            0.0,
            5e-12 * comparisonScale);
        Assert.True(independentNorm <= result.ConvergenceThreshold);
    }

    private static double EuclideanNorm(ReadOnlySpan<double> vector)
    {
        var scale = 0.0;
        var scaledSquares = 1.0;
        var hasNonZero = false;

        for (var index = 0; index < vector.Length; index++)
        {
            var absolute = System.Math.Abs(vector[index]);
            if (absolute == 0.0)
            {
                continue;
            }

            hasNonZero = true;
            if (scale < absolute)
            {
                var ratio = scale / absolute;
                scaledSquares = 1.0 + (scaledSquares * ratio * ratio);
                scale = absolute;
            }
            else
            {
                var ratio = absolute / scale;
                scaledSquares += ratio * ratio;
            }
        }

        return hasNonZero ? scale * System.Math.Sqrt(scaledSquares) : 0.0;
    }
}
