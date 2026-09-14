namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Immutable public view of the complete eigensystem of a real symmetric matrix.
/// </summary>
/// <remarks>
/// Eigenvectors are stored as columns and correspond to the eigenvalues at the
/// same index. The decomposition is returned in descending eigenvalue order.
/// Copies are exposed deliberately so callers cannot accidentally mutate a
/// completed numerical result.
/// </remarks>
public sealed class SymmetricEigendecomposition
{
    private readonly double[] _eigenvalues;
    private readonly DenseMatrix _eigenvectors;

    internal SymmetricEigendecomposition(double[] eigenvalues, DenseMatrix eigenvectors)
    {
        ArgumentNullException.ThrowIfNull(eigenvalues);
        ArgumentNullException.ThrowIfNull(eigenvectors);

        if (eigenvectors.Rows != eigenvectors.Columns || eigenvectors.Columns != eigenvalues.Length)
        {
            throw new ArgumentException("Eigenvalue and eigenvector dimensions must describe a square eigensystem.");
        }

        _eigenvalues = (double[])eigenvalues.Clone();
        _eigenvectors = eigenvectors.Clone();
    }

    /// <summary>
    /// Gets the number of eigenpairs in the decomposition.
    /// </summary>
    public int Size => _eigenvalues.Length;

    /// <summary>
    /// Gets the eigenvalues in descending order.
    /// </summary>
    public double[] Eigenvalues => (double[])_eigenvalues.Clone();

    /// <summary>
    /// Gets a copy of the orthonormal eigenvector matrix.
    /// </summary>
    /// <remarks>Column <c>i</c> belongs to eigenvalue <c>Eigenvalues[i]</c>.</remarks>
    public DenseMatrix Eigenvectors => _eigenvectors.Clone();

    /// <summary>
    /// Returns one eigenpair from the decomposition.
    /// </summary>
    public Eigenpair GetEigenpair(int index)
    {
        if ((uint)index >= (uint)Size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var vector = new double[Size];
        for (var row = 0; row < Size; row++)
        {
            vector[row] = _eigenvectors[row, index];
        }

        return new Eigenpair(_eigenvalues[index], vector);
    }
}
