using Sasd.Numerics.Common;
using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Tests;

public sealed class SparsePreconditionerTests
{
    [Fact]
    public void JacobiPreconditioner_UsesReciprocalDiagonalAndProtectsState()
    {
        var matrix = CsrMatrix.FromEntries(
            3,
            3,
            [
                new SparseMatrixEntry(0, 0, 2.0),
                new SparseMatrixEntry(0, 1, 7.0),
                new SparseMatrixEntry(1, 1, -4.0),
                new SparseMatrixEntry(2, 2, 0.5)
            ]);

        var preconditioner = JacobiPreconditioner.Create(matrix);
        var vector = new[] { 4.0, 8.0, 1.0 };

        // Jacobi is component-wise, so overlapping source/destination storage is well defined.
        preconditioner.Apply(vector, vector);

        Assert.Equal(new[] { 2.0, -2.0, 2.0 }, vector);
        Assert.Equal(3, preconditioner.Size);
        Assert.Equal(0.5, preconditioner.GetInverseDiagonalValue(0));
        Assert.Equal(-0.25, preconditioner.GetInverseDiagonalValue(1));
        Assert.Equal(2.0, preconditioner.GetInverseDiagonalValue(2));

        var copy = preconditioner.CopyInverseDiagonal();
        copy[0] = 99.0;
        Assert.Equal(0.5, preconditioner.GetInverseDiagonalValue(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => preconditioner.GetInverseDiagonalValue(3));
    }

    [Fact]
    public void JacobiPreconditioner_RejectsMissingTinyOrUnrepresentableDiagonalInverse()
    {
        var missing = CsrMatrix.FromEntries(
            2,
            2,
            [new SparseMatrixEntry(0, 0, 1.0), new SparseMatrixEntry(0, 1, 2.0)]);
        Assert.Throws<ArgumentException>(() => JacobiPreconditioner.Create(missing));

        var tiny = CsrMatrix.FromEntries(
            2,
            2,
            [new SparseMatrixEntry(0, 0, 1e-8), new SparseMatrixEntry(1, 1, 2.0)]);
        Assert.Throws<ArgumentException>(() =>
            JacobiPreconditioner.Create(tiny, absoluteDiagonalTolerance: 1e-7));

        var reciprocalOverflow = CsrMatrix.FromEntries(
            1,
            1,
            [new SparseMatrixEntry(0, 0, 1e-320)]);
        Assert.Throws<ArithmeticException>(() => JacobiPreconditioner.Create(reciprocalOverflow));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JacobiPreconditioner.Create(tiny, absoluteDiagonalTolerance: double.NaN));
    }

    [Fact]
    public void PreconditionedCg_JacobiSolvesScaledDiagonalSystemInOneIteration()
    {
        var matrix = CsrMatrix.FromEntries(
            4,
            4,
            [
                new SparseMatrixEntry(0, 0, 1.0),
                new SparseMatrixEntry(1, 1, 10.0),
                new SparseMatrixEntry(2, 2, 100.0),
                new SparseMatrixEntry(3, 3, 1000.0)
            ]);
        var rightHandSide = new[] { 1.0, 10.0, 100.0, 1000.0 };
        var options = new SparseIterativeSolverOptions
        {
            AbsoluteTolerance = 0.0,
            RelativeTolerance = 1e-12,
            MaximumIterations = 10
        };

        var cg = ConjugateGradientSolver.Solve(matrix, rightHandSide, options: options);
        var jacobi = JacobiPreconditioner.Create(matrix);
        var pcg = ConjugateGradientSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            jacobi,
            options: options);

        Assert.True(cg.Converged);
        Assert.True(pcg.Converged);
        Assert.True(cg.Iterations > pcg.Iterations);
        Assert.Equal(1, pcg.Iterations);
        Assert.True(pcg.ResidualNorm <= pcg.ConvergenceThreshold);
        Assert.Equal(IterationStatus.Converged, pcg.Status);

        for (var index = 0; index < 4; index++)
        {
            Assert.InRange(pcg.GetSolutionValue(index), 1.0 - 1e-12, 1.0 + 1e-12);
        }
    }

    [Fact]
    public void PreconditionedCg_ValidatesSizeAndDetectsNonSpdPreconditioner()
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

        Assert.Throws<ArgumentException>(() =>
            ConjugateGradientSolver.SolvePreconditioned(
                matrix,
                [1.0, 0.0],
                new NegativeIdentityPreconditioner(3)));

        var result = ConjugateGradientSolver.SolvePreconditioned(
            matrix,
            [1.0, 0.0],
            new NegativeIdentityPreconditioner(2));

        Assert.False(result.Converged);
        Assert.Equal(IterationStatus.NumericalBreakdown, result.Status);
        Assert.Equal(0, result.Iterations);
        Assert.Contains("SPD", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NegativeIdentityPreconditioner(int size) : ISparsePreconditioner
    {
        public int Size { get; } = size;

        public void Apply(ReadOnlySpan<double> source, Span<double> destination)
        {
            if (source.Length != Size || destination.Length != Size)
            {
                throw new ArgumentException("Unexpected test-vector size.");
            }

            for (var index = 0; index < Size; index++)
            {
                destination[index] = -source[index];
            }
        }
    }
}
