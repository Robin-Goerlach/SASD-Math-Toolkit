using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Represents a classical Wielandt rank-one deflation of a real matrix.
/// </summary>
/// <remarks>
/// Given a known right eigenpair <c>A*v = lambda*v</c>, Wielandt deflation constructs
/// <c>B = A - v*x^T</c>. The row vector <c>x^T</c> is chosen from a row of <c>A</c> so
/// that <c>x^T*v = lambda</c>. Consequently the removed eigenvalue is replaced by zero
/// while the remaining eigenvalues are retained when the supplied eigenpair is accurate.
/// </remarks>
public sealed class WielandtDeflation
{
    private readonly DenseMatrix _originalMatrix;
    private readonly DenseMatrix _deflatedMatrix;
    private readonly double[] _removedEigenvector;
    private readonly double[] _deflationRow;

    private WielandtDeflation(
        DenseMatrix originalMatrix,
        DenseMatrix deflatedMatrix,
        double removedEigenvalue,
        double[] removedEigenvector,
        double[] deflationRow,
        int pivotIndex,
        double sourceResidual)
    {
        _originalMatrix = originalMatrix;
        _deflatedMatrix = deflatedMatrix;
        RemovedEigenvalue = removedEigenvalue;
        _removedEigenvector = removedEigenvector;
        _deflationRow = deflationRow;
        PivotIndex = pivotIndex;
        SourceResidual = sourceResidual;
    }

    /// <summary>
    /// Gets the eigenvalue that the deflation replaces by zero.
    /// </summary>
    public double RemovedEigenvalue { get; }

    /// <summary>
    /// Gets the row index selected from the known eigenvector.
    /// </summary>
    public int PivotIndex { get; }

    /// <summary>
    /// Gets the Euclidean residual of the eigenpair that was accepted for deflation.
    /// </summary>
    public double SourceResidual { get; }

    /// <summary>
    /// Gets a defensive copy of the deflated matrix.
    /// </summary>
    public DenseMatrix DeflatedMatrix => _deflatedMatrix.Clone();

    /// <summary>
    /// Gets a defensive copy of the eigenpair removed by the deflation.
    /// </summary>
    public Eigenpair RemovedEigenpair => new(RemovedEigenvalue, _removedEigenvector);

    /// <summary>
    /// Creates a Wielandt deflation from a square matrix and an already known approximate eigenpair.
    /// </summary>
    /// <param name="matrix">Square original matrix.</param>
    /// <param name="eigenpair">Finite claimed eigenpair to remove.</param>
    /// <param name="residualTolerance">
    /// Relative acceptance threshold for <c>||A*v-lambda*v||2</c>, scaled by the largest matrix/eigenvalue magnitude.
    /// </param>
    /// <remarks>
    /// Deflation is meaningful only when the supplied pair actually approximates an eigenpair of
    /// the matrix. V1 therefore validates the equation defect instead of silently constructing a
    /// rank-one transform from unrelated data.
    /// </remarks>
    public static WielandtDeflation Create(
        DenseMatrix matrix,
        Eigenpair eigenpair,
        double residualTolerance = 1e-8)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(eigenpair);
        NumericGuard.Positive(residualTolerance, nameof(residualTolerance));

        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Matrix must be square.", nameof(matrix));
        }

        if (!double.IsFinite(eigenpair.Eigenvalue))
        {
            throw new ArgumentOutOfRangeException(nameof(eigenpair), "Eigenvalue must be finite.");
        }

        if (eigenpair.Dimension != matrix.Rows)
        {
            throw new ArgumentException("Eigenvector length must match matrix size.", nameof(eigenpair));
        }

        var vector = eigenpair.Eigenvector;
        Normalize(vector);

        var multiplied = matrix.Multiply(vector);
        var sourceResidual = ResidualNorm(multiplied, vector, eigenpair.Eigenvalue);
        var problemScale = System.Math.Max(
            1.0,
            System.Math.Max(MatrixMaximumAbsoluteEntry(matrix), System.Math.Abs(eigenpair.Eigenvalue)));
        var threshold = residualTolerance * problemScale;
        if (!double.IsFinite(threshold))
        {
            threshold = double.MaxValue;
        }

        if (sourceResidual > threshold)
        {
            throw new ArgumentException(
                "Supplied eigenpair residual is too large for a reliable Wielandt deflation.",
                nameof(eigenpair));
        }

        // Choosing the largest component is the usual stable form of the classical Wielandt
        // construction because the selected component becomes a divisor.
        var pivotIndex = 0;
        var pivotMagnitude = System.Math.Abs(vector[0]);
        for (var i = 1; i < vector.Length; i++)
        {
            var magnitude = System.Math.Abs(vector[i]);
            if (magnitude > pivotMagnitude)
            {
                pivotIndex = i;
                pivotMagnitude = magnitude;
            }
        }

        if (pivotMagnitude <= NumericConstants.NearlyZero)
        {
            throw new ArgumentException("Eigenvector must not be the zero vector.", nameof(eigenpair));
        }

        var selectedComponent = vector[pivotIndex];
        var deflationRow = new double[matrix.Columns];
        for (var column = 0; column < matrix.Columns; column++)
        {
            var value = matrix[pivotIndex, column] / selectedComponent;
            EnsureFinite(value, "Wielandt deflation row overflowed.");
            deflationRow[column] = value;
        }

        var deflated = new DenseMatrix(matrix.Rows, matrix.Columns);
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                var correction = vector[row] * deflationRow[column];
                EnsureFinite(correction, "Wielandt rank-one correction overflowed.");
                var value = matrix[row, column] - correction;
                EnsureFinite(value, "Wielandt deflation produced a non-finite matrix entry.");
                deflated[row, column] = value;
            }
        }

        return new WielandtDeflation(
            matrix.Clone(),
            deflated,
            eigenpair.Eigenvalue,
            vector,
            deflationRow,
            pivotIndex,
            sourceResidual);
    }

    /// <summary>
    /// Maps an eigenpair of the deflated matrix back to an eigenpair candidate of the original matrix.
    /// </summary>
    /// <remarks>
    /// For a deflated eigenpair <c>B*y = mu*y</c>, the original eigenvector is of the form
    /// <c>y + alpha*v</c>. The correction becomes ill-conditioned when <c>mu</c> is nearly equal
    /// to the removed eigenvalue; that case is reported explicitly.
    /// </remarks>
    public Eigenpair Restore(Eigenpair deflatedEigenpair)
    {
        ArgumentNullException.ThrowIfNull(deflatedEigenpair);
        if (!double.IsFinite(deflatedEigenpair.Eigenvalue))
        {
            throw new ArgumentOutOfRangeException(nameof(deflatedEigenpair), "Eigenvalue must be finite.");
        }

        if (deflatedEigenpair.Dimension != _originalMatrix.Rows)
        {
            throw new ArgumentException("Eigenvector length must match matrix size.", nameof(deflatedEigenpair));
        }

        var vector = deflatedEigenpair.Eigenvector;
        Normalize(vector);

        var denominator = deflatedEigenpair.Eigenvalue - RemovedEigenvalue;
        EnsureFinite(denominator, "Wielandt restoration denominator became non-finite.");
        var scale = System.Math.Max(
            1.0,
            System.Math.Max(System.Math.Abs(deflatedEigenpair.Eigenvalue), System.Math.Abs(RemovedEigenvalue)));

        if (System.Math.Abs(denominator) <= 100.0 * NumericConstants.NearlyZero * scale)
        {
            throw new ArithmeticException(
                "Wielandt eigenvector restoration is ill-conditioned for a repeated or nearly repeated eigenvalue.");
        }

        var alpha = DotFinite(_deflationRow, vector) / denominator;
        EnsureFinite(alpha, "Wielandt restoration coefficient became non-finite.");

        var restored = new double[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            var correction = alpha * _removedEigenvector[i];
            EnsureFinite(correction, "Wielandt restoration correction overflowed.");
            restored[i] = vector[i] + correction;
            EnsureFinite(restored[i], "Wielandt restoration produced a non-finite vector component.");
        }

        Normalize(restored);

        // Recompute the eigenvalue with the original matrix. The Rayleigh quotient removes part
        // of the small error introduced by the iterative estimates used during deflation.
        var multiplied = _originalMatrix.Multiply(restored);
        var eigenvalue = DotFinite(restored, multiplied);
        return new Eigenpair(eigenvalue, restored);
    }

    private static void Normalize(double[] vector)
    {
        var scale = 0.0;
        for (var i = 0; i < vector.Length; i++)
        {
            EnsureFinite(vector[i], "Eigenvector contains a non-finite value.");
            scale = System.Math.Max(scale, System.Math.Abs(vector[i]));
        }

        if (scale <= NumericConstants.NearlyZero)
        {
            throw new ArgumentException("Vector must not be zero or numerically negligible.", nameof(vector));
        }

        var scaledSum = 0.0;
        for (var i = 0; i < vector.Length; i++)
        {
            var scaled = vector[i] / scale;
            scaledSum += scaled * scaled;
        }

        var scaledNorm = System.Math.Sqrt(scaledSum);
        EnsureFinite(scaledNorm, "Eigenvector normalization failed.");
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (vector[i] / scale) / scaledNorm;
        }
    }

    private static double MatrixMaximumAbsoluteEntry(DenseMatrix matrix)
    {
        var maximum = 0.0;
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                maximum = System.Math.Max(maximum, System.Math.Abs(matrix[row, column]));
            }
        }

        return maximum;
    }

    private static double ResidualNorm(IReadOnlyList<double> ax, IReadOnlyList<double> vector, double eigenvalue)
    {
        var residual = new double[ax.Count];
        for (var i = 0; i < residual.Length; i++)
        {
            var scaled = eigenvalue * vector[i];
            EnsureFinite(scaled, "Eigenpair residual overflowed while scaling the vector.");
            residual[i] = ax[i] - scaled;
            EnsureFinite(residual[i], "Eigenpair residual became non-finite.");
        }

        return StableNorm(residual);
    }

    private static double DotFinite(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        if (left.Count != right.Count)
        {
            throw new ArgumentException("Vector dimensions must match.");
        }

        var sum = 0.0;
        for (var i = 0; i < left.Count; i++)
        {
            var product = left[i] * right[i];
            EnsureFinite(product, "Dot product overflowed.");
            sum += product;
            EnsureFinite(sum, "Dot product accumulation overflowed.");
        }

        return sum;
    }

    private static double StableNorm(IReadOnlyList<double> vector)
    {
        var scale = 0.0;
        var sumSquares = 1.0;
        for (var i = 0; i < vector.Count; i++)
        {
            var absolute = System.Math.Abs(vector[i]);
            EnsureFinite(absolute, "Norm calculation encountered a non-finite value.");
            if (absolute == 0.0)
            {
                continue;
            }

            if (scale < absolute)
            {
                var ratio = scale / absolute;
                sumSquares = 1.0 + (sumSquares * ratio * ratio);
                scale = absolute;
            }
            else
            {
                var ratio = absolute / scale;
                sumSquares += ratio * ratio;
            }
        }

        if (scale == 0.0)
        {
            return 0.0;
        }

        var norm = scale * System.Math.Sqrt(sumSquares);
        EnsureFinite(norm, "Norm exceeds the finite range of double precision.");
        return norm;
    }

    private static void EnsureFinite(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
