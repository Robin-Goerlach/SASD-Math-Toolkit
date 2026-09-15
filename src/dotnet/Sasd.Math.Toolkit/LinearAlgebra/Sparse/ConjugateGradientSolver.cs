using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Conjugate Gradient solver for real symmetric positive-definite sparse systems.
/// </summary>
/// <remarks>
/// <para>
/// Conjugate Gradient (CG) is intended for large sparse systems <c>A*x=b</c> where <c>A</c> is
/// symmetric positive definite (SPD). The solver never converts the matrix to dense storage and
/// reuses fixed work vectors for all matrix-vector products.
/// </para>
/// <para>
/// Symmetry can be validated before iteration. Positive definiteness is a mathematical contract
/// that cannot be proven cheaply for an arbitrary large sparse matrix; CG therefore also treats a
/// non-positive search-direction curvature <c>p^T*A*p</c> as a numerical breakdown rather than
/// silently continuing with an invalid recurrence.
/// </para>
/// <para>
/// Recursive residuals are inexpensive but can drift from the true residual through floating-point
/// roundoff. Whenever the recursive residual first claims convergence, this implementation recomputes
/// <c>b-A*x</c> explicitly. If that verification fails the tolerance, CG restarts from the refreshed
/// residual instead of reporting a false convergence.
/// </para>
/// </remarks>
public static class ConjugateGradientSolver
{
    /// <summary>
    /// Solves <c>A*x=b</c> for a square SPD CSR matrix.
    /// </summary>
    /// <param name="matrix">Canonical sparse system matrix.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="initialGuess">Optional finite starting vector; zero is used by default.</param>
    /// <param name="options">Shared sparse iterative-solver options.</param>
    /// <returns>A status-bearing best solution and residual diagnostics.</returns>
    public static SparseLinearSolveResult Solve(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        IReadOnlyList<double>? initialGuess = null,
        SparseIterativeSolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Conjugate Gradient requires a square matrix.", nameof(matrix));
        }

        var settings = options ?? new SparseIterativeSolverOptions();
        settings.Validate();

        var n = matrix.Rows;
        if (rightHandSide.Count != n)
        {
            throw new ArgumentException(
                "Right-hand-side length must match the sparse matrix size.",
                nameof(rightHandSide));
        }

        if (initialGuess is not null && initialGuess.Count != n)
        {
            throw new ArgumentException(
                "Initial-guess length must match the sparse matrix size.",
                nameof(initialGuess));
        }

        var rhs = CopyAndValidateVector(rightHandSide, nameof(rightHandSide));
        var solution = initialGuess is null
            ? new double[n]
            : CopyAndValidateVector(initialGuess, nameof(initialGuess));

        if (settings.ValidateSymmetry
            && !SparseMatrixDiagnostics.IsSymmetric(matrix, settings.SymmetryTolerance))
        {
            throw new ArgumentException(
                "Conjugate Gradient requires a symmetric matrix. Disable validation only when symmetry is guaranteed externally.",
                nameof(matrix));
        }

        if (!SparseMatrixDiagnostics.HasStrictlyPositiveDiagonal(matrix))
        {
            throw new ArgumentException(
                "Conjugate Gradient requires an SPD matrix; every diagonal entry of an SPD matrix must be strictly positive.",
                nameof(matrix));
        }

        if (!TryEuclideanNorm(rhs, out var rhsNorm))
        {
            return Breakdown(
                solution,
                0,
                double.NaN,
                double.NaN,
                double.NaN,
                double.NaN,
                "Right-hand-side norm is outside the finite double range.");
        }

        var relativeThreshold = settings.RelativeTolerance * rhsNorm;
        if (!double.IsFinite(relativeThreshold))
        {
            return Breakdown(
                solution,
                0,
                double.NaN,
                double.NaN,
                rhsNorm,
                double.NaN,
                "Relative convergence threshold became non-finite.");
        }

        var convergenceThreshold = System.Math.Max(settings.AbsoluteTolerance, relativeThreshold);
        var residual = new double[n];
        var matrixVector = new double[n];

        if (!TryComputeTrueResidual(matrix, solution, rhs, residual, matrixVector, out var residualNorm, out var residualError))
        {
            return Breakdown(
                solution,
                0,
                double.NaN,
                double.NaN,
                rhsNorm,
                convergenceThreshold,
                residualError);
        }

        var initialResidualNorm = residualNorm;
        if (residualNorm <= convergenceThreshold)
        {
            return new SparseLinearSolveResult(
                solution,
                0,
                IterationStatus.Converged,
                residualNorm,
                initialResidualNorm,
                rhsNorm,
                convergenceThreshold);
        }

        var direction = (double[])residual.Clone();
        if (!TryDot(residual, residual, out var residualEnergy) || !(residualEnergy > 0.0))
        {
            return Breakdown(
                solution,
                0,
                residualNorm,
                initialResidualNorm,
                rhsNorm,
                convergenceThreshold,
                "Initial residual inner product underflowed, overflowed, or became non-positive.");
        }

        for (var iteration = 1; iteration <= settings.MaximumIterations; iteration++)
        {
            try
            {
                matrix.Multiply(direction, matrixVector);
            }
            catch (ArithmeticException exception)
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    $"Sparse matrix-vector multiplication failed during CG: {exception.Message}");
            }

            if (!TryDot(direction, matrixVector, out var curvature))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG search-direction curvature became non-finite.");
            }

            if (!(curvature > 0.0))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG encountered non-positive p^T*A*p. The matrix may not be positive definite or roundoff has destroyed the SPD recurrence.");
            }

            var alpha = residualEnergy / curvature;
            if (!double.IsFinite(alpha))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG step length became non-finite.");
            }

            for (var index = 0; index < n; index++)
            {
                var nextSolution = solution[index] + (alpha * direction[index]);
                var nextResidual = residual[index] - (alpha * matrixVector[index]);
                if (!double.IsFinite(nextSolution) || !double.IsFinite(nextResidual))
                {
                    return Breakdown(
                        solution,
                        iteration - 1,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "CG produced a non-finite solution or residual component.");
                }

                solution[index] = nextSolution;
                residual[index] = nextResidual;
            }

            if (!TryEuclideanNorm(residual, out residualNorm))
            {
                return Breakdown(
                    solution,
                    iteration,
                    double.NaN,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG recursive residual norm is outside the finite double range.");
            }

            if (residualNorm <= convergenceThreshold)
            {
                // Verify against the true residual before announcing convergence. If roundoff has
                // accumulated enough drift to fail the check, restart CG from the refreshed residual.
                if (!TryComputeTrueResidual(
                        matrix,
                        solution,
                        rhs,
                        residual,
                        matrixVector,
                        out residualNorm,
                        out residualError))
                {
                    return Breakdown(
                        solution,
                        iteration,
                        double.NaN,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        residualError);
                }

                if (residualNorm <= convergenceThreshold)
                {
                    return new SparseLinearSolveResult(
                        solution,
                        iteration,
                        IterationStatus.Converged,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold);
                }

                Array.Copy(residual, direction, n);
                if (!TryDot(residual, residual, out residualEnergy) || !(residualEnergy > 0.0))
                {
                    return Breakdown(
                        solution,
                        iteration,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "CG could not restart from the refreshed true residual.");
                }

                continue;
            }

            if (!TryDot(residual, residual, out var nextResidualEnergy) || !(nextResidualEnergy > 0.0))
            {
                return Breakdown(
                    solution,
                    iteration,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG residual inner product underflowed, overflowed, or became non-positive.");
            }

            var beta = nextResidualEnergy / residualEnergy;
            if (!double.IsFinite(beta))
            {
                return Breakdown(
                    solution,
                    iteration,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG direction-update coefficient became non-finite.");
            }

            for (var index = 0; index < n; index++)
            {
                var nextDirection = residual[index] + (beta * direction[index]);
                if (!double.IsFinite(nextDirection))
                {
                    return Breakdown(
                        solution,
                        iteration,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "CG produced a non-finite search direction.");
                }

                direction[index] = nextDirection;
            }

            residualEnergy = nextResidualEnergy;
        }

        if (!TryComputeTrueResidual(
                matrix,
                solution,
                rhs,
                residual,
                matrixVector,
                out residualNorm,
                out residualError))
        {
            return Breakdown(
                solution,
                settings.MaximumIterations,
                double.NaN,
                initialResidualNorm,
                rhsNorm,
                convergenceThreshold,
                residualError);
        }

        return new SparseLinearSolveResult(
            solution,
            settings.MaximumIterations,
            IterationStatus.MaximumIterationsReached,
            residualNorm,
            initialResidualNorm,
            rhsNorm,
            convergenceThreshold,
            "Maximum number of Conjugate Gradient iterations reached.");
    }

    private static bool TryComputeTrueResidual(
        CsrMatrix matrix,
        double[] solution,
        double[] rightHandSide,
        double[] residual,
        double[] matrixVector,
        out double residualNorm,
        out string? error)
    {
        try
        {
            matrix.Multiply(solution, matrixVector);
        }
        catch (ArithmeticException exception)
        {
            residualNorm = double.NaN;
            error = $"True residual matrix-vector multiplication failed: {exception.Message}";
            return false;
        }

        for (var index = 0; index < residual.Length; index++)
        {
            var value = rightHandSide[index] - matrixVector[index];
            if (!double.IsFinite(value))
            {
                residualNorm = double.NaN;
                error = "True residual computation became non-finite.";
                return false;
            }

            residual[index] = value;
        }

        if (!TryEuclideanNorm(residual, out residualNorm))
        {
            error = "True residual norm is outside the finite double range.";
            return false;
        }

        error = null;
        return true;
    }

    private static double[] CopyAndValidateVector(IReadOnlyList<double> source, string parameterName)
    {
        var result = new double[source.Count];
        for (var index = 0; index < source.Count; index++)
        {
            var value = source[index];
            if (!double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Vector values must be finite.");
            }

            result[index] = value;
        }

        return result;
    }

    private static bool TryEuclideanNorm(ReadOnlySpan<double> vector, out double norm)
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

        if (!hasNonZero)
        {
            norm = 0.0;
            return true;
        }

        norm = scale * System.Math.Sqrt(scaledSquares);
        return double.IsFinite(norm);
    }

    private static bool TryDot(ReadOnlySpan<double> left, ReadOnlySpan<double> right, out double result)
    {
        var sum = 0.0;
        var compensation = 0.0;

        for (var index = 0; index < left.Length; index++)
        {
            var product = left[index] * right[index];
            if (!double.IsFinite(product))
            {
                result = double.NaN;
                return false;
            }

            // Kahan-style compensation reduces avoidable cancellation error in the recurrence's
            // dot products without adding dependencies or changing the solver contract.
            var corrected = product - compensation;
            var next = sum + corrected;
            if (!double.IsFinite(next))
            {
                result = double.NaN;
                return false;
            }

            compensation = (next - sum) - corrected;
            sum = next;
        }

        result = sum;
        return double.IsFinite(result);
    }

    private static SparseLinearSolveResult Breakdown(
        double[] solution,
        int iterations,
        double residualNorm,
        double initialResidualNorm,
        double rightHandSideNorm,
        double convergenceThreshold,
        string? message) =>
        new(
            solution,
            iterations,
            IterationStatus.NumericalBreakdown,
            residualNorm,
            initialResidualNorm,
            rightHandSideNorm,
            convergenceThreshold,
            message);
}
