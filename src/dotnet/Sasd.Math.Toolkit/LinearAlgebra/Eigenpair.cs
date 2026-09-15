using System.Collections.ObjectModel;

namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Immutable value object describing one real eigenvalue and its associated real eigenvector.
/// </summary>
/// <remarks>
/// <para>
/// The public constructor requires a finite eigenvalue and finite vector components. The input
/// vector is copied so later caller mutations cannot change a completed numerical result.
/// </para>
/// <para>
/// Eigenvectors are defined only up to a non-zero scalar factor. The toolkit's eigensolvers
/// return normalized vectors, but the public type deliberately does not force normalization so
/// callers can also use it to describe independently obtained reference eigenpairs.
/// </para>
/// </remarks>
public sealed class Eigenpair
{
    private readonly double[] _eigenvector;
    private readonly ReadOnlyCollection<double> _components;

    /// <summary>
    /// Creates an immutable eigenpair from a finite eigenvalue and finite vector components.
    /// </summary>
    public Eigenpair(double eigenvalue, IReadOnlyList<double> eigenvector)
        : this(eigenvalue, eigenvector, validate: true)
    {
    }

    private Eigenpair(double eigenvalue, IReadOnlyList<double> eigenvector, bool validate)
    {
        ArgumentNullException.ThrowIfNull(eigenvector);
        if (eigenvector.Count == 0)
        {
            throw new ArgumentException("Eigenvector must contain at least one component.", nameof(eigenvector));
        }

        _eigenvector = new double[eigenvector.Count];
        for (var i = 0; i < eigenvector.Count; i++)
        {
            var component = eigenvector[i];
            if (validate && !double.IsFinite(component))
            {
                throw new ArgumentOutOfRangeException(nameof(eigenvector), "Eigenvector components must be finite.");
            }

            _eigenvector[i] = component;
        }

        if (validate && !double.IsFinite(eigenvalue))
        {
            throw new ArgumentOutOfRangeException(nameof(eigenvalue), "Eigenvalue must be finite.");
        }

        Eigenvalue = eigenvalue;
        _components = Array.AsReadOnly(_eigenvector);
    }

    /// <summary>
    /// Gets the eigenvalue.
    /// </summary>
    public double Eigenvalue { get; }

    /// <summary>
    /// Gets the number of components in the eigenvector.
    /// </summary>
    public int Dimension => _eigenvector.Length;

    /// <summary>
    /// Gets a read-only view of the internally owned eigenvector.
    /// </summary>
    public IReadOnlyList<double> Components => _components;

    /// <summary>
    /// Gets a defensive array copy of the eigenvector for compatibility with array-based callers.
    /// </summary>
    public double[] Eigenvector => (double[])_eigenvector.Clone();

    /// <summary>
    /// Gets one eigenvector component.
    /// </summary>
    public double this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_eigenvector.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _eigenvector[index];
        }
    }

    /// <summary>
    /// Creates the internal placeholder used when an iterative composite algorithm cannot
    /// produce the requested eigenpair. Callers should inspect the surrounding result status
    /// before reading its value.
    /// </summary>
    internal static Eigenpair Unavailable(int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension));
        }

        return new Eigenpair(double.NaN, new double[dimension], validate: false);
    }
}
