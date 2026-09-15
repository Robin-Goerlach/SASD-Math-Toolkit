namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Immutable real sparse matrix stored in canonical compressed-sparse-row (CSR) form.
/// </summary>
/// <remarks>
/// <para>
/// CSR stores one row-pointer array, one column-index array and one value array. Within every row,
/// column indices are strictly increasing and explicit zero values are not stored. This canonical
/// representation makes row traversal and matrix-vector multiplication predictable while keeping
/// memory proportional to the number of stored entries rather than <c>Rows * Columns</c>.
/// </para>
/// <para>
/// Construction is deliberately stricter than a raw three-array wrapper. Public factories copy
/// caller data, validate finite values and dimensions, and either canonicalize coordinate entries
/// or verify that supplied CSR arrays are already canonical. The matrix is immutable after
/// construction so iterative solvers can safely reuse it without defensive copying.
/// </para>
/// </remarks>
public sealed class CsrMatrix
{
    private readonly int[] _rowPointers;
    private readonly int[] _columnIndices;
    private readonly double[] _values;

    private CsrMatrix(
        int rows,
        int columns,
        int[] rowPointers,
        int[] columnIndices,
        double[] values)
    {
        Rows = rows;
        Columns = columns;
        _rowPointers = rowPointers;
        _columnIndices = columnIndices;
        _values = values;
    }

    /// <summary>Gets the number of rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns.</summary>
    public int Columns { get; }

    /// <summary>Gets whether this matrix is square.</summary>
    public bool IsSquare => Rows == Columns;

    /// <summary>Gets the number of explicitly stored non-zero entries.</summary>
    public int NonZeroCount => _values.Length;

    /// <summary>
    /// Gets the fraction of matrix positions that are explicitly stored.
    /// </summary>
    public double Density => NonZeroCount / ((double)Rows * Columns);

    /// <summary>
    /// Builds a canonical CSR matrix from coordinate entries.
    /// </summary>
    /// <param name="rows">Positive row count.</param>
    /// <param name="columns">Positive column count.</param>
    /// <param name="entries">Finite coordinate entries.</param>
    /// <remarks>
    /// <para>
    /// Entries may arrive in arbitrary row/column order. Duplicate coordinates are added in their
    /// enumeration order; if the accumulated value becomes exactly zero, that coordinate is
    /// removed. The final CSR layout is sorted by row and then by strictly increasing column index.
    /// </para>
    /// <para>
    /// Duplicate aggregation is useful when assembling a matrix from local contributions. An
    /// intermediate non-finite sum is reported as an <see cref="ArithmeticException"/> instead of
    /// storing an invalid sparse value.
    /// </para>
    /// </remarks>
    public static CsrMatrix FromEntries(
        int rows,
        int columns,
        IEnumerable<SparseMatrixEntry> entries)
    {
        ValidateShape(rows, columns);
        ArgumentNullException.ThrowIfNull(entries);

        // A dictionary is used only during construction. The immutable result is stored as compact
        // arrays, so lookup overhead is not paid by numerical hot paths such as sparse matvec.
        var accumulated = new Dictionary<long, double>();
        foreach (var entry in entries)
        {
            ValidateCoordinate(entry.Row, entry.Column, rows, columns);
            EnsureFinite(entry.Value, nameof(entries));

            if (entry.Value == 0.0)
            {
                continue;
            }

            var key = (((long)entry.Row) * columns) + entry.Column;
            if (!accumulated.TryGetValue(key, out var current))
            {
                accumulated.Add(key, entry.Value);
                continue;
            }

            var combined = current + entry.Value;
            if (!double.IsFinite(combined))
            {
                throw new ArithmeticException(
                    $"Duplicate sparse entries at ({entry.Row}, {entry.Column}) overflowed while being combined.");
            }

            if (combined == 0.0)
            {
                accumulated.Remove(key);
            }
            else
            {
                accumulated[key] = combined;
            }
        }

        var keys = accumulated.Keys.ToArray();
        Array.Sort(keys);

        var rowPointers = new int[rows + 1];
        var columnIndices = new int[keys.Length];
        var values = new double[keys.Length];

        for (var index = 0; index < keys.Length; index++)
        {
            var row = (int)(keys[index] / columns);
            var column = (int)(keys[index] % columns);
            rowPointers[row + 1]++;
            columnIndices[index] = column;
            values[index] = accumulated[keys[index]];
        }

        for (var row = 0; row < rows; row++)
        {
            rowPointers[row + 1] += rowPointers[row];
        }

        return new CsrMatrix(rows, columns, rowPointers, columnIndices, values);
    }

    /// <summary>
    /// Builds a canonical CSR matrix by copying a dense matrix and omitting entries at or below an
    /// explicit absolute zero threshold.
    /// </summary>
    /// <param name="matrix">Finite dense source matrix.</param>
    /// <param name="absoluteZeroTolerance">
    /// Non-negative absolute threshold. The default zero stores every mathematically non-zero
    /// source entry; a positive value intentionally sparsifies smaller entries.
    /// </param>
    public static CsrMatrix FromDense(DenseMatrix matrix, double absoluteZeroTolerance = 0.0)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ValidateAbsoluteZeroTolerance(absoluteZeroTolerance);

        var rowPointers = new int[matrix.Rows + 1];
        var columnIndices = new List<int>();
        var values = new List<double>();

        for (var row = 0; row < matrix.Rows; row++)
        {
            var source = matrix.GetRowSpan(row);
            for (var column = 0; column < source.Length; column++)
            {
                var value = source[column];
                if (System.Math.Abs(value) <= absoluteZeroTolerance)
                {
                    continue;
                }

                columnIndices.Add(column);
                values.Add(value);
            }

            rowPointers[row + 1] = values.Count;
        }

        return new CsrMatrix(
            matrix.Rows,
            matrix.Columns,
            rowPointers,
            columnIndices.ToArray(),
            values.ToArray());
    }

    /// <summary>
    /// Builds a matrix from already compressed row data after validating canonical CSR invariants.
    /// </summary>
    /// <remarks>
    /// This factory is intended for file formats, native-library adapters and other interoperability
    /// layers that already have CSR arrays. Row pointers must start at zero, end at the stored-value
    /// count and be non-decreasing. Column indices must be strictly increasing inside each row.
    /// Stored values must be finite and non-zero. All arrays are defensively copied.
    /// </remarks>
    public static CsrMatrix FromCompressedRows(
        int rows,
        int columns,
        IReadOnlyList<int> rowPointers,
        IReadOnlyList<int> columnIndices,
        IReadOnlyList<double> values)
    {
        ValidateShape(rows, columns);
        ArgumentNullException.ThrowIfNull(rowPointers);
        ArgumentNullException.ThrowIfNull(columnIndices);
        ArgumentNullException.ThrowIfNull(values);

        if (rowPointers.Count != rows + 1)
        {
            throw new ArgumentException("CSR row-pointer count must equal rows + 1.", nameof(rowPointers));
        }

        if (columnIndices.Count != values.Count)
        {
            throw new ArgumentException(
                "CSR column-index and value arrays must have the same length.",
                nameof(columnIndices));
        }

        var nonZeroCount = values.Count;
        if (rowPointers[0] != 0 || rowPointers[^1] != nonZeroCount)
        {
            throw new ArgumentException(
                "CSR row pointers must start at zero and end at the stored-value count.",
                nameof(rowPointers));
        }

        for (var row = 0; row < rows; row++)
        {
            var start = rowPointers[row];
            var end = rowPointers[row + 1];
            if (start < 0 || end < start || end > nonZeroCount)
            {
                throw new ArgumentException(
                    "CSR row pointers must be non-negative, non-decreasing and within the stored-value range.",
                    nameof(rowPointers));
            }

            var previousColumn = -1;
            for (var position = start; position < end; position++)
            {
                var column = columnIndices[position];
                if ((uint)column >= (uint)columns)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(columnIndices),
                        $"CSR column index {column} at position {position} is outside the matrix bounds.");
                }

                if (column <= previousColumn)
                {
                    throw new ArgumentException(
                        "CSR column indices must be strictly increasing within each row.",
                        nameof(columnIndices));
                }

                var value = values[position];
                EnsureFinite(value, nameof(values));
                if (value == 0.0)
                {
                    throw new ArgumentException(
                        "Canonical CSR storage must not contain explicit zero values.",
                        nameof(values));
                }

                previousColumn = column;
            }
        }

        var rowPointerCopy = new int[rowPointers.Count];
        var columnIndexCopy = new int[columnIndices.Count];
        var valueCopy = new double[values.Count];
        for (var i = 0; i < rowPointerCopy.Length; i++)
        {
            rowPointerCopy[i] = rowPointers[i];
        }

        for (var i = 0; i < valueCopy.Length; i++)
        {
            columnIndexCopy[i] = columnIndices[i];
            valueCopy[i] = values[i];
        }

        return new CsrMatrix(rows, columns, rowPointerCopy, columnIndexCopy, valueCopy);
    }

    /// <summary>Returns one matrix value, or zero when the coordinate is not explicitly stored.</summary>
    public double GetValue(int row, int column)
    {
        ValidateCoordinate(row, column, Rows, Columns);

        var low = _rowPointers[row];
        var high = _rowPointers[row + 1] - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var storedColumn = _columnIndices[middle];
            if (storedColumn == column)
            {
                return _values[middle];
            }

            if (storedColumn < column)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return 0.0;
    }

    /// <summary>Returns a defensive copy of all canonical entries in one row.</summary>
    public SparseMatrixEntry[] GetRowEntries(int row)
    {
        ValidateRow(row);

        var start = _rowPointers[row];
        var count = _rowPointers[row + 1] - start;
        if (count == 0)
        {
            return Array.Empty<SparseMatrixEntry>();
        }

        var result = new SparseMatrixEntry[count];
        for (var offset = 0; offset < count; offset++)
        {
            var position = start + offset;
            result[offset] = new SparseMatrixEntry(row, _columnIndices[position], _values[position]);
        }

        return result;
    }

    /// <summary>Returns a defensive copy of the CSR row-pointer array.</summary>
    public int[] CopyRowPointers() => (int[])_rowPointers.Clone();

    /// <summary>Returns a defensive copy of the CSR column-index array.</summary>
    public int[] CopyColumnIndices() => (int[])_columnIndices.Clone();

    /// <summary>Returns a defensive copy of the CSR value array.</summary>
    public double[] CopyValues() => (double[])_values.Clone();

    /// <summary>Converts this sparse matrix to an independent dense matrix.</summary>
    public DenseMatrix ToDense()
    {
        var result = new DenseMatrix(Rows, Columns);
        for (var row = 0; row < Rows; row++)
        {
            var destination = result.GetMutableRowSpan(row);
            for (var position = _rowPointers[row]; position < _rowPointers[row + 1]; position++)
            {
                destination[_columnIndices[position]] = _values[position];
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the transpose as another canonical CSR matrix.
    /// </summary>
    /// <remarks>
    /// The algorithm counts destination-row entries first and then scatters source entries in
    /// increasing source-row order. Consequently the transposed column indices are already sorted;
    /// no dense intermediate or per-row sort is required.
    /// </remarks>
    public CsrMatrix Transpose()
    {
        var transposedRowPointers = new int[Columns + 1];
        for (var position = 0; position < _columnIndices.Length; position++)
        {
            transposedRowPointers[_columnIndices[position] + 1]++;
        }

        for (var row = 0; row < Columns; row++)
        {
            transposedRowPointers[row + 1] += transposedRowPointers[row];
        }

        var nextPosition = (int[])transposedRowPointers.Clone();
        var transposedColumnIndices = new int[NonZeroCount];
        var transposedValues = new double[NonZeroCount];

        for (var row = 0; row < Rows; row++)
        {
            for (var position = _rowPointers[row]; position < _rowPointers[row + 1]; position++)
            {
                var destinationRow = _columnIndices[position];
                var destination = nextPosition[destinationRow]++;
                transposedColumnIndices[destination] = row;
                transposedValues[destination] = _values[position];
            }
        }

        return new CsrMatrix(
            Columns,
            Rows,
            transposedRowPointers,
            transposedColumnIndices,
            transposedValues);
    }

    /// <summary>Multiplies this matrix by a finite vector and allocates the result.</summary>
    public double[] Multiply(IReadOnlyList<double> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Count != Columns)
        {
            throw new ArgumentException("Vector length must match sparse matrix column count.", nameof(vector));
        }

        var source = vector as double[] ?? vector.ToArray();
        var result = new double[Rows];
        Multiply(source.AsSpan(), result.AsSpan());
        return result;
    }

    /// <summary>
    /// Multiplies this matrix by a finite vector into caller-provided storage.
    /// </summary>
    /// <remarks>
    /// This overload exists for iterative solvers that must avoid allocating one result vector per
    /// iteration. Overlapping input/output spans are supported by taking a temporary input snapshot
    /// before the destination is modified.
    /// </remarks>
    public void Multiply(ReadOnlySpan<double> vector, Span<double> destination)
    {
        if (vector.Length != Columns)
        {
            throw new ArgumentException("Vector length must match sparse matrix column count.", nameof(vector));
        }

        if (destination.Length != Rows)
        {
            throw new ArgumentException("Destination length must match sparse matrix row count.", nameof(destination));
        }

        for (var i = 0; i < vector.Length; i++)
        {
            if (!double.IsFinite(vector[i]))
            {
                throw new ArgumentOutOfRangeException(nameof(vector), "Vector values must be finite.");
            }
        }

        ReadOnlySpan<double> source = vector;
        ReadOnlySpan<double> destinationView = destination;
        if (vector.Overlaps(destinationView))
        {
            source = vector.ToArray();
        }

        for (var row = 0; row < Rows; row++)
        {
            var sum = 0.0;
            for (var position = _rowPointers[row]; position < _rowPointers[row + 1]; position++)
            {
                var product = _values[position] * source[_columnIndices[position]];
                EnsureFiniteComputation(product, "Sparse matrix-vector multiplication produced a non-finite product.");

                sum += product;
                EnsureFiniteComputation(sum, "Sparse matrix-vector multiplication overflowed while accumulating a row.");
            }

            destination[row] = sum;
        }
    }

    internal ReadOnlySpan<int> RowPointersSpan => _rowPointers;

    internal ReadOnlySpan<int> ColumnIndicesSpan => _columnIndices;

    internal ReadOnlySpan<double> ValuesSpan => _values;

    private static void ValidateShape(int rows, int columns)
    {
        if (rows <= 0 || rows == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rows),
                "Row count must be positive and leave room for the CSR terminal row pointer.");
        }

        if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Column count must be greater than zero.");
        }
    }

    private static void ValidateCoordinate(int row, int column, int rows, int columns)
    {
        if ((uint)row >= (uint)rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if ((uint)column >= (uint)columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }
    }

    private void ValidateRow(int row)
    {
        if ((uint)row >= (uint)Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
    }

    private static void ValidateAbsoluteZeroTolerance(double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Absolute zero tolerance must be finite and non-negative.");
        }
    }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Sparse matrix values must be finite.");
        }
    }

    private static void EnsureFiniteComputation(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
