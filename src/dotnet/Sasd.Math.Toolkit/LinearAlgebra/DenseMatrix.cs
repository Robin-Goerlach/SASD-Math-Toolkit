namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Small, dependency-free dense matrix type used by the classical numerical algorithms.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DenseMatrix"/> is deliberately a readable reference data structure, not a
/// replacement for a BLAS/LAPACK-backed matrix package. Storage is row-major and matrix
/// dimensions are immutable after construction; individual entries remain mutable so the
/// textbook algorithms can build and transform working matrices without hidden allocations.
/// </para>
/// <para>
/// Matrix entries are required to be finite. This invariant catches invalid numerical input
/// close to its source and keeps later algorithms from silently propagating NaN or infinity.
/// </para>
/// </remarks>
public sealed class DenseMatrix
{
    private readonly double[] _data;

    /// <summary>
    /// Creates a zero-filled matrix with the requested dimensions.
    /// </summary>
    public DenseMatrix(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Row count must be greater than zero.");
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Column count must be greater than zero.");
        }

        var elementCount = (long)rows * columns;
        if (elementCount > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Matrix dimensions are too large for the reference implementation.");
        }

        Rows = rows;
        Columns = columns;
        _data = new double[(int)elementCount];
    }

    /// <summary>
    /// Creates a matrix by copying a rectangular two-dimensional array.
    /// </summary>
    /// <param name="values">Finite matrix values.</param>
    public DenseMatrix(double[,] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        Rows = values.GetLength(0);
        Columns = values.GetLength(1);
        if (Rows == 0 || Columns == 0)
        {
            throw new ArgumentException("Matrix must not be empty.", nameof(values));
        }

        _data = new double[values.Length];
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var value = values[row, column];
                EnsureFiniteValue(value, nameof(values));
                _data[(row * Columns) + column] = value;
            }
        }
    }

    /// <summary>
    /// Gets the number of matrix rows.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Gets the number of matrix columns.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets whether the matrix has the same number of rows and columns.
    /// </summary>
    public bool IsSquare => Rows == Columns;

    /// <summary>
    /// Gets or sets one finite matrix entry.
    /// </summary>
    public double this[int row, int column]
    {
        get
        {
            ValidateIndices(row, column);
            return _data[(row * Columns) + column];
        }
        set
        {
            ValidateIndices(row, column);
            EnsureFiniteValue(value, "value");
            _data[(row * Columns) + column] = value;
        }
    }

    /// <summary>
    /// Creates an independent copy of the matrix.
    /// </summary>
    public DenseMatrix Clone()
    {
        var clone = new DenseMatrix(Rows, Columns);
        Array.Copy(_data, clone._data, _data.Length);
        return clone;
    }

    /// <summary>
    /// Multiplies this matrix by a vector using the straightforward reference algorithm.
    /// </summary>
    public double[] Multiply(IReadOnlyList<double> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Count != Columns)
        {
            throw new ArgumentException("Vector length must match matrix column count.", nameof(vector));
        }

        for (var i = 0; i < vector.Count; i++)
        {
            EnsureFiniteValue(vector[i], nameof(vector));
        }

        var result = new double[Rows];
        for (var row = 0; row < Rows; row++)
        {
            var sum = 0.0;
            for (var column = 0; column < Columns; column++)
            {
                var product = this[row, column] * vector[column];
                EnsureFiniteComputation(product, "Matrix-vector multiplication produced a non-finite product.");

                sum += product;
                EnsureFiniteComputation(sum, "Matrix-vector multiplication overflowed while accumulating a row.");
            }

            result[row] = sum;
        }

        return result;
    }

    /// <summary>
    /// Multiplies this matrix by another dense matrix using the straightforward reference algorithm.
    /// </summary>
    /// <remarks>
    /// Clarity is preferred over cache blocking or SIMD-specific code at this stage. A future
    /// high-performance backend can optimize this operation without changing the numerical APIs
    /// built on top of <see cref="DenseMatrix"/>.
    /// </remarks>
    public DenseMatrix Multiply(DenseMatrix other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Columns != other.Rows)
        {
            throw new ArgumentException("Left matrix column count must match right matrix row count.", nameof(other));
        }

        var result = new DenseMatrix(Rows, other.Columns);
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < other.Columns; column++)
            {
                var sum = 0.0;
                for (var inner = 0; inner < Columns; inner++)
                {
                    var product = this[row, inner] * other[inner, column];
                    EnsureFiniteComputation(product, "Matrix multiplication produced a non-finite product.");

                    sum += product;
                    EnsureFiniteComputation(sum, "Matrix multiplication overflowed while accumulating an entry.");
                }

                result[row, column] = sum;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates an identity matrix of the requested order.
    /// </summary>
    public static DenseMatrix Identity(int size)
    {
        var result = new DenseMatrix(size, size);
        for (var i = 0; i < size; i++)
        {
            result[i, i] = 1.0;
        }

        return result;
    }

    internal void SwapRows(int first, int second)
    {
        if (first == second)
        {
            return;
        }

        for (var column = 0; column < Columns; column++)
        {
            (this[first, column], this[second, column]) = (this[second, column], this[first, column]);
        }
    }

    private static void EnsureFiniteValue(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Matrix and vector values must be finite.");
        }
    }

    private static void EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }

    private void ValidateIndices(int row, int column)
    {
        if ((uint)row >= (uint)Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if ((uint)column >= (uint)Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
    }
}
