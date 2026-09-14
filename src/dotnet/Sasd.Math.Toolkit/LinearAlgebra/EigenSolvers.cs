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
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Rows != matrix.Columns) throw new ArgumentException("Matrix must be square.", nameof(matrix));
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
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Rows != matrix.Columns) throw new ArgumentException("Matrix must be square.", nameof(matrix));
        NumericGuard.Positive(tolerance, nameof(tolerance));
        NumericGuard.Positive(maximumIterations, nameof(maximumIterations));
        var n = matrix.Rows;
        var x = initialVector?.ToArray() ?? Enumerable.Repeat(1.0, n).ToArray();
        if (x.Length != n) throw new ArgumentException("Initial vector length must match matrix size.", nameof(initialVector));
        Normalize(x);
        var eigenvalue = 0.0;

        // Inverse iteration solves A*y=x repeatedly with the same A. Keeping one LU
        // factorization makes that repeated-use relationship explicit and exercises the
        // same reusable factorization abstraction exposed to library callers.
        var factorization = LinearSystemSolvers.FactorizeLu(matrix);

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

    private static void Normalize(double[] vector)
    {
        var norm = EuclideanNorm(vector);
        if (norm <= NumericConstants.NearlyZero) throw new ArgumentException("Vector must not be the zero vector.", nameof(vector));
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
