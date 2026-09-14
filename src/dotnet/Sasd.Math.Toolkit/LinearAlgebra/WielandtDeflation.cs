using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Represents a classical Wielandt rank-one deflation of a real matrix.
/// </summary>
/// <remarks>
/// Given a known right eigenpair <c>A*v = lambda*v</c>, Wielandt deflation
/// constructs <c>B = A - v*x^T</c>. The row vector <c>x^T</c> is chosen from a
/// row of <c>A</c> so that <c>x^T*v = lambda</c>. Consequently the removed
/// eigenvalue is replaced by zero while the remaining eigenvalues are retained.
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
        int pivotIndex)
    {
        _originalMatrix = originalMatrix;
        _deflatedMatrix = deflatedMatrix;
        RemovedEigenvalue = removedEigenvalue;
        _removedEigenvector = removedEigenvector;
        _deflationRow = deflationRow;
        PivotIndex = pivotIndex;
    }

    /// <summary>
    /// Gets the eigenvalue that the deflation replaces by zero.
    /// </summary>
    public double RemovedEigenvalue { get; }

    /// <summary>
    /// Gets the row index selected from the known eigenvector.
    /// </summary>
    /// <remarks>
    /// The index with the largest absolute eigenvector component is used to avoid
    /// an unnecessarily small divisor in the deflation formula.
    /// </remarks>
    public int PivotIndex { get; }

    /// <summary>
    /// Gets a copy of the deflated matrix.
    /// </summary>
    public DenseMatrix DeflatedMatrix => _deflatedMatrix.Clone();

    /// <summary>
    /// Gets a defensive copy of the eigenpair removed by the deflation.
    /// </summary>
    public Eigenpair RemovedEigenpair =>
        new(RemovedEigenvalue, (double[])_removedEigenvector.Clone());

    /// <summary>
    /// Creates a Wielandt deflation from a square matrix and a known eigenpair.
    /// </summary>
    public static WielandtDeflation Create(DenseMatrix matrix, Eigenpair eigenpair)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentNullException.ThrowIfNull(eigenpair);

        if (matrix.Rows != matrix.Columns)
        {
            throw new ArgumentException("Matrix must be square.", nameof(matrix));
        }

        if (!double.IsFinite(eigenpair.Eigenvalue))
        {
            throw new ArgumentOutOfRangeException(nameof(eigenpair), "Eigenvalue must be finite.");
        }

        if (eigenpair.Eigenvector.Length != matrix.Rows)
        {
            throw new ArgumentException("Eigenvector length must match matrix size.", nameof(eigenpair));
        }

        var vector = (double[])eigenpair.Eigenvector.Clone();
        Normalize(vector);

        // Choosing the largest component is the usual stable form of the classical
        // Wielandt construction because the selected component becomes a divisor.
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
            deflationRow[column] = matrix[pivotIndex, column] / selectedComponent;
        }

        var deflated = matrix.Clone();
        for (var row = 0; row < matrix.Rows; row++)
        {
            for (var column = 0; column < matrix.Columns; column++)
            {
                deflated[row, column] -= vector[row] * deflationRow[column];
            }
        }

        return new WielandtDeflation(
            matrix.Clone(),
            deflated,
            eigenpair.Eigenvalue,
            vector,
            deflationRow,
            pivotIndex);
    }

    /// <summary>
    /// Maps an eigenpair of the deflated matrix back to an eigenpair candidate of
    /// the original matrix.
    /// </summary>
    /// <remarks>
    /// For a deflated eigenpair <c>B*y = mu*y</c>, the original eigenvector is of
    /// the form <c>y + alpha*v</c>. The correction is undefined for a repeated
    /// eigenvalue where <c>mu</c> is numerically indistinguishable from the removed
    /// eigenvalue; that case is reported explicitly instead of hiding the division.
    /// </remarks>
    public Eigenpair Restore(Eigenpair deflatedEigenpair)
    {
        ArgumentNullException.ThrowIfNull(deflatedEigenpair);
        if (deflatedEigenpair.Eigenvector.Length != _originalMatrix.Rows)
        {
            throw new ArgumentException("Eigenvector length must match matrix size.", nameof(deflatedEigenpair));
        }

        var vector = (double[])deflatedEigenpair.Eigenvector.Clone();
        Normalize(vector);

        var denominator = deflatedEigenpair.Eigenvalue - RemovedEigenvalue;
        var scale = System.Math.Max(
            1.0,
            System.Math.Max(System.Math.Abs(deflatedEigenpair.Eigenvalue), System.Math.Abs(RemovedEigenvalue)));

        if (System.Math.Abs(denominator) <= 100.0 * NumericConstants.NearlyZero * scale)
        {
            throw new ArithmeticException(
                "Wielandt eigenvector restoration is ill-conditioned for a repeated or nearly repeated eigenvalue.");
        }

        var alpha = Dot(_deflationRow, vector) / denominator;
        var restored = new double[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            restored[i] = vector[i] + (alpha * _removedEigenvector[i]);
        }

        Normalize(restored);

        // Recompute the eigenvalue with the original matrix. The Rayleigh quotient
        // removes part of the small error introduced by iterative power estimates.
        var multiplied = _originalMatrix.Multiply(restored);
        var eigenvalue = Dot(restored, multiplied);
        return new Eigenpair(eigenvalue, restored);
    }

    private static void Normalize(double[] vector)
    {
        var normSquared = Dot(vector, vector);
        if (!double.IsFinite(normSquared) || normSquared <= NumericConstants.NearlyZero * NumericConstants.NearlyZero)
        {
            throw new ArgumentException("Vector must contain finite values and must not be the zero vector.", nameof(vector));
        }

        var norm = System.Math.Sqrt(normSquared);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }
    }

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var sum = 0.0;
        for (var i = 0; i < left.Count; i++)
        {
            sum += left[i] * right[i];
        }

        return sum;
    }
}
