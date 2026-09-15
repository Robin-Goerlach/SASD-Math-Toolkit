using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Restarted Generalized Minimal Residual solver for real square sparse linear systems.
/// </summary>
/// <remarks>
/// <para>
/// GMRES targets general square systems <c>A*x=b</c>, including nonsymmetric matrices for which
/// Conjugate Gradient is not mathematically applicable. The implementation uses the canonical CSR
/// matrix-vector product, modified Gram-Schmidt Arnoldi orthogonalization with a second correction
/// pass, and Givens rotations to maintain the small least-squares problem incrementally.
/// </para>
/// <para>
/// Restarting bounds Krylov-basis memory. A restart length of <c>m</c> stores at most <c>m+1</c>
/// orthonormal basis vectors plus <c>m</c> preconditioned basis vectors. Smaller restart lengths save
/// memory but can slow or even stall convergence; the parameter is therefore explicit rather than a
/// hidden global setting.
/// </para>
/// <para>
/// Optional preconditioning is applied on the <em>right</em>: GMRES builds the Krylov space of
/// <c>A*M^-1</c> and updates the physical solution with preconditioned basis vectors. This keeps the
/// minimized residual in the original system <c>b-A*x</c>, so the common SASD sparse convergence rule
/// remains meaningful without translating tolerances into a preconditioned norm.
/// </para>
/// </remarks>
public static class GmresSolver
{
    private const double MachineEpsilon = 2.2204460492503131e-16;

    /// <summary>
    /// Solves a general square sparse system using restarted GMRES without preconditioning.
    /// </summary>
    /// <param name="matrix">Square canonical CSR system matrix.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="restartLength">Maximum Krylov basis size before a restart.</param>
    /// <param name="initialGuess">Optional finite initial solution; zero is used by default.</param>
    /// <param name="options">Shared sparse convergence options.</param>
    public static SparseLinearSolveResult Solve(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        int restartLength = 30,
        IReadOnlyList<double>? initialGuess = null,
        SparseIterativeSolverOptions? options = null) =>
        SolveCore(matrix, rightHandSide, restartLength, initialGuess, preconditioner: null, options);

    /// <summary>
    /// Solves a general square sparse system using restarted right-preconditioned GMRES.
    /// </summary>
    /// <param name="matrix">Square canonical CSR system matrix.</param>
    /// <param name="rightHandSide">Finite right-hand-side vector.</param>
    /// <param name="preconditioner">Approximate-inverse operation used as a right preconditioner.</param>
    /// <param name="restartLength">Maximum Krylov basis size before a restart.</param>
    /// <param name="initialGuess">Optional finite initial solution; zero is used by default.</param>
    /// <param name="options">Shared sparse convergence options.</param>
    /// <remarks>
    /// The preconditioner is expected to behave as a fixed deterministic linear approximate inverse
    /// during a solve. Unlike PCG, GMRES does not require the preconditioner to be symmetric positive
    /// definite.
    /// </remarks>
    public static SparseLinearSolveResult SolvePreconditioned(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        ISparsePreconditioner preconditioner,
        int restartLength = 30,
        IReadOnlyList<double>? initialGuess = null,
        SparseIterativeSolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(preconditioner);
        return SolveCore(matrix, rightHandSide, restartLength, initialGuess, preconditioner, options);
    }

    private static SparseLinearSolveResult SolveCore(
        CsrMatrix matrix,
        IReadOnlyList<double> rightHandSide,
        int restartLength,
        IReadOnlyList<double>? initialGuess,
        ISparsePreconditioner? preconditioner,
        SparseIterativeSolverOptions? options)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(rightHandSide);

        if (!matrix.IsSquare)
        {
            throw new ArgumentException("GMRES requires a square matrix.", nameof(matrix));
        }

        if (restartLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restartLength), "Restart length must be greater than zero.");
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

        var iterations = 0;
        var effectiveRestartLength = System.Math.Min(restartLength, n);

        while (iterations < settings.MaximumIterations)
        {
            var stepsThisCycle = System.Math.Min(
                effectiveRestartLength,
                settings.MaximumIterations - iterations);

            var basis = new double[stepsThisCycle + 1][];
            var preconditionedBasis = new double[stepsThisCycle][];
            var hessenberg = new double[stepsThisCycle + 1, stepsThisCycle];
            var cosines = new double[stepsThisCycle];
            var sines = new double[stepsThisCycle];
            var projectedRightHandSide = new double[stepsThisCycle + 1];

            basis[0] = new double[n];
            var inverseResidualNorm = 1.0 / residualNorm;
            if (!double.IsFinite(inverseResidualNorm))
            {
                return Breakdown(
                    solution,
                    iterations,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    "GMRES could not normalize the restart residual within the finite double range.");
            }

            for (var index = 0; index < n; index++)
            {
                basis[0][index] = residual[index] * inverseResidualNorm;
            }

            projectedRightHandSide[0] = residualNorm;
            var completedSteps = 0;
            var restartRequested = false;

            for (var column = 0; column < stepsThisCycle; column++)
            {
                var preconditionedVector = new double[n];
                if (!TryApplyPreconditioner(
                        preconditioner,
                        basis[column],
                        preconditionedVector,
                        out var preconditionerError))
                {
                    return Breakdown(
                        solution,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        preconditionerError);
                }

                preconditionedBasis[column] = preconditionedVector;

                var work = new double[n];
                try
                {
                    matrix.Multiply(preconditionedVector, work);
                }
                catch (ArithmeticException exception)
                {
                    return Breakdown(
                        solution,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        $"Sparse matrix-vector multiplication failed during GMRES: {exception.Message}");
                }

                if (!TryEuclideanNorm(work, out var initialWorkNorm))
                {
                    return Breakdown(
                        solution,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "GMRES Arnoldi vector norm became non-finite.");
                }

                // Modified Gram-Schmidt is applied twice. The second pass is intentionally retained
                // in the managed reference implementation because loss of orthogonality can damage
                // GMRES residual estimates long before it becomes visually obvious in the basis.
                for (var pass = 0; pass < 2; pass++)
                {
                    for (var row = 0; row <= column; row++)
                    {
                        if (!TryDot(work, basis[row], out var coefficient))
                        {
                            return Breakdown(
                                solution,
                                iterations,
                                residualNorm,
                                initialResidualNorm,
                                rhsNorm,
                                convergenceThreshold,
                                "GMRES Arnoldi inner product became non-finite.");
                        }

                        hessenberg[row, column] += coefficient;
                        for (var index = 0; index < n; index++)
                        {
                            var next = work[index] - (coefficient * basis[row][index]);
                            if (!double.IsFinite(next))
                            {
                                return Breakdown(
                                    solution,
                                    iterations,
                                    residualNorm,
                                    initialResidualNorm,
                                    rhsNorm,
                                    convergenceThreshold,
                                    "GMRES orthogonalization produced a non-finite component.");
                            }

                            work[index] = next;
                        }
                    }
                }

                if (!TryEuclideanNorm(work, out var nextBasisNorm))
                {
                    return Breakdown(
                        solution,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "GMRES orthogonalized Arnoldi vector norm became non-finite.");
                }

                hessenberg[column + 1, column] = nextBasisNorm;
                var arnoldiThreshold = 64.0 * MachineEpsilon * initialWorkNorm;
                var happyBreakdown = nextBasisNorm <= arnoldiThreshold;

                if (!happyBreakdown)
                {
                    var inverseNorm = 1.0 / nextBasisNorm;
                    if (!double.IsFinite(inverseNorm))
                    {
                        return Breakdown(
                            solution,
                            iterations,
                            residualNorm,
                            initialResidualNorm,
                            rhsNorm,
                            convergenceThreshold,
                            "GMRES could not normalize an Arnoldi basis vector.");
                    }

                    var nextBasis = new double[n];
                    for (var index = 0; index < n; index++)
                    {
                        nextBasis[index] = work[index] * inverseNorm;
                    }

                    basis[column + 1] = nextBasis;
                }

                // Apply all previously accumulated Givens rotations to the new Hessenberg column.
                for (var rotation = 0; rotation < column; rotation++)
                {
                    var upper = hessenberg[rotation, column];
                    var lower = hessenberg[rotation + 1, column];
                    hessenberg[rotation, column] =
                        (cosines[rotation] * upper) + (sines[rotation] * lower);
                    hessenberg[rotation + 1, column] =
                        (-sines[rotation] * upper) + (cosines[rotation] * lower);
                }

                if (!TryCreateGivensRotation(
                        hessenberg[column, column],
                        hessenberg[column + 1, column],
                        out var cosine,
                        out var sine,
                        out var rotatedDiagonal))
                {
                    return Breakdown(
                        solution,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "GMRES could not construct a finite Givens rotation.");
                }

                cosines[column] = cosine;
                sines[column] = sine;
                hessenberg[column, column] = rotatedDiagonal;
                hessenberg[column + 1, column] = 0.0;

                var projected = projectedRightHandSide[column];
                projectedRightHandSide[column] = cosine * projected;
                projectedRightHandSide[column + 1] = -sine * projected;

                iterations++;
                completedSteps = column + 1;
                var residualEstimate = System.Math.Abs(projectedRightHandSide[column + 1]);

                var mustInspectCandidate =
                    residualEstimate <= convergenceThreshold
                    || happyBreakdown
                    || iterations == settings.MaximumIterations;

                if (!mustInspectCandidate)
                {
                    continue;
                }

                if (!TryBuildCandidate(
                        solution,
                        preconditionedBasis,
                        hessenberg,
                        projectedRightHandSide,
                        completedSteps,
                        out var candidate,
                        out var candidateError))
                {
                    return Breakdown(
                        solution,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        candidateError);
                }

                if (!TryComputeTrueResidual(
                        matrix,
                        candidate,
                        rhs,
                        residual,
                        matrixVector,
                        out residualNorm,
                        out residualError))
                {
                    return Breakdown(
                        candidate,
                        iterations,
                        double.NaN,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        residualError);
                }

                if (residualNorm <= convergenceThreshold)
                {
                    return new SparseLinearSolveResult(
                        candidate,
                        iterations,
                        IterationStatus.Converged,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold);
                }

                if (happyBreakdown)
                {
                    return Breakdown(
                        candidate,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold,
                        "GMRES Arnoldi expansion terminated before the requested residual tolerance was reached.");
                }

                if (iterations == settings.MaximumIterations)
                {
                    return MaximumIterations(
                        candidate,
                        iterations,
                        residualNorm,
                        initialResidualNorm,
                        rhsNorm,
                        convergenceThreshold);
                }

                solution = candidate;
                restartRequested = true;
                break;
            }

            if (restartRequested)
            {
                continue;
            }

            if (!TryBuildCandidate(
                    solution,
                    preconditionedBasis,
                    hessenberg,
                    projectedRightHandSide,
                    completedSteps,
                    out var cycleCandidate,
                    out var cycleError))
            {
                return Breakdown(
                    solution,
                    iterations,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    cycleError);
            }

            if (!TryComputeTrueResidual(
                    matrix,
                    cycleCandidate,
                    rhs,
                    residual,
                    matrixVector,
                    out residualNorm,
                    out residualError))
            {
                return Breakdown(
                    cycleCandidate,
                    iterations,
                    double.NaN,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold,
                    residualError);
            }

            if (residualNorm <= convergenceThreshold)
            {
                return new SparseLinearSolveResult(
                    cycleCandidate,
                    iterations,
                    IterationStatus.Converged,
                    residualNorm,
                    initialResidualNorm,
                    rhsNorm,
                    convergenceThreshold);
            }

            solution = cycleCandidate;
        }

        return MaximumIterations(
            solution,
            iterations,
            residualNorm,
            initialResidualNorm,
            rhsNorm,
            convergenceThreshold);
    }

    private static bool TryBuildCandidate(
        double[] baseSolution,
        double[][] preconditionedBasis,
        double[,] hessenberg,
        double[] projectedRightHandSide,
        int stepCount,
        out double[] candidate,
        out string? error)
    {
        candidate = (double[])baseSolution.Clone();
        if (stepCount <= 0)
        {
            error = "GMRES restart cycle did not complete an Arnoldi step.";
            return false;
        }

        var coefficients = new double[stepCount];
        for (var row = stepCount - 1; row >= 0; row--)
        {
            var value = projectedRightHandSide[row];
            for (var column = row + 1; column < stepCount; column++)
            {
                value -= hessenberg[row, column] * coefficients[column];
            }

            var diagonal = hessenberg[row, row];
            if (!double.IsFinite(value) || !double.IsFinite(diagonal) || diagonal == 0.0)
            {
                error = "GMRES projected upper-triangular system is singular or non-finite.";
                return false;
            }

            var coefficient = value / diagonal;
            if (!double.IsFinite(coefficient))
            {
                error = "GMRES projected solve produced a non-finite coefficient.";
                return false;
            }

            coefficients[row] = coefficient;
        }

        for (var basisIndex = 0; basisIndex < stepCount; basisIndex++)
        {
            var vector = preconditionedBasis[basisIndex];
            if (vector is null)
            {
                error = "GMRES preconditioned basis is incomplete.";
                return false;
            }

            var coefficient = coefficients[basisIndex];
            for (var index = 0; index < candidate.Length; index++)
            {
                var next = candidate[index] + (coefficient * vector[index]);
                if (!double.IsFinite(next))
                {
                    error = "GMRES solution update produced a non-finite component.";
                    return false;
                }

                candidate[index] = next;
            }
        }

        error = null;
        return true;
    }

    private static bool TryCreateGivensRotation(
        double upper,
        double lower,
        out double cosine,
        out double sine,
        out double rotatedDiagonal)
    {
        if (!double.IsFinite(upper) || !double.IsFinite(lower))
        {
            cosine = double.NaN;
            sine = double.NaN;
            rotatedDiagonal = double.NaN;
            return false;
        }

        if (lower == 0.0)
        {
            cosine = 1.0;
            sine = 0.0;
            rotatedDiagonal = upper;
            return double.IsFinite(rotatedDiagonal);
        }

        if (upper == 0.0)
        {
            cosine = 0.0;
            sine = 1.0;
            rotatedDiagonal = lower;
            return double.IsFinite(rotatedDiagonal);
        }

        var scale = System.Math.Max(System.Math.Abs(upper), System.Math.Abs(lower));
        var scaledUpper = upper / scale;
        var scaledLower = lower / scale;
        var magnitude = scale * System.Math.Sqrt((scaledUpper * scaledUpper) + (scaledLower * scaledLower));
        if (!double.IsFinite(magnitude) || magnitude == 0.0)
        {
            cosine = double.NaN;
            sine = double.NaN;
            rotatedDiagonal = double.NaN;
            return false;
        }

        cosine = upper / magnitude;
        sine = lower / magnitude;
        rotatedDiagonal = magnitude;
        return double.IsFinite(cosine) && double.IsFinite(sine);
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

    private static SparseLinearSolveResult MaximumIterations(
        double[] solution,
        int iterations,
        double residualNorm,
        double initialResidualNorm,
        double rightHandSideNorm,
        double convergenceThreshold) =>
        new(
            solution,
            iterations,
            IterationStatus.MaximumIterationsReached,
            residualNorm,
            initialResidualNorm,
            rightHandSideNorm,
            convergenceThreshold,
            "Maximum number of GMRES iterations reached.");
}
