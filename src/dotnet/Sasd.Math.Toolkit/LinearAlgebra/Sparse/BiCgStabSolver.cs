using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// BiConjugate Gradient Stabilized solver for real square sparse linear systems.
/// </summary>
/// <remarks>
/// <para>
/// BiCGSTAB targets general nonsymmetric systems <c>A*x=b</c> while keeping a fixed number of work
/// vectors. Unlike restarted GMRES, its memory use does not grow with a Krylov restart length, which
/// makes it attractive for large sparse problems when its short recurrence behaves well.
/// </para>
/// <para>
/// Optional preconditioning is applied on the right. Search directions are transformed with
/// <c>M^-1</c> before multiplication by <c>A</c>, while convergence is always measured against the
/// physical residual <c>b-A*x</c>. The same <see cref="ISparsePreconditioner"/> contract used by GMRES
/// can therefore be reused without changing public residual semantics.
/// </para>
/// <para>
/// BiCGSTAB has more scalar breakdown points than GMRES. This implementation treats zero or
/// non-finite recurrence denominators as <see cref="IterationStatus.NumericalBreakdown"/> and never
/// reports convergence from a recursive residual alone. Whenever the recurrence first reaches the
/// requested tolerance, the true residual is recomputed explicitly; if roundoff invalidates the
/// claim, the method restarts from that refreshed residual.
/// </para>
/// </remarks>
public static class BiCgStabSolver
{
    /// <summary>Solves a general square sparse system without preconditioning.</summary>
    /// <param name="matrix">Square canonical CSR system matrix.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="initialGuess">Optional finite initial solution; zero is used by default.</param>
    /// <param name="options">Shared sparse convergence options.</param>
    public static SparseLinearSolveResult Solve(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        IReadOnlyList<double>? initialGuess = null,
        SparseIterativeSolverOptions? options = null) =>
        SolveCore(matrix, rightHandSide, initialGuess, preconditioner: null, options);

    /// <summary>Solves a general square sparse system using right-preconditioned BiCGSTAB.</summary>
    /// <param name="matrix">Square canonical CSR system matrix.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="preconditioner">Fixed approximate-inverse operation used on search directions.</param>
    /// <param name="initialGuess">Optional finite initial solution; zero is used by default.</param>
    /// <param name="options">Shared sparse convergence options.</param>
    /// <remarks>
    /// The preconditioner need not be symmetric positive definite, but it is expected to behave as a
    /// fixed deterministic linear operation during one solve. A numerically invalid preconditioner
    /// output is reported as numerical breakdown.
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
            throw new ArgumentException("BiCGSTAB requires a square matrix.", nameof(matrix));
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

        var shadowResidual = (double[])residual.Clone();
        var direction = new double[n];
        var matrixDirection = new double[n];
        var preconditionedDirection = new double[n];
        var intermediateResidual = new double[n];
        var preconditionedIntermediate = new double[n];
        var matrixIntermediate = new double[n];

        var previousRho = 1.0;
        var alpha = 1.0;
        var omega = 1.0;
        var startsNewRecurrence = true;

        for (var iteration = 1; iteration <= settings.MaximumIterations; iteration++)
        {
            if (!TryDot(shadowResidual, residual, out var rho) || rho == 0.0)
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB shadow-residual inner product became zero or non-finite.");
            }

            if (startsNewRecurrence)
            {
                residual.CopyTo(direction, 0);
                startsNewRecurrence = false;
            }
            else
            {
                if (omega == 0.0 || !double.IsFinite(omega))
                {
                    return Breakdown(
                        solution,
                        iteration - 1,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "BiCGSTAB stabilization coefficient became zero or non-finite.");
                }

                var beta = (rho / previousRho) * (alpha / omega);
                if (!double.IsFinite(beta))
                {
                    return Breakdown(
                        solution,
                        iteration - 1,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "BiCGSTAB direction-update coefficient became non-finite.");
                }

                for (var index = 0; index < n; index++)
                {
                    var value = residual[index] + (beta * (direction[index] - (omega * matrixDirection[index])));
                    if (!double.IsFinite(value))
                    {
                        return Breakdown(
                            solution,
                            iteration - 1,
                            residualNorm,
                            initialResidualNorm,
                            rhsNorm,
                            convergenceThreshold,
                            "BiCGSTAB produced a non-finite search direction.");
                    }

                    direction[index] = value;
                }
            }

            if (!TryApplyPreconditioner(
                    preconditioner,
                    direction,
                    preconditionedDirection,
                    out var preconditionerError))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    preconditionerError);
            }

            try
            {
                matrix.Multiply(preconditionedDirection, matrixDirection);
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
                    $"Sparse matrix-vector multiplication failed during BiCGSTAB: {exception.Message}");
            }

            if (!TryDot(shadowResidual, matrixDirection, out var alphaDenominator)
                || alphaDenominator == 0.0)
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB alpha denominator became zero or non-finite.");
            }

            alpha = rho / alphaDenominator;
            if (!double.IsFinite(alpha))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB alpha coefficient became non-finite.");
            }

            for (var index = 0; index < n; index++)
            {
                var value = residual[index] - (alpha * matrixDirection[index]);
                if (!double.IsFinite(value))
                {
                    return Breakdown(
                        solution,
                        iteration - 1,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "BiCGSTAB intermediate residual became non-finite.");
                }

                intermediateResidual[index] = value;
            }

            if (!TryEuclideanNorm(intermediateResidual, out var intermediateNorm))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    double.NaN,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB intermediate residual norm is outside the finite double range.");
            }

            if (intermediateNorm <= convergenceThreshold)
            {
                if (!TryAddScaled(solution, alpha, preconditionedDirection, out var candidateError))
                {
                    return Breakdown(
                        solution,
                        iteration - 1,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        candidateError);
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

                RestartFromTrueResidual(residual, shadowResidual, direction, matrixDirection);
                previousRho = 1.0;
                alpha = 1.0;
                omega = 1.0;
                startsNewRecurrence = true;
                continue;
            }

            if (!TryApplyPreconditioner(
                    preconditioner,
                    intermediateResidual,
                    preconditionedIntermediate,
                    out preconditionerError))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    preconditionerError);
            }

            try
            {
                matrix.Multiply(preconditionedIntermediate, matrixIntermediate);
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
                    $"Sparse matrix-vector multiplication failed during BiCGSTAB stabilization: {exception.Message}");
            }

            if (!TryDot(matrixIntermediate, matrixIntermediate, out var omegaDenominator)
                || omegaDenominator == 0.0)
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB omega denominator became zero or non-finite before convergence.");
            }

            if (!TryDot(matrixIntermediate, intermediateResidual, out var omegaNumerator))
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB omega numerator became non-finite.");
            }

            omega = omegaNumerator / omegaDenominator;
            if (!double.IsFinite(omega) || omega == 0.0)
            {
                return Breakdown(
                    solution,
                    iteration - 1,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "BiCGSTAB stabilization coefficient became zero or non-finite.");
            }

            for (var index = 0; index < n; index++)
            {
                var nextSolution = solution[index]
                    + (alpha * preconditionedDirection[index])
                    + (omega * preconditionedIntermediate[index]);
                var nextResidual = intermediateResidual[index] - (omega * matrixIntermediate[index]);

                if (!double.IsFinite(nextSolution) || !double.IsFinite(nextResidual))
                {
                    return Breakdown(
                        solution,
                        iteration - 1,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "BiCGSTAB produced a non-finite solution or residual component.");
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
                    "BiCGSTAB recursive residual norm is outside the finite double range.");
            }

            if (residualNorm <= convergenceThreshold)
            {
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

                RestartFromTrueResidual(residual, shadowResidual, direction, matrixDirection);
                previousRho = 1.0;
                alpha = 1.0;
                omega = 1.0;
                startsNewRecurrence = true;
                continue;
            }

            previousRho = rho;
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
            "Maximum number of BiCGSTAB iterations reached.");
    }

    private static void RestartFromTrueResidual(
        double[] residual,
        double[] shadowResidual,
        double[] direction,
        double[] matrixDirection)
    {
        // Refreshing the shadow residual and clearing the short-recurrence state turns the next
        // iteration into a clean BiCGSTAB restart around the current physical solution.
        Array.Copy(residual, shadowResidual, residual.Length);
        Array.Clear(direction);
        Array.Clear(matrixDirection);
    }

    private static bool TryAddScaled(
        double[] target,
        double scale,
        ReadOnlySpan<double> direction,
        out string? error)
    {
        for (var index = 0; index < target.Length; index++)
        {
            var value = target[index] + (scale * direction[index]);
            if (!double.IsFinite(value))
            {
                error = "BiCGSTAB candidate solution became non-finite.";
                return false;
            }

            target[index] = value;
        }

        error = null;
        return true;
    }

    private static bool TryApplyPreconditioner(
        ISparsePreconditioner? preconditioner,
        ReadOnlySpan<double> source,
        Span<double> destination,
        out string? error)
    {
        if (preconditioner is null)
        {
            source.CopyTo(destination);
            error = null;
            return true;
        }

        try
        {
            preconditioner.Apply(source, destination);
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

            // Compensated accumulation reduces avoidable cancellation in the short recurrence
            // without hiding the algorithm behind an external vector dependency.
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
            (double[])solution.Clone(),
            iterations,
            IterationStatus.NumericalBreakdown,
            residualNorm,
            initialResidualNorm,
            rightHandSideNorm,
            convergenceThreshold,
            message);
}
