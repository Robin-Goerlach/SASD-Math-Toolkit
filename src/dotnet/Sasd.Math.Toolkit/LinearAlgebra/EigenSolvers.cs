using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Iterative eigenvalue routines for small and medium dense matrices.
/// </summary>
/// <remarks>
/// These are deliberately readable reference implementations. They expose convergence status
/// and residual diagnostics instead of hiding iterative failure behind a scalar return value.
/// Large sparse or high-performance workloads should eventually use specialized backends behind
/// separate SASD abstractions rather than complicating these textbook-oriented routines.
/// </remarks>
public static class EigenSolvers
{
    /// <summary>
    /// Approximates an eigenpair associated with the eigenvalue of largest magnitude.
    /// </summary>
    /// <param name="matrix">Square coefficient matrix. The input matrix is not modified.</param>
    /// <param name="initialVector">
    /// Optional finite non-zero start vector. When omitted, a deterministic all-ones vector is used.
    /// </param>
    /// <param name="tolerance">
    /// Absolute tolerance applied to both the Rayleigh-quotient change and the Euclidean eigenpair residual.
    /// </param>
    /// <param name="maximumIterations">Maximum number of power iterations.</param>
    /// <returns>The latest normalized eigenpair together with convergence diagnostics.</returns>
    /// <remarks>
    /// The power method requires a non-zero component in the desired dominant eigendirection. If
    /// a caller knows that the deterministic default could be orthogonal to that direction, an
    /// application-specific start vector should be supplied explicitly.
    /// </remarks>
    public static IterativeResult<Eigenpair> PowerMethod(
        DenseMatrix matrix,
        IReadOnlyList<double>? initialVector = null,
        double tolerance = 1e-10,
        int maximumIterations = 500)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));

        var x = CreateInitialVector(matrix.Rows, initialVector);
        var eigenvalue = 0.0;

        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var y = matrix.Multiply(x);
            if (!TryNormalizeInPlace(y))
            {
                return new IterativeResult<Eigenpair>(
                    new Eigenpair(0.0, x),
                    iteration - 1,
                    IterationStatus.NumericalBreakdown,
                    Message: "Power iteration reached the zero or numerically negligible vector.");
            }

            // Eigenvectors are sign-indeterminate. Aligning consecutive vectors prevents a
            // negative dominant eigenvalue from appearing as a harmless sign oscillation.
            AlignSign(y, x);

            var ay = matrix.Multiply(y);
            var nextEigenvalue = DotFinite(y, ay, "Rayleigh quotient overflowed.");
            var residual = ResidualNorm(ay, y, nextEigenvalue);

            if (System.Math.Abs(nextEigenvalue - eigenvalue) <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<Eigenpair>(
                    new Eigenpair(nextEigenvalue, y),
                    iteration,
                    IterationStatus.Converged,
                    residual);
            }

            x = y;
            eigenvalue = nextEigenvalue;
        }

        var finalAx = matrix.Multiply(x);
        return new IterativeResult<Eigenpair>(
            new Eigenpair(eigenvalue, x),
            maximumIterations,
            IterationStatus.MaximumIterationsReached,
            ResidualNorm(finalAx, x, eigenvalue),
            "Maximum number of power iterations reached before convergence.");
    }

    /// <summary>
    /// Approximates an eigenpair associated with the eigenvalue nearest zero using unshifted inverse iteration.
    /// </summary>
    /// <param name="matrix">Square, numerically nonsingular coefficient matrix.</param>
    /// <param name="initialVector">Optional finite non-zero start vector.</param>
    /// <param name="tolerance">Absolute eigenvalue-change and eigenpair-residual tolerance.</param>
    /// <param name="maximumIterations">Maximum number of inverse iterations.</param>
    /// <param name="pivotTolerance">Absolute LU pivot threshold used by the reusable factorization.</param>
    /// <remarks>
    /// The matrix is LU-factorized once and reused for all iterations. A singular or numerically
    /// singular matrix therefore raises <see cref="ArithmeticException"/> before iteration starts.
    /// This V1 API is intentionally unshifted; shifted inverse iteration belongs in a later explicit API.
    /// </remarks>
    public static IterativeResult<Eigenpair> InversePowerMethod(
        DenseMatrix matrix,
        IReadOnlyList<double>? initialVector = null,
        double tolerance = 1e-10,
        int maximumIterations = 100,
        double pivotTolerance = NumericConstants.NearlyZero)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));
        NumericGuard.Positive(pivotTolerance, nameof(pivotTolerance));

        var x = CreateInitialVector(matrix.Rows, initialVector);
        var eigenvalue = 0.0;

        // The matrix does not change during inverse iteration. Factoring once is part of the
        // numerical abstraction, not a micro-optimization: triangular solves are the repeated step.
        var factorization = LuFactorization.Decompose(matrix, pivotTolerance);

        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var y = factorization.Solve(x);
            if (!TryNormalizeInPlace(y))
            {
                return new IterativeResult<Eigenpair>(
                    new Eigenpair(eigenvalue, x),
                    iteration - 1,
                    IterationStatus.NumericalBreakdown,
                    Message: "Inverse iteration reached the zero or numerically negligible vector.");
            }

            AlignSign(y, x);
            var ay = matrix.Multiply(y);
            var nextEigenvalue = DotFinite(y, ay, "Rayleigh quotient overflowed.");
            var residual = ResidualNorm(ay, y, nextEigenvalue);

            if (System.Math.Abs(nextEigenvalue - eigenvalue) <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<Eigenpair>(
                    new Eigenpair(nextEigenvalue, y),
                    iteration,
                    IterationStatus.Converged,
                    residual);
            }

            x = y;
            eigenvalue = nextEigenvalue;
        }

        var finalAx = matrix.Multiply(x);
        return new IterativeResult<Eigenpair>(
            new Eigenpair(eigenvalue, x),
            maximumIterations,
            IterationStatus.MaximumIterationsReached,
            ResidualNorm(finalAx, x, eigenvalue),
            "Maximum number of inverse-power iterations reached before convergence.");
    }

    /// <summary>
    /// Computes the Euclidean defect <c>||A*v - lambda*v||2</c> of a claimed eigenpair.
    /// </summary>
    /// <remarks>
    /// A residual near zero shows that the supplied pair satisfies the eigen-equation well in an
    /// absolute sense. It does not by itself quantify eigenvalue conditioning or sensitivity.
    /// </remarks>
    public static double EigenpairResidualNorm(DenseMatrix matrix, Eigenpair eigenpair)
    {
        EnsureSquare(matrix);
        ArgumentNullException.ThrowIfNull(eigenpair);

        if (!double.IsFinite(eigenpair.Eigenvalue))
        {
            throw new ArgumentOutOfRangeException(nameof(eigenpair), "Eigenvalue must be finite before a residual can be evaluated.");
        }

        if (eigenpair.Dimension != matrix.Rows)
        {
            throw new ArgumentException("Eigenvector length must match matrix size.", nameof(eigenpair));
        }

        var multiplied = matrix.Multiply(eigenpair.Components);
        return ResidualNorm(multiplied, eigenpair.Components, eigenpair.Eigenvalue);
    }

    /// <summary>
    /// Finds the eigenpair with the second-largest magnitude by combining the power
    /// method with classical Wielandt deflation.
    /// </summary>
    /// <remarks>
    /// The dominant eigenpair is calculated first, removed by a rank-one Wielandt
    /// transformation and the power method is then applied to the deflated matrix.
    /// Finally the deflated eigenvector is mapped back to the original matrix.
    /// Repeated dominant eigenvalues are intrinsically ill-conditioned for this
    /// restoration and are reported as a numerical breakdown.
    /// </remarks>
    public static IterativeResult<Eigenpair> WielandtSecondEigenpair(
        DenseMatrix matrix,
        IReadOnlyList<double>? initialVector = null,
        double tolerance = 1e-10,
        int maximumIterations = 500)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));

        if (matrix.Rows < 2)
        {
            throw new ArgumentException("Wielandt deflation requires a matrix of order at least two.", nameof(matrix));
        }

        var dominant = PowerMethod(matrix, initialVector, tolerance, maximumIterations);
        if (!dominant.Converged)
        {
            return new IterativeResult<Eigenpair>(
                Eigenpair.Unavailable(matrix.Rows),
                dominant.Iterations,
                dominant.Status,
                dominant.Residual,
                "Dominant eigenpair did not converge; Wielandt deflation was not attempted.");
        }

        // The direct Wielandt API validates the claimed known eigenpair. Match that validation to
        // at least the requested power-method tolerance so a deliberately loose caller tolerance
        // does not cause the composite routine to reject its own accepted dominant pair.
        var deflationTolerance = System.Math.Max(1e-8, tolerance);
        var deflation = WielandtDeflation.Create(matrix, dominant.Value, deflationTolerance);

        // Do not start the second power iteration in a single coordinate direction: that direction
        // can itself be another exact eigenvector. Start broadly, then remove the component along
        // the deliberately deflated zero-eigenvalue direction.
        var deflatedInitial = CreateDeflatedInitialVector(dominant.Value.Components);
        var deflated = PowerMethod(deflation.DeflatedMatrix, deflatedInitial, tolerance, maximumIterations);
        var totalIterations = dominant.Iterations + deflated.Iterations;

        if (!deflated.Converged)
        {
            return new IterativeResult<Eigenpair>(
                Eigenpair.Unavailable(matrix.Rows),
                totalIterations,
                deflated.Status,
                deflated.Residual,
                "Power iteration on the Wielandt-deflated matrix did not converge.");
        }

        try
        {
            var restored = deflation.Restore(deflated.Value);
            var residual = EigenpairResidualNorm(matrix, restored);

            return new IterativeResult<Eigenpair>(
                restored,
                totalIterations,
                IterationStatus.Converged,
                residual);
        }
        catch (ArithmeticException exception)
        {
            return new IterativeResult<Eigenpair>(
                Eigenpair.Unavailable(matrix.Rows),
                totalIterations,
                IterationStatus.NumericalBreakdown,
                Message: exception.Message);
        }
    }

    /// <summary>
    /// Computes all eigenvalues and an orthonormal eigenvector basis of a real
    /// symmetric matrix using cyclic Jacobi rotations.
    /// </summary>
    /// <param name="matrix">Real symmetric input matrix. The input is not modified.</param>
    /// <param name="tolerance">Relative convergence tolerance for the off-diagonal Frobenius norm.</param>
    /// <param name="maximumSweeps">Maximum number of complete cyclic sweeps through all off-diagonal pairs.</param>
    /// <param name="symmetryTolerance">Relative tolerance used to validate matrix symmetry.</param>
    /// <remarks>
    /// The <see cref="IterativeResult{T}.Residual"/> is the off-diagonal Frobenius norm of the
    /// working matrix, not the maximum <c>||A*v-lambda*v||2</c> over returned eigenpairs. Use
    /// <see cref="EigenpairResidualNorm"/> when an application needs that independent check.
    /// </remarks>
    public static IterativeResult<SymmetricEigendecomposition> CyclicJacobi(
        DenseMatrix matrix,
        double tolerance = NumericConstants.DefaultTolerance,
        int maximumSweeps = 100,
        double symmetryTolerance = NumericConstants.DefaultTolerance)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumSweeps, nameof(maximumSweeps));
        NumericGuard.Positive(symmetryTolerance, nameof(symmetryTolerance));
        EnsureSymmetric(matrix, symmetryTolerance);

        var work = matrix.Clone();
        var eigenvectors = DenseMatrix.Identity(matrix.Rows);
        var scale = System.Math.Max(1.0, FrobeniusNorm(matrix));
        var target = tolerance * scale;
        EnsureFiniteComputation(target, "Jacobi convergence target overflowed.");

        var residual = OffDiagonalFrobeniusNorm(work);
        if (residual <= target)
        {
            return new IterativeResult<SymmetricEigendecomposition>(
                BuildSortedSymmetricEigendecomposition(work, eigenvectors),
                0,
                IterationStatus.Converged,
                residual);
        }

        for (var sweep = 1; sweep <= maximumSweeps; sweep++)
        {
            for (var p = 0; p < work.Rows - 1; p++)
            {
                for (var q = p + 1; q < work.Columns; q++)
                {
                    ApplyJacobiRotation(work, eigenvectors, p, q);
                }
            }

            residual = OffDiagonalFrobeniusNorm(work);
            if (residual <= target)
            {
                return new IterativeResult<SymmetricEigendecomposition>(
                    BuildSortedSymmetricEigendecomposition(work, eigenvectors),
                    sweep,
                    IterationStatus.Converged,
                    residual);
            }
        }

        return new IterativeResult<SymmetricEigendecomposition>(
            BuildSortedSymmetricEigendecomposition(work, eigenvectors),
            maximumSweeps,
            IterationStatus.MaximumIterationsReached,
            residual,
            "Maximum number of Jacobi sweeps reached before the off-diagonal norm met the requested tolerance.");
    }

    private static void ApplyJacobiRotation(DenseMatrix matrix, DenseMatrix eigenvectors, int p, int q)
    {
        var apq = matrix[p, q];
        if (System.Math.Abs(apq) <= NumericConstants.NearlyZero)
        {
            return;
        }

        var app = matrix[p, p];
        var aqq = matrix[q, q];
        var tangent = ComputeJacobiTangent(app, aqq, apq);
        var cosine = 1.0 / System.Math.Sqrt(1.0 + (tangent * tangent));
        var sine = tangent * cosine;

        var diagonalCorrection = tangent * apq;
        var newApp = app - diagonalCorrection;
        var newAqq = aqq + diagonalCorrection;
        EnsureFiniteComputation(newApp, "Jacobi rotation produced a non-finite diagonal entry.");
        EnsureFiniteComputation(newAqq, "Jacobi rotation produced a non-finite diagonal entry.");

        matrix[p, p] = newApp;
        matrix[q, q] = newAqq;
        matrix[p, q] = 0.0;
        matrix[q, p] = 0.0;

        // Update both symmetric halves explicitly. The straightforward form keeps the reference
        // implementation easy to inspect and leaves storage optimizations to a future backend.
        for (var k = 0; k < matrix.Rows; k++)
        {
            if (k == p || k == q)
            {
                continue;
            }

            var akp = matrix[k, p];
            var akq = matrix[k, q];
            var rotatedP = (cosine * akp) - (sine * akq);
            var rotatedQ = (sine * akp) + (cosine * akq);
            EnsureFiniteComputation(rotatedP, "Jacobi rotation overflowed while updating the matrix.");
            EnsureFiniteComputation(rotatedQ, "Jacobi rotation overflowed while updating the matrix.");

            matrix[k, p] = rotatedP;
            matrix[p, k] = rotatedP;
            matrix[k, q] = rotatedQ;
            matrix[q, k] = rotatedQ;
        }

        // Accumulate the same orthogonal rotations so the final columns are eigenvectors of the
        // original matrix rather than of the transformed working copy.
        for (var row = 0; row < eigenvectors.Rows; row++)
        {
            var vip = eigenvectors[row, p];
            var viq = eigenvectors[row, q];
            var rotatedP = (cosine * vip) - (sine * viq);
            var rotatedQ = (sine * vip) + (cosine * viq);
            EnsureFiniteComputation(rotatedP, "Jacobi eigenvector accumulation overflowed.");
            EnsureFiniteComputation(rotatedQ, "Jacobi eigenvector accumulation overflowed.");
            eigenvectors[row, p] = rotatedP;
            eigenvectors[row, q] = rotatedQ;
        }
    }

    private static double ComputeJacobiTangent(double app, double aqq, double apq)
    {
        // Scale the three relevant entries before forming tau. This avoids overflow in aqq-app
        // for very large finite matrices while preserving the dimensionless rotation ratio.
        var scale = System.Math.Max(
            System.Math.Abs(apq),
            System.Math.Max(System.Math.Abs(app), System.Math.Abs(aqq)));
        if (scale == 0.0)
        {
            return 0.0;
        }

        var scaledApq = apq / scale;
        if (scaledApq == 0.0)
        {
            // The coupling is below representable scale compared with the diagonal entries.
            return 0.0;
        }

        var tau = ((aqq / scale) - (app / scale)) / (2.0 * scaledApq);
        if (tau == 0.0)
        {
            return 1.0;
        }

        var sign = tau < 0.0 ? -1.0 : 1.0;
        var absoluteTau = System.Math.Abs(tau);
        if (double.IsPositiveInfinity(absoluteTau))
        {
            return 0.0;
        }

        // Two algebraically equivalent branches avoid both tau^2 overflow and cancellation.
        if (absoluteTau > 1.0)
        {
            var reciprocal = 1.0 / absoluteTau;
            return sign * reciprocal / (1.0 + System.Math.Sqrt(1.0 + (reciprocal * reciprocal)));
        }

        return sign / (absoluteTau + System.Math.Sqrt(1.0 + (absoluteTau * absoluteTau)));
    }

    private static SymmetricEigendecomposition BuildSortedSymmetricEigendecomposition(
        DenseMatrix diagonalized,
        DenseMatrix eigenvectors)
    {
        var order = Enumerable.Range(0, diagonalized.Rows)
            .OrderByDescending(index => diagonalized[index, index])
            .ToArray();

        var eigenvalues = new double[diagonalized.Rows];
        var sortedVectors = new DenseMatrix(diagonalized.Rows, diagonalized.Columns);

        for (var newColumn = 0; newColumn < order.Length; newColumn++)
        {
            var oldColumn = order[newColumn];
            eigenvalues[newColumn] = diagonalized[oldColumn, oldColumn];
            for (var row = 0; row < diagonalized.Rows; row++)
            {
                sortedVectors[row, newColumn] = eigenvectors[row, oldColumn];
            }
        }

        return new SymmetricEigendecomposition(eigenvalues, sortedVectors);
    }

    private static double[] CreateInitialVector(int size, IReadOnlyList<double>? initialVector)
    {
        var vector = initialVector?.ToArray() ?? Enumerable.Repeat(1.0, size).ToArray();
        if (vector.Length != size)
        {
            throw new ArgumentException("Initial vector length must match matrix size.", nameof(initialVector));
        }

        for (var i = 0; i < vector.Length; i++)
        {
            if (!double.IsFinite(vector[i]))
            {
                throw new ArgumentOutOfRangeException(nameof(initialVector), "Initial vector components must be finite.");
            }
        }

        if (!TryNormalizeInPlace(vector))
        {
            throw new ArgumentException("Initial vector must not be zero or numerically negligible.", nameof(initialVector));
        }

        return vector;
    }

    private static double[] CreateDeflatedInitialVector(IReadOnlyList<double> removedEigenvector)
    {
        var denominator = DotFinite(removedEigenvector, removedEigenvector, "Deflated start-vector projection overflowed.");
        if (denominator <= NumericConstants.NearlyZero)
        {
            throw new ArgumentException("Removed eigenvector must not be the zero vector.", nameof(removedEigenvector));
        }

        var vector = Enumerable.Repeat(1.0, removedEigenvector.Count).ToArray();
        RemoveProjection(vector, removedEigenvector, denominator);
        if (StableEuclideanNorm(vector) > NumericConstants.NearlyZero)
        {
            return vector;
        }

        for (var candidate = 0; candidate < removedEigenvector.Count; candidate++)
        {
            Array.Clear(vector);
            vector[candidate] = 1.0;
            RemoveProjection(vector, removedEigenvector, denominator);
            if (StableEuclideanNorm(vector) > NumericConstants.NearlyZero)
            {
                return vector;
            }
        }

        throw new ArithmeticException("Unable to construct an initial vector for the deflated power iteration.");
    }

    private static void RemoveProjection(double[] vector, IReadOnlyList<double> direction, double directionNormSquared)
    {
        var coefficient = DotFinite(vector, direction, "Projection coefficient overflowed.") / directionNormSquared;
        EnsureFiniteComputation(coefficient, "Projection coefficient became non-finite.");

        for (var i = 0; i < vector.Length; i++)
        {
            var value = vector[i] - (coefficient * direction[i]);
            EnsureFiniteComputation(value, "Projection removal overflowed.");
            vector[i] = value;
        }
    }

    private static void EnsureSquare(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Matrix must be square.", nameof(matrix));
        }
    }

    private static void EnsureSymmetric(DenseMatrix matrix, double tolerance)
    {
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = row + 1; column < matrix.Columns; column++)
            {
                var left = matrix[row, column];
                var right = matrix[column, row];
                var scale = System.Math.Max(1.0, System.Math.Max(System.Math.Abs(left), System.Math.Abs(right)));
                if (System.Math.Abs(left - right) > tolerance * scale)
                {
                    throw new ArgumentException("Cyclic Jacobi requires a real symmetric matrix.", nameof(matrix));
                }
            }
        }
    }

    private static bool TryNormalizeInPlace(double[] vector)
    {
        var scale = 0.0;
        for (var i = 0; i < vector.Length; i++)
        {
            var value = vector[i];
            if (!double.IsFinite(value))
            {
                throw new ArithmeticException("Vector normalization encountered a non-finite component.");
            }

            scale = System.Math.Max(scale, System.Math.Abs(value));
        }

        if (scale <= NumericConstants.NearlyZero)
        {
            return false;
        }

        // Divide by the largest magnitude before forming the norm. This lets a valid vector such
        // as [1e308, 1e308] be normalized without squaring its components and overflowing.
        var scaledSumSquares = 0.0;
        for (var i = 0; i < vector.Length; i++)
        {
            var scaled = vector[i] / scale;
            scaledSumSquares += scaled * scaled;
        }

        var scaledNorm = System.Math.Sqrt(scaledSumSquares);
        EnsureFiniteComputation(scaledNorm, "Vector normalization produced a non-finite norm.");
        if (scaledNorm == 0.0)
        {
            return false;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (vector[i] / scale) / scaledNorm;
        }

        return true;
    }

    private static void AlignSign(double[] vector, IReadOnlyList<double> reference)
    {
        if (DotFinite(vector, reference, "Vector sign alignment overflowed.") < 0.0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] = -vector[i];
            }
        }
    }

    private static double DotFinite(IReadOnlyList<double> left, IReadOnlyList<double> right, string overflowMessage)
    {
        if (left.Count != right.Count)
        {
            throw new ArgumentException("Vector dimensions must match.");
        }

        var sum = 0.0;
        for (var i = 0; i < left.Count; i++)
        {
            var product = left[i] * right[i];
            EnsureFiniteComputation(product, overflowMessage);
            sum += product;
            EnsureFiniteComputation(sum, overflowMessage);
        }

        return sum;
    }

    private static double StableEuclideanNorm(IReadOnlyList<double> vector)
    {
        var scale = 0.0;
        var scaledSumSquares = 1.0;
        for (var i = 0; i < vector.Count; i++)
        {
            AddToScaledSum(vector[i], ref scale, ref scaledSumSquares);
        }

        return FinishScaledNorm(scale, scaledSumSquares);
    }

    private static double FrobeniusNorm(DenseMatrix matrix)
    {
        var scale = 0.0;
        var scaledSumSquares = 1.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                AddToScaledSum(matrix[row, column], ref scale, ref scaledSumSquares);
            }
        }

        return FinishScaledNorm(scale, scaledSumSquares);
    }

    private static double OffDiagonalFrobeniusNorm(DenseMatrix matrix)
    {
        var scale = 0.0;
        var scaledSumSquares = 1.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = row + 1; column < matrix.Columns; column++)
            {
                var value = matrix[row, column];
                AddToScaledSum(value, ref scale, ref scaledSumSquares);
                AddToScaledSum(value, ref scale, ref scaledSumSquares);
            }
        }

        return FinishScaledNorm(scale, scaledSumSquares);
    }

    private static double ResidualNorm(IReadOnlyList<double> ax, IReadOnlyList<double> x, double lambda)
    {
        if (ax.Count != x.Count)
        {
            throw new ArgumentException("Residual vectors must have the same dimension.");
        }

        EnsureFiniteComputation(lambda, "Eigenvalue became non-finite while evaluating a residual.");

        var scale = 0.0;
        var scaledSumSquares = 1.0;
        for (var i = 0; i < ax.Count; i++)
        {
            var product = lambda * x[i];
            EnsureFiniteComputation(product, "Eigenpair residual overflowed while scaling the eigenvector.");
            var residual = ax[i] - product;
            EnsureFiniteComputation(residual, "Eigenpair residual overflowed.");
            AddToScaledSum(residual, ref scale, ref scaledSumSquares);
        }

        return FinishScaledNorm(scale, scaledSumSquares);
    }

    private static void AddToScaledSum(double value, ref double scale, ref double scaledSumSquares)
    {
        EnsureFiniteComputation(value, "Norm calculation encountered a non-finite value.");
        var absolute = System.Math.Abs(value);
        if (absolute == 0.0)
        {
            return;
        }

        if (scale < absolute)
        {
            var ratio = scale / absolute;
            scaledSumSquares = 1.0 + (scaledSumSquares * ratio * ratio);
            scale = absolute;
        }
        else
        {
            var ratio = absolute / scale;
            scaledSumSquares += ratio * ratio;
        }
    }

    private static double FinishScaledNorm(double scale, double scaledSumSquares)
    {
        if (scale == 0.0)
        {
            return 0.0;
        }

        var norm = scale * System.Math.Sqrt(scaledSumSquares);
        EnsureFiniteComputation(norm, "Norm exceeds the finite range of double precision.");
        return norm;
    }

    private static void EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
