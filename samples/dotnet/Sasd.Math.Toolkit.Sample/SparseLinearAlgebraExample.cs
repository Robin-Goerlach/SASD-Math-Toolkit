using Sasd.Numerics.LinearAlgebra.Sparse;

namespace Sasd.Math.Toolkit.Sample;

/// <summary>
/// Runs a deterministic sparse linear-algebra smoke example using the public 2026 solver APIs.
/// </summary>
/// <remarks>
/// The example deliberately uses a nonsymmetric tridiagonal system with a known solution so both
/// restarted GMRES and BiCGSTAB can be exercised against the same physical problem. Jacobi is used
/// as the baseline right preconditioner. The example is small enough for the normal sample program,
/// but large enough to require genuine Krylov iteration rather than a single toy 2x2 solve.
/// </remarks>
public static class SparseLinearAlgebraExample
{
    /// <summary>Runs the sparse reference solve and returns deterministic diagnostics.</summary>
    public static SparseLinearAlgebraExampleResult Run()
    {
        const int size = 24;
        var matrix = BuildTridiagonal(
            size,
            lower: -1.20,
            diagonal: 4.25,
            upper: 0.45);
        var expected = BuildReferenceSolution(size);
        var rightHandSide = matrix.Multiply(expected);
        var preconditioner = JacobiPreconditioner.Create(matrix);
        var options = new SparseIterativeSolverOptions
        {
            AbsoluteTolerance = 1e-12,
            RelativeTolerance = 1e-10,
            MaximumIterations = 400
        };

        var gmres = GmresSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            preconditioner,
            restartLength: 8,
            options: options);
        var bicgstab = BiCgStabSolver.SolvePreconditioned(
            matrix,
            rightHandSide,
            preconditioner,
            options: options);

        if (!gmres.Converged)
        {
            throw new InvalidOperationException(
                $"Sparse demo GMRES did not converge: {gmres.Status}. {gmres.Message}");
        }

        if (!bicgstab.Converged)
        {
            throw new InvalidOperationException(
                $"Sparse demo BiCGSTAB did not converge: {bicgstab.Status}. {bicgstab.Message}");
        }

        var gmresSolution = gmres.Solution;
        var bicgstabSolution = bicgstab.Solution;
        var maximumSolutionError = 0.0;
        var maximumSolverDisagreement = 0.0;

        for (var index = 0; index < size; index++)
        {
            maximumSolutionError = System.Math.Max(
                maximumSolutionError,
                System.Math.Max(
                    System.Math.Abs(gmresSolution[index] - expected[index]),
                    System.Math.Abs(bicgstabSolution[index] - expected[index])));
            maximumSolverDisagreement = System.Math.Max(
                maximumSolverDisagreement,
                System.Math.Abs(gmresSolution[index] - bicgstabSolution[index]));
        }

        var gmresResidual = SparseMatrixDiagnostics.ResidualEuclideanNorm(
            matrix,
            gmresSolution,
            rightHandSide);
        var bicgstabResidual = SparseMatrixDiagnostics.ResidualEuclideanNorm(
            matrix,
            bicgstabSolution,
            rightHandSide);
        var worstResidualNorm = System.Math.Max(gmresResidual, bicgstabResidual);

        // This is a sample smoke invariant, not a benchmark. If a later change breaks solver
        // correctness strongly enough to exceed this generous threshold, the sample should fail
        // loudly instead of printing plausible-looking but incorrect numbers.
        if (maximumSolutionError > 1e-7 || maximumSolverDisagreement > 1e-7)
        {
            throw new InvalidOperationException(
                "Sparse demo solvers converged according to residuals but disagreed with the known solution.");
        }

        return new SparseLinearAlgebraExampleResult(
            size,
            gmres.Iterations,
            bicgstab.Iterations,
            worstResidualNorm,
            maximumSolutionError,
            maximumSolverDisagreement);
    }

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
        var result = new double[size];
        for (var index = 0; index < size; index++)
        {
            var position = index + 1.0;
            result[index] = 0.5 + (0.02 * position) + System.Math.Sin(0.18 * position);
        }

        return result;
    }
}

/// <summary>Deterministic diagnostics produced by <see cref="SparseLinearAlgebraExample"/>.</summary>
/// <param name="Size">Order of the square sparse reference system.</param>
/// <param name="GmresIterations">Completed restarted-GMRES iterations.</param>
/// <param name="BiCgStabIterations">Completed BiCGSTAB iterations.</param>
/// <param name="WorstResidualNorm">Larger independently recomputed true residual norm.</param>
/// <param name="MaximumSolutionError">Largest absolute component error against the known solution.</param>
/// <param name="MaximumSolverDisagreement">Largest component difference between the two solver results.</param>
public sealed record SparseLinearAlgebraExampleResult(
    int Size,
    int GmresIterations,
    int BiCgStabIterations,
    double WorstResidualNorm,
    double MaximumSolutionError,
    double MaximumSolverDisagreement);
