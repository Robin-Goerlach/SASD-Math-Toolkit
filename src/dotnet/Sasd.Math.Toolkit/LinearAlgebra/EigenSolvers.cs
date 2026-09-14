using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

public sealed record Eigenpair(double Eigenvalue, double[] Eigenvector);

/// <summary>
/// Iterative eigenvalue routines for small and medium dense matrices.
/// </summary>
public static class EigenSolvers
{
    public static IterativeResult<Eigenpair> PowerMethod(
        DenseMatrix matrix,
        IReadOnlyList<double>? initialVector = null,
        double tolerance = 1e-10,
        int maximumIterations = 500)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));
        var n = matrix.Rows;
        var x = initialVector?.ToArray() ?? Enumerable.Repeat(1.0, n).ToArray();
        if (x.Length != n) throw new ArgumentException("Initial vector length must match matrix size.", nameof(initialVector));
        Normalize(x);
        var eigenvalue = 0.0;

        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var y = matrix.Multiply(x);
            var norm = EuclideanNorm(y);
            if (norm <= NumericConstants.NearlyZero)
            {
                return new IterativeResult<Eigenpair>(new Eigenpair(0.0, x), iteration - 1,
                    IterationStatus.NumericalBreakdown, Message: "Power iteration reached the zero vector.");
            }
            for (var i = 0; i < n; i++) y[i] /= norm;
            AlignSign(y, x);
            var ay = matrix.Multiply(y);
            var nextEigenvalue = Dot(y, ay);
            var residual = ResidualNorm(ay, y, nextEigenvalue);
            if (System.Math.Abs(nextEigenvalue - eigenvalue) <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<Eigenpair>(new Eigenpair(nextEigenvalue, y), iteration,
                    IterationStatus.Converged, residual);
            }
            x = y;
            eigenvalue = nextEigenvalue;
        }

        var finalAx = matrix.Multiply(x);
        return new IterativeResult<Eigenpair>(new Eigenpair(eigenvalue, x), maximumIterations,
            IterationStatus.MaximumIterationsReached, ResidualNorm(finalAx, x, eigenvalue));
    }

    public static IterativeResult<Eigenpair> InversePowerMethod(
        DenseMatrix matrix,
        IReadOnlyList<double>? initialVector = null,
        double tolerance = 1e-10,
        int maximumIterations = 100)
    {
        EnsureSquare(matrix);
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));
        var n = matrix.Rows;
        var x = initialVector?.ToArray() ?? Enumerable.Repeat(1.0, n).ToArray();
        if (x.Length != n) throw new ArgumentException("Initial vector length must match matrix size.", nameof(initialVector));
        Normalize(x);
        var eigenvalue = 0.0;

        // The matrix does not change during inverse iteration, so factor it once and
        // reuse the decomposition instead of repeating Gaussian elimination at every step.
        var factorization = LuFactorization.Decompose(matrix);

        for (var iteration = 1; iteration <= maximumIterations; iteration++)
        {
            var y = factorization.Solve(x);
            Normalize(y);
            AlignSign(y, x);
            var ay = matrix.Multiply(y);
            var nextEigenvalue = Dot(y, ay);
            var residual = ResidualNorm(ay, y, nextEigenvalue);
            if (System.Math.Abs(nextEigenvalue - eigenvalue) <= tolerance && residual <= tolerance)
            {
                return new IterativeResult<Eigenpair>(new Eigenpair(nextEigenvalue, y), iteration,
                    IterationStatus.Converged, residual);
            }
            x = y;
            eigenvalue = nextEigenvalue;
        }

        var finalAx = matrix.Multiply(x);
        return new IterativeResult<Eigenpair>(new Eigenpair(eigenvalue, x), maximumIterations,
            IterationStatus.MaximumIterationsReached, ResidualNorm(finalAx, x, eigenvalue));
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
                new Eigenpair(double.NaN, new double[matrix.Rows]),
                dominant.Iterations,
                dominant.Status,
                dominant.Residual,
                "Dominant eigenpair did not converge; Wielandt deflation was not attempted.");
        }

        var deflation = WielandtDeflation.Create(matrix, dominant.Value);

        // Do not start the second power iteration in a single coordinate direction:
        // that direction can itself be another exact eigenvector and would make the
        // iteration converge to whichever eigenvalue happens to own that coordinate.
        // Instead use a deterministic broad vector and remove its component along the
        // deflated zero-eigenvalue direction. A basis-vector fallback handles the case
        // where the removed eigenvector itself is proportional to the all-ones vector.
        var deflatedInitial = CreateDeflatedInitialVector(dominant.Value.Eigenvector);
        var deflated = PowerMethod(deflation.DeflatedMatrix, deflatedInitial, tolerance, maximumIterations);
        var totalIterations = dominant.Iterations + deflated.Iterations;

        if (!deflated.Converged)
        {
            return new IterativeResult<Eigenpair>(
                new Eigenpair(double.NaN, new double[matrix.Rows]),
                totalIterations,
                deflated.Status,
                deflated.Residual,
                "Power iteration on the Wielandt-deflated matrix did not converge.");
        }

        try
        {
            var restored = deflation.Restore(deflated.Value);
            var multiplied = matrix.Multiply(restored.Eigenvector);
            var residual = ResidualNorm(multiplied, restored.Eigenvector, restored.Eigenvalue);

            return new IterativeResult<Eigenpair>(
                restored,
                totalIterations,
                IterationStatus.Converged,
                residual);
        }
        catch (ArithmeticException exception)
        {
            return new IterativeResult<Eigenpair>(
                new Eigenpair(double.NaN, new double[matrix.Rows]),
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
        var tau = (aqq - app) / (2.0 * apq);

        // This algebraic form chooses the smaller-magnitude tangent root and avoids
        // the cancellation that occurs when solving the rotation quadratic directly.
        var tangent = tau == 0.0
            ? 1.0
            : System.Math.Sign(tau) / (System.Math.Abs(tau) + System.Math.Sqrt(1.0 + (tau * tau)));
        var cosine = 1.0 / System.Math.Sqrt(1.0 + (tangent * tangent));
        var sine = tangent * cosine;

        matrix[p, p] = app - (tangent * apq);
        matrix[q, q] = aqq + (tangent * apq);
        matrix[p, q] = 0.0;
        matrix[q, p] = 0.0;

        // Update both symmetric halves explicitly. The straightforward form keeps the
        // reference implementation easy to inspect; later optimization can exploit
        // symmetry/storage without changing the public algorithm contract.
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

            matrix[k, p] = rotatedP;
            matrix[p, k] = rotatedP;
            matrix[k, q] = rotatedQ;
            matrix[q, k] = rotatedQ;
        }

        // Accumulate the same orthogonal rotations so the final columns are the
        // eigenvectors of the original matrix rather than of the diagonalized copy.
        for (var row = 0; row < eigenvectors.Rows; row++)
        {
            var vip = eigenvectors[row, p];
            var viq = eigenvectors[row, q];
            eigenvectors[row, p] = (cosine * vip) - (sine * viq);
            eigenvectors[row, q] = (sine * vip) + (cosine * viq);
        }
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

    private static double[] CreateDeflatedInitialVector(IReadOnlyList<double> removedEigenvector)
    {
        var denominator = Dot(removedEigenvector, removedEigenvector);
        if (denominator <= NumericConstants.NearlyZero)
        {
            throw new ArgumentException("Removed eigenvector must not be the zero vector.", nameof(removedEigenvector));
        }

        // Start with a broad deterministic vector so all remaining eigendirections
        // normally have a non-zero component, then project out the direction that was
        // deliberately replaced by the zero eigenvalue.
        var vector = Enumerable.Repeat(1.0, removedEigenvector.Count).ToArray();
        RemoveProjection(vector, removedEigenvector, denominator);
        if (EuclideanNorm(vector) > NumericConstants.NearlyZero)
        {
            return vector;
        }

        // If the removed eigenvector is itself proportional to the all-ones vector,
        // project individual basis vectors until a non-parallel direction is found.
        for (var candidate = 0; candidate < removedEigenvector.Count; candidate++)
        {
            Array.Clear(vector);
            vector[candidate] = 1.0;
            RemoveProjection(vector, removedEigenvector, denominator);
            if (EuclideanNorm(vector) > NumericConstants.NearlyZero)
            {
                return vector;
            }
        }

        throw new ArithmeticException("Unable to construct an initial vector for the deflated power iteration.");
    }

    private static void RemoveProjection(double[] vector, IReadOnlyList<double> direction, double directionNormSquared)
    {
        var coefficient = Dot(vector, direction) / directionNormSquared;
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] -= coefficient * direction[i];
        }
    }

    private static void EnsureSquare(DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Rows != matrix.Columns)
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
                if (!double.IsFinite(left) || !double.IsFinite(right) || System.Math.Abs(left - right) > tolerance * scale)
                {
                    throw new ArgumentException("Cyclic Jacobi requires a real symmetric matrix.", nameof(matrix));
                }
            }

            if (!double.IsFinite(matrix[row, row]))
            {
                throw new ArgumentException("Matrix entries must be finite.", nameof(matrix));
            }
        }
    }

    private static void Normalize(double[] vector)
    {
        var norm = EuclideanNorm(vector);
        if (!double.IsFinite(norm) || norm <= NumericConstants.NearlyZero)
        {
            throw new ArgumentException("Vector must contain finite values and must not be the zero vector.", nameof(vector));
        }
        for (var i = 0; i < vector.Length; i++) vector[i] /= norm;
    }

    private static void AlignSign(double[] vector, IReadOnlyList<double> reference)
    {
        if (Dot(vector, reference) < 0.0)
        {
            for (var i = 0; i < vector.Length; i++) vector[i] = -vector[i];
        }
    }

    private static double Dot(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Count; i++) sum += a[i] * b[i];
        return sum;
    }

    private static double EuclideanNorm(IReadOnlyList<double> vector) => System.Math.Sqrt(Dot(vector, vector));

    private static double FrobeniusNorm(DenseMatrix matrix)
    {
        var sum = 0.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                sum += matrix[row, column] * matrix[row, column];
            }
        }

        return System.Math.Sqrt(sum);
    }

    private static double OffDiagonalFrobeniusNorm(DenseMatrix matrix)
    {
        var sum = 0.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = row + 1; column < matrix.Columns; column++)
            {
                var value = matrix[row, column];
                sum += 2.0 * value * value;
            }
        }

        return System.Math.Sqrt(sum);
    }

    private static double ResidualNorm(IReadOnlyList<double> ax, IReadOnlyList<double> x, double lambda)
    {
        var sum = 0.0;
        for (var i = 0; i < ax.Count; i++)
        {
            var r = ax[i] - (lambda * x[i]);
            sum += r * r;
        }
        return System.Math.Sqrt(sum);
    }
}
