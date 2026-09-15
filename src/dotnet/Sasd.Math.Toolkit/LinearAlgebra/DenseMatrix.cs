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
/// Matrix entries are required to be finite. Public element access validates that invariant.
/// Internal row-span helpers exist only so trusted algorithms in this assembly can avoid paying
/// repeated bounds and invariant checks inside O(n^3) loops after their inputs are validated.
/// </para>
/// </remarks>
public sealed class DenseMatrix
{
    private readonly double[] _data;

    /// <summary>Creates a zero-filled matrix with the requested dimensions.</summary>
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

    /// <summary>Creates a matrix by copying a rectangular two-dimensional array.</summary>
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
            var rowOffset = row * Columns;
            for (var column = 0; column < Columns; column++)
            {
                var value = values[row, column];
                EnsureFiniteValue(value, nameof(values));
                _data[rowOffset + column] = value;
            }
        }
    }

    /// <summary>Gets the number of matrix rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of matrix columns.</summary>
    public int Columns { get; }

    /// <summary>Gets whether the matrix has the same number of rows and columns.</summary>
    public bool IsSquare => Rows == Columns;

    /// <summary>Gets or sets one finite matrix entry.</summary>
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

    /// <summary>Creates an independent copy of the matrix.</summary>
    public DenseMatrix Clone()
    {
        var clone = new DenseMatrix(Rows, Columns);
        Array.Copy(_data, clone._data, _data.Length);
        return clone;
    }

    /// <summary>Multiplies this matrix by a vector.</summary>
    /// <remarks>
    /// The hot inner loop addresses the row-major backing array directly after one validation pass.
    /// This preserves the public safety checks while avoiding two-dimensional index validation for
    /// every multiply-add operation.
    /// </remarks>
    public double[] Multiply(IReadOnlyList<double> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Count != Columns)
        {
            throw new ArgumentException("Vector length must match matrix column count.", nameof(vector));
        }

        // Arrays are by far the common hot-path input. Other IReadOnlyList implementations are
        // copied once so the O(rows*columns) loop does not repeatedly dispatch through an interface.
        var vectorData = vector as double[] ?? vector.ToArray();
        for (var i = 0; i < vectorData.Length; i++)
        {
            EnsureFiniteValue(vectorData[i], nameof(vector));
        }

        var result = new double[Rows];
        for (var row = 0; row < Rows; row++)
        {
            var rowOffset = row * Columns;
            var sum = 0.0;
            for (var column = 0; column < Columns; column++)
            {
                var product = _data[rowOffset + column] * vectorData[column];
                EnsureFiniteComputation(product, "Matrix-vector multiplication produced a non-finite product.");

                sum += product;
                EnsureFiniteComputation(sum, "Matrix-vector multiplication overflowed while accumulating a row.");
            }

            result[row] = sum;
        }

        return result;
    }

    /// <summary>Multiplies this matrix by another dense matrix.</summary>
    /// <remarks>
    /// The loop order is row-inner-column rather than row-column-inner. Because storage is row-major,
    /// one row of the right matrix and one row of the result are traversed contiguously for each
    /// inner dimension. This gives materially better cache locality while preserving the same
    /// accumulation order for each result element. No blocking, SIMD or unsafe code is required.
    /// </remarks>
    public DenseMatrix Multiply(DenseMatrix other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Columns != other.Rows)
        {
            throw new ArgumentException("Left matrix column count must match right matrix row count.", nameof(other));
        }

        var result = new DenseMatrix(Rows, other.Columns);
        var resultColumns = other.Columns;

        for (var row = 0; row < Rows; row++)
        {
            var leftOffset = row * Columns;
            var resultOffset = row * resultColumns;

            for (var inner = 0; inner < Columns; inner++)
            {
                var leftValue = _data[leftOffset + inner];
                var rightOffset = inner * resultColumns;

                for (var column = 0; column < resultColumns; column++)
                {
                    var product = leftValue * other._data[rightOffset + column];
                    EnsureFiniteComputation(product, "Matrix multiplication produced a non-finite product.");

                    var resultIndex = resultOffset + column;
                    var updated = result._data[resultIndex] + product;
                    EnsureFiniteComputation(updated, "Matrix multiplication overflowed while accumulating an entry.");
                    result._data[resultIndex] = updated;
                }
            }
        }

        return result;
    }

    /// <summary>Creates an identity matrix of the requested order.</summary>
    public static DenseMatrix Identity(int size)
    {
        var result = new DenseMatrix(size, size);
        for (var i = 0; i < size; i++)
        {
            result._data[(i * size) + i] = 1.0;
        }

        return result;
    }

    /// <summary>
    /// Returns a read-only span over one validated row for trusted algorithms in this assembly.
    /// </summary>
    internal ReadOnlySpan<double> GetRowSpan(int row)
    {
        ValidateRowIndex(row);
        return _data.AsSpan(row * Columns, Columns);
    }

    /// <summary>
    /// Returns a mutable span over one validated row for trusted algorithms in this assembly.
    /// Callers must preserve the finite-entry invariant themselves.
    /// </summary>
    internal Span<double> GetMutableRowSpan(int row)
    {
        ValidateRowIndex(row);
        return _data.AsSpan(row * Columns, Columns);
    }

    internal void SwapRows(int first, int second)
    {
        ValidateRowIndex(first);
        ValidateRowIndex(second);
        if (first == second)
        {
            return;
        }

        var firstOffset = first * Columns;
        var secondOffset = second * Columns;
        for (var column = 0; column < Columns; column++)
        {
            (_data[firstOffset + column], _data[secondOffset + column]) =
                (_data[secondOffset + column], _data[firstOffset + column]);
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
        ValidateRowIndex(row);
        if ((uint)column >= (uint)Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
    }

    private void ValidateRowIndex(int row)
    {
        if ((uint)row >= (uint)Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
    }
}
