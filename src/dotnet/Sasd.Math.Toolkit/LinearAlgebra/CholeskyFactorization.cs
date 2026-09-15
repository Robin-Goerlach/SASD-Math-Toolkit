namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Reusable Cholesky factorization for real symmetric positive-definite dense matrices.
/// </summary>
/// <remarks>
/// Represents <c>A = L*L^T</c>. The implementation keeps a compact lower-triangular factor,
/// validates symmetry and numerical positive-definiteness with explicit relative tolerances,
/// and reuses the same factor for vector and multi-right-hand-side solves.
/// </remarks>
public sealed class CholeskyFactorization
{
    public const double DefaultRelativeSymmetryTolerance = 1e-12;
    public const double DefaultRelativePositiveDefiniteTolerance = 1e-14;

    private readonly double[] _lower;

    private CholeskyFactorization(
        int size,
        double[] lower,
        double relativeSymmetryTolerance,
        double relativePositiveDefiniteTolerance)
    {
        Size = size;
        _lower = lower;
        RelativeSymmetryTolerance = relativeSymmetryTolerance;
        RelativePositiveDefiniteTolerance = relativePositiveDefiniteTolerance;
    }

    public int Size { get; }
    public double RelativeSymmetryTolerance { get; }
    public double RelativePositiveDefiniteTolerance { get; }

    /// <summary>Returns a defensive copy of the lower-triangular factor.</summary>
    public DenseMatrix LowerTriangularFactor
    {
        get
        {
            var result = new DenseMatrix(Size, Size);
            for (var row = 0; row < Size; row++)
            {
                _lower.AsSpan(row * Size, row + 1).CopyTo(result.GetMutableRowSpan(row));
            }

            return result;
        }
    }

    public static CholeskyFactorization Decompose(
        DenseMatrix matrix,
        double relativeSymmetryTolerance = DefaultRelativeSymmetryTolerance,
        double relativePositiveDefiniteTolerance = DefaultRelativePositiveDefiniteTolerance)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ValidateRelativeTolerance(relativeSymmetryTolerance, nameof(relativeSymmetryTolerance));
        ValidateRelativeTolerance(relativePositiveDefiniteTolerance, nameof(relativePositiveDefiniteTolerance));

        if (!matrix.IsSquare)
        {
            throw new ArgumentException("Cholesky factorization requires a square matrix.", nameof(matrix));
        }

        ValidateSymmetry(matrix, relativeSymmetryTolerance);

        var size = matrix.Rows;
        var diagonalScale = 0.0;
        for (var i = 0; i < size; i++)
        {
            diagonalScale = System.Math.Max(diagonalScale, System.Math.Abs(matrix.GetRowSpan(i)[i]));
        }

        if (diagonalScale == 0.0)
        {
            throw new ArithmeticException("A zero-diagonal matrix cannot be positive definite.");
        }

        var pivotThreshold = diagonalScale * relativePositiveDefiniteTolerance;
        EnsureFinite(pivotThreshold, "Cholesky pivot threshold became non-finite.");

        var lower = new double[checked(size * size)];
        for (var row = 0; row < size; row++)
        {
            var matrixRow = matrix.GetRowSpan(row);
            var rowOffset = row * size;

            for (var column = 0; column <= row; column++)
            {
                var sum = matrixRow[column];
                var columnOffset = column * size;
                for (var inner = 0; inner < column; inner++)
                {
                    var product = lower[rowOffset + inner] * lower[columnOffset + inner];
                    EnsureFinite(product, "Cholesky factorization produced a non-finite inner product term.");
                    sum -= product;
                    EnsureFinite(sum, "Cholesky factorization overflowed while updating a factor entry.");
                }

                if (row == column)
                {
                    if (sum <= pivotThreshold)
                    {
                        throw new ArithmeticException(
                            "Matrix is not numerically positive definite at the configured relative threshold.");
                    }

                    lower[rowOffset + column] = System.Math.Sqrt(sum);
                    EnsureFinite(lower[rowOffset + column], "Cholesky factorization produced a non-finite diagonal entry.");
                }
                else
                {
                    lower[rowOffset + column] = sum / lower[columnOffset + column];
                    EnsureFinite(lower[rowOffset + column], "Cholesky factorization produced a non-finite factor entry.");
                }
            }
        }

        return new CholeskyFactorization(
            size,
            lower,
            relativeSymmetryTolerance,
            relativePositiveDefiniteTolerance);
    }

    /// <summary>Solves <c>A*x=b</c> without modifying caller-owned input.</summary>
    public double[] Solve(IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (rightHandSide.Count != Size)
        {
            throw new ArgumentException("Right-hand side length must match matrix size.", nameof(rightHandSide));
        }

        var work = rightHandSide.ToArray();
        ValidateFiniteVector(work, nameof(rightHandSide));

        for (var row = 0; row < Size; row++)
        {
            var sum = work[row];
            var rowOffset = row * Size;
            for (var column = 0; column < row; column++)
            {
                sum -= _lower[rowOffset + column] * work[column];
                EnsureFinite(sum, "Cholesky forward substitution became non-finite.");
            }

            work[row] = sum / _lower[rowOffset + row];
            EnsureFinite(work[row], "Cholesky forward substitution produced a non-finite value.");
        }

        for (var row = Size - 1; row >= 0; row--)
        {
            var sum = work[row];
            for (var column = row + 1; column < Size; column++)
            {
                sum -= _lower[(column * Size) + row] * work[column];
                EnsureFinite(sum, "Cholesky backward substitution became non-finite.");
            }

            work[row] = sum / _lower[(row * Size) + row];
            EnsureFinite(work[row], "Cholesky backward substitution produced a non-finite value.");
        }

        return work;
    }

    /// <summary>Solves <c>A*X=B</c> with right-hand sides stored as matrix columns.</summary>
    public DenseMatrix Solve(DenseMatrix rightHandSides)
    {
        ArgumentNullException.ThrowIfNull(rightHandSides);
        if (rightHandSides.Rows != Size)
        {
            throw new ArgumentException("Right-hand-side matrix row count must match matrix size.", nameof(rightHandSides));
        }

        var result = rightHandSides.Clone();

        for (var row = 0; row < Size; row++)
        {
            var destination = result.GetMutableRowSpan(row);
            var rowOffset = row * Size;
            var diagonal = _lower[rowOffset + row];
            for (var rhs = 0; rhs < result.Columns; rhs++)
            {
                var sum = destination[rhs];
                for (var inner = 0; inner < row; inner++)
                {
                    sum -= _lower[rowOffset + inner] * result.GetRowSpan(inner)[rhs];
                    EnsureFinite(sum, "Cholesky multi-RHS forward substitution became non-finite.");
                }

                destination[rhs] = sum / diagonal;
                EnsureFinite(destination[rhs], "Cholesky multi-RHS forward substitution produced a non-finite value.");
            }
        }

        for (var row = Size - 1; row >= 0; row--)
        {
            var destination = result.GetMutableRowSpan(row);
            var diagonal = _lower[(row * Size) + row];
            for (var rhs = 0; rhs < result.Columns; rhs++)
            {
                var sum = destination[rhs];
                for (var inner = row + 1; inner < Size; inner++)
                {
                    sum -= _lower[(inner * Size) + row] * result.GetRowSpan(inner)[rhs];
                    EnsureFinite(sum, "Cholesky multi-RHS backward substitution became non-finite.");
                }

                destination[rhs] = sum / diagonal;
                EnsureFinite(destination[rhs], "Cholesky multi-RHS backward substitution produced a non-finite value.");
            }
        }

        return result;
    }

    public double LogDeterminant()
    {
        var result = 0.0;
        for (var i = 0; i < Size; i++)
        {
            result += 2.0 * System.Math.Log(_lower[(i * Size) + i]);
            EnsureFinite(result, "Cholesky log-determinant became non-finite.");
        }

        return result;
    }

    public double Determinant()
    {
        var result = System.Math.Exp(LogDeterminant());
        if (!double.IsFinite(result) || result == 0.0)
        {
            throw new ArithmeticException(
                "Cholesky determinant overflowed or underflowed; use LogDeterminant() instead.");
        }

        return result;
    }

    public DenseMatrix Inverse() => Solve(DenseMatrix.Identity(Size));

    private static void ValidateSymmetry(DenseMatrix matrix, double tolerance)
    {
        for (var row = 0; row < matrix.Rows; row++)
        {
            var rowValues = matrix.GetRowSpan(row);
            for (var column = row + 1; column < matrix.Columns; column++)
            {
                var left = rowValues[column];
                var right = matrix.GetRowSpan(column)[row];
                var scale = System.Math.Max(1.0, System.Math.Max(System.Math.Abs(left), System.Math.Abs(right)));
                var difference = System.Math.Abs(left - right);
                if (!double.IsFinite(difference) || difference > tolerance * scale)
                {
                    throw new ArgumentException(
                        "Cholesky factorization requires symmetry within the configured tolerance.",
                        nameof(matrix));
                }
            }
        }
    }

    private static void ValidateRelativeTolerance(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0 || value >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Relative tolerance must be finite and strictly between zero and one.");
        }
    }

    private static void ValidateFiniteVector(IReadOnlyList<double> values, string parameterName)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (!double.IsFinite(values[i]))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Vector values must be finite.");
            }
        }
    }

    private static void EnsureFinite(double value, string message)
    {
        if (!double.IsFinite(value))
        {
            throw new ArithmeticException(message);
        }
    }
}
