using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Conjugate Gradient and Preconditioned Conjugate Gradient solvers for real SPD sparse systems.
/// </summary>
/// <remarks>
/// <para>
/// Conjugate Gradient (CG) is intended for large sparse systems <c>A*x=b</c> where <c>A</c> is
/// symmetric positive definite (SPD). The solver never converts the matrix to dense storage and
/// reuses fixed work vectors for all matrix-vector products.
/// </para>
/// <para>
/// <see cref="SolvePreconditioned"/> implements left-preconditioned CG with an abstract operation
/// <c>z = M^-1*r</c>. The supplied preconditioner must itself be SPD for the PCG recurrence to be
/// mathematically valid. The solver detects a non-positive <c>r^T*z</c> or <c>p^T*A*p</c> and reports
/// numerical breakdown instead of silently continuing with an invalid recurrence.
/// </para>
/// <para>
/// Recursive residuals are inexpensive but can drift from the true residual through floating-point
/// roundoff. Whenever the recursive residual first claims convergence, this implementation recomputes
/// <c>b-A*x</c> explicitly. If that verification fails the tolerance, CG/PCG restarts from the
/// refreshed residual instead of reporting a false convergence.
/// </para>
/// </remarks>
public static class ConjugateGradientSolver
{
    /// <summary>
    /// Solves <c>A*x=b</c> for a square SPD CSR matrix without preconditioning.
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
        SparseIterativeSolverOptions? options = null) =>
        SolveCore(matrix, rightHandSide, initialGuess, preconditioner: null, options);

    /// <summary>
    /// Solves <c>A*x=b</c> for a square SPD CSR matrix using Preconditioned Conjugate Gradient.
    /// </summary>
    /// <param name="matrix">Canonical sparse SPD system matrix.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="preconditioner">
    /// SPD approximate-inverse operation with the same vector dimension as the matrix.
    /// </param>
    /// <param name="initialGuess">Optional finite starting vector; zero is used by default.</param>
    /// <param name="options">Shared sparse iterative-solver options.</param>
    /// <remarks>
    /// The matrix must be SPD and the preconditioner must preserve the SPD PCG inner-product
    /// contract. <see cref="JacobiPreconditioner"/> created from an SPD matrix satisfies this
    /// requirement because every SPD matrix has a strictly positive diagonal.
    /// </remarks>
    public static SparseLinearSolveResult SolvePreconditioned(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        ISparsePreconditioner preconditioner,
        IReadOnlyList<double>? initialGuess = null,
        SparseIterativeSolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(preconditioner);
        return SolveCore(matrix, rightHandSide, initialGuess, preconditioner, options);
    }

    private static SparseLinearSolveResult SolveCore(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        IReadOnlyList<double>? initialGuess,
        ISparsePreconditioner? preconditioner,
        SparseIterativeSolverOptions? options)
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

        if (preconditioner is not null && preconditioner.Size != n)
        {
            throw new ArgumentException(
                "Preconditioner size must match the sparse matrix size.",
                nameof(preconditioner));
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

        if (!TryComputeTrueResidual(
                matrix,
                solution,
                rhs,
                residual,
                matrixVector,
                out var residualNorm,
                out var residualError))
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

        var preconditionedResidual = new double[n];
        if (!TryApplyPreconditioner(preconditioner, residual, preconditionedResidual, out var preconditionerError))
        {
            return Breakdown(
                solution,
                0,
                residualNorm,
                initialResidualNorm,
                rhsNorm,
                convergenceThreshold,
                preconditionerError);
        }

        if (!TryDot(residual, preconditionedResidual, out var residualInnerProduct)
            || !(residualInnerProduct > 0.0))
        {
            return Breakdown(
                solution,
                0,
                residualNorm,
                initialResidualNorm,
                rhsNorm,
                convergenceThreshold,
                "Initial r^T*M^-1*r underflowed, overflowed, or became non-positive. The preconditioner may violate the SPD contract.");
        }

        var direction = (double[])preconditionedResidual.Clone();

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
                    $"Sparse matrix-vector multiplication failed during CG/PCG: {exception.Message}");
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
                    "CG/PCG search-direction curvature became non-finite.");
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
                    "CG/PCG encountered non-positive p^T*A*p. The matrix may not be positive definite or roundoff has destroyed the SPD recurrence.");
            }

            var alpha = residualInnerProduct / curvature;
            if (!double.IsFinite(alpha))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG/PCG step length became non-finite.");
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
                        "CG/PCG produced a non-finite solution or residual component.");
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
                    "CG/PCG recursive residual norm is outside the finite double range.");
            }

            if (residualNorm <= convergenceThreshold)
            {
                // Verify against the true residual before announcing convergence. If roundoff has
                // accumulated enough drift to fail the check, restart from the refreshed residual.
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

                if (!TryApplyPreconditioner(
                        preconditioner,
                        residual,
                        preconditionedResidual,
                        out preconditionerError))
                {
                    return Breakdown(
                        solution,
                        iteration,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        preconditionerError);
                }

                if (!TryDot(residual, preconditionedResidual, out residualInnerProduct)
                    || !(residualInnerProduct > 0.0))
                {
                    return Breakdown(
                        solution,
                        iteration,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "CG/PCG could not restart because r^T*M^-1*r became non-positive or non-finite.");
                }

                Array.Copy(preconditionedResidual, direction, n);
                continue;
            }

            if (!TryApplyPreconditioner(
                    preconditioner,
                    residual,
                    preconditionedResidual,
                    out preconditionerError))
            {
                return Breakdown(
                    solution,
                    iteration,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    preconditionerError);
            }

            if (!TryDot(residual, preconditionedResidual, out var nextResidualInnerProduct)
                || !(nextResidualInnerProduct > 0.0))
            {
                return Breakdown(
                    solution,
                    iteration,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG/PCG r^T*M^-1*r underflowed, overflowed, or became non-positive. The preconditioner may violate the SPD contract.");
            }

            var beta = nextResidualInnerProduct / residualInnerProduct;
            if (!double.IsFinite(beta))
            {
                return Breakdown(
                    solution,
                    iteration,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "CG/PCG direction-update coefficient became non-finite.");
            }

            for (var index = 0; index < n; index++)
            {
                var nextDirection = preconditionedResidual[index] + (beta * direction[index]);
                if (!double.IsFinite(nextDirection))
                {
                    return Breakdown(
                        solution,
                        iteration,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "CG/PCG produced a non-finite search direction.");
                }

                direction[index] = nextDirection;
            }

            residualInnerProduct = nextResidualInnerProduct;
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

    private static bool TryApplyPreconditioner(
        ISparsePreconditioner? preconditioner,
        ReadOnlySpan<double> residual,
        Span<double> destination,
        out string? error)
    {
        if (preconditioner is null)
        {
            residual.CopyTo(destination);
            error = null;
            return true;
        }

        try
        {
            preconditioner.Apply(residual, destination);
        }
        catch (ArithmeticException exception)
        {
            error = $"Preconditioner application failed numerically: {exception.Message}";
            return false;
        }

        for (var index = 0; index < destination.Length; index++)
        {
            if (!double.IsFinite(destination[index]))
            {
                error = "Preconditioner produced a non-finite output component.";
                return false;
            }
        }

        error = null;
        return true;
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
