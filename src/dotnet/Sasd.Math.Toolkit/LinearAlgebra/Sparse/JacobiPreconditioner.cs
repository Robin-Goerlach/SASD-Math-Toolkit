namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Diagonal (Jacobi) preconditioner for sparse linear systems.
/// </summary>
/// <remarks>
/// <para>
/// The preconditioner stores the reciprocal diagonal of a square sparse matrix and applies
/// <c>z_i = r_i / A_ii</c>. It is inexpensive to construct, requires O(n) storage and O(n) work per
/// application, and is therefore a useful baseline before more elaborate incomplete factorizations
/// are introduced.
/// </para>
/// <para>
/// A Jacobi preconditioner is SPD when it is built from a matrix with strictly positive diagonal
/// entries, which makes it suitable for Preconditioned Conjugate Gradient on SPD systems. For later
/// nonsymmetric solvers this type can also represent finite non-zero negative diagonal entries.
/// </para>
/// <para>
/// The optional absolute diagonal tolerance is a caller-controlled modelling decision. The default
/// zero rejects only exact missing/zero diagonal entries. A positive threshold can deliberately
/// reject very small pivots, but one universal epsilon is not appropriate for all problem scales.
/// </para>
/// </remarks>
public sealed class JacobiPreconditioner : ISparsePreconditioner
{
    private readonly double[] _inverseDiagonal;

    private JacobiPreconditioner(double[] inverseDiagonal)
    {
        _inverseDiagonal = inverseDiagonal;
    }

    /// <inheritdoc />
    public int Size => _inverseDiagonal.Length;

    /// <summary>
    /// Creates a Jacobi preconditioner from the diagonal of a square CSR matrix.
    /// </summary>
    /// <param name="matrix">Square sparse source matrix.</param>
    /// <param name="absoluteDiagonalTolerance">
    /// Non-negative absolute threshold below which a diagonal entry is rejected.
    /// </param>
    public static JacobiPreconditioner Create(
        CsrMatrix matrix,
        double absoluteDiagonalTolerance = 0.0)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Jacobi preconditioning requires a square matrix.", nameof(matrix));
        }

        if (!double.IsFinite(absoluteDiagonalTolerance) || absoluteDiagonalTolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absoluteDiagonalTolerance),
                "Absolute diagonal tolerance must be finite and non-negative.");
        }

        var inverseDiagonal = new double[matrix.Rows];
        var rowPointers = matrix.RowPointersSpan;
        var columnIndices = matrix.ColumnIndicesSpan;
        var values = matrix.ValuesSpan;

        for (var row = 0; row < matrix.Rows; row++)
        {
            var found = false;
            var diagonal = 0.0;

            for (var position = rowPointers[row]; position < rowPointers[row + 1]; position++)
            {
                var column = columnIndices[position];
                if (column == row)
                {
                    diagonal = values[position];
                    found = true;
                    break;
                }

                // Canonical CSR columns are sorted, so once we pass the diagonal column it cannot
                // occur later in the row.
                if (column > row)
                {
                    break;
                }
            }

            if (!found || System.Math.Abs(diagonal) <= absoluteDiagonalTolerance)
            {
                throw new ArgumentException(
                    $"Jacobi preconditioning requires a usable non-zero diagonal entry in row {row}.",
                    nameof(matrix));
            }

            var reciprocal = 1.0 / diagonal;
            if (!double.IsFinite(reciprocal))
            {
                throw new ArithmeticException(
                    $"The reciprocal diagonal for row {row} is outside the finite double range.");
            }

            inverseDiagonal[row] = reciprocal;
        }

        return new JacobiPreconditioner(inverseDiagonal);
    }

    /// <inheritdoc />
    /// <remarks>
    /// This implementation supports overlapping source and destination spans because every output
    /// component depends only on the corresponding input component.
    /// </remarks>
    public void Apply(ReadOnlySpan<double> source, Span<double> destination)
    {
        if (source.Length != Size)
        {
            throw new ArgumentException("Source length must match preconditioner size.", nameof(source));
        }

        if (destination.Length != Size)
        {
            throw new ArgumentException("Destination length must match preconditioner size.", nameof(destination));
        }

        for (var index = 0; index < Size; index++)
        {
            var value = source[index];
            if (!double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Preconditioner input values must be finite.");
            }

            var result = value * _inverseDiagonal[index];
            if (!double.IsFinite(result))
            {
                throw new ArithmeticException(
                    "Jacobi preconditioning produced a non-finite output component.");
            }

            destination[index] = result;
        }
    }

    /// <summary>Returns one stored reciprocal diagonal value.</summary>
    public double GetInverseDiagonalValue(int index)
    {
        if ((uint)index >= (uint)Size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _inverseDiagonal[index];
    }

    /// <summary>Returns a defensive copy of the stored reciprocal diagonal.</summary>
    public double[] CopyInverseDiagonal() => (double[])_inverseDiagonal.Clone();
}
