namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Small, dependency-free dense matrix type used by the classical numerical algorithms.
/// This is intentionally not a replacement for BLAS/LAPACK-backed matrix libraries.
/// </summary>
public sealed class DenseMatrix
{
    private readonly double[] _data;

    public DenseMatrix(int rows, int columns)
    {
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
        Rows = rows;
        Columns = columns;
        _data = new double[rows * columns];
    }

    public DenseMatrix(double[,] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Rows = values.GetLength(0);
        Columns = values.GetLength(1);
        if (Rows == 0 || Columns == 0) throw new ArgumentException("Matrix must not be empty.", nameof(values));
        _data = new double[Rows * Columns];
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                this[row, column] = values[row, column];
            }
        }
    }

    public int Rows { get; }
    public int Columns { get; }

    public double this[int row, int column]
    {
        get => _data[(row * Columns) + column];
        set => _data[(row * Columns) + column] = value;
    }

    public DenseMatrix Clone()
    {
        var clone = new DenseMatrix(Rows, Columns);
        Array.Copy(_data, clone._data, _data.Length);
        return clone;
    }

    public double[] Multiply(IReadOnlyList<double> vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.Count != Columns) throw new ArgumentException("Vector length must match matrix column count.", nameof(vector));
        var result = new double[Rows];
        for (var row = 0; row < Rows; row++)
        {
            var sum = 0.0;
            for (var column = 0; column < Columns; column++)
            {
                sum += this[row, column] * vector[column];
            }
            result[row] = sum;
        }
        return result;
    }

    public static DenseMatrix Identity(int size)
    {
        var result = new DenseMatrix(size, size);
        for (var i = 0; i < size; i++) result[i, i] = 1.0;
        return result;
    }

    internal void SwapRows(int first, int second)
    {
        if (first == second) return;
        for (var column = 0; column < Columns; column++)
        {
            (this[first, column], this[second, column]) = (this[second, column], this[first, column]);
        }
    }
}
