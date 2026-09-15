namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Immutable public view of the complete eigensystem of a real symmetric matrix.
/// </summary>
/// <remarks>
/// Eigenvectors are stored as columns and correspond to the eigenvalues at the same index.
/// The decomposition is returned in descending numerical eigenvalue order. Public arrays and
/// matrices are defensive copies so callers cannot accidentally mutate a completed result.
/// </remarks>
public sealed class SymmetricEigendecomposition
{
    private readonly double[] _eigenvalues;
    private readonly DenseMatrix _eigenvectors;

    internal SymmetricEigendecomposition(double[] eigenvalues, DenseMatrix eigenvectors)
    {
        ArgumentNullException.ThrowIfNull(eigenvalues);
        ArgumentNullException.ThrowIfNull(eigenvectors);

        if (!eigenvectors.IsSquare || eigenvectors.Columns != eigenvalues.Length)
        {
            throw new ArgumentException("Eigenvalue and eigenvector dimensions must describe a square eigensystem.");
        }

        for (var i = 0; i < eigenvalues.Length; i++)
        {
            if (!double.IsFinite(eigenvalues[i]))
            {
                throw new ArgumentOutOfRangeException(nameof(eigenvalues), "Eigenvalues must be finite.");
            }
        }

        _eigenvalues = (double[])eigenvalues.Clone();
        _eigenvectors = eigenvectors.Clone();
    }

    /// <summary>
    /// Gets the number of eigenpairs in the decomposition.
    /// </summary>
    public int Size => _eigenvalues.Length;

    /// <summary>
    /// Gets the eigenvalues in descending numerical order as a defensive copy.
    /// </summary>
    public double[] Eigenvalues => (double[])_eigenvalues.Clone();

    /// <summary>
    /// Gets a defensive copy of the orthonormal eigenvector matrix.
    /// </summary>
    /// <remarks>Column <c>i</c> belongs to eigenvalue <c>Eigenvalues[i]</c>.</remarks>
    public DenseMatrix Eigenvectors => _eigenvectors.Clone();

    /// <summary>
    /// Gets one eigenvalue without allocating an array copy.
    /// </summary>
    public double GetEigenvalue(int index)
    {
        ValidateIndex(index);
        return _eigenvalues[index];
    }

    /// <summary>
    /// Gets one eigenvector as an independent array copy.
    /// </summary>
    public double[] GetEigenvector(int index)
    {
        ValidateIndex(index);

        var vector = new double[Size];
        for (var row = 0; row < Size; row++)
        {
            vector[row] = _eigenvectors[row, index];
        }

        return vector;
    }

    /// <summary>
    /// Returns one immutable eigenpair from the decomposition.
    /// </summary>
    public Eigenpair GetEigenpair(int index)
    {
        ValidateIndex(index);
        return new Eigenpair(_eigenvalues[index], GetEigenvector(index));
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
