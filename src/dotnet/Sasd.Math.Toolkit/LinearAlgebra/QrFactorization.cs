namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Householder QR factorization for square and tall dense matrices.
/// </summary>
/// <remarks>
/// <para>
/// The factorization represents an <c>m x n</c> matrix with <c>m &gt;= n</c> as
/// <c>A = Q*R</c>, where the exposed orthogonal factor is the thin <c>m x n</c>
/// matrix and <c>R</c> is <c>n x n</c> upper triangular.
/// </para>
/// <para>
/// Householder reflections are used rather than classical Gram-Schmidt because they
/// provide substantially better numerical orthogonality for general dense problems.
/// The packed reflector representation is retained internally so least-squares solves
/// can apply <c>Q^T</c> without materializing <c>Q</c>.
/// </para>
/// <para>
/// Rank detection is based on the diagonal of <c>R</c> relative to the largest diagonal
/// magnitude. This is a practical diagnostic, not a substitute for an SVD-based rank or
/// condition-number analysis. SVD is a later modernization milestone.
/// </para>
/// </remarks>
public sealed class QrFactorization
{
    /// <summary>
    /// Default relative threshold used for the practical full-column-rank diagnostic.
    /// </summary>
    public const double DefaultRelativeRankTolerance = 1e-12;

    private readonly double[] _qr;
    private readonly double[] _rDiagonal;

    private QrFactorization(
        int rows,
        int columns,
        double[] qr,
        double[] rDiagonal,
        double relativeRankTolerance,
        int estimatedRank)
    {
        Rows = rows;
        Columns = columns;
        _qr = qr;
        _rDiagonal = rDiagonal;
        RelativeRankTolerance = relativeRankTolerance;
        EstimatedRank = estimatedRank;
    }

    /// <summary>Gets the row count of the original matrix.</summary>
    public int Rows { get; }

    /// <summary>Gets the column count of the original matrix.</summary>
    public int Columns { get; }

    /// <summary>
    /// Gets the relative threshold used to estimate whether diagonal entries of <c>R</c>
    /// are numerically significant.
    /// </summary>
    public double RelativeRankTolerance { get; }

    /// <summary>
    /// Gets the estimated column rank derived from the diagonal of <c>R</c>.
    /// </summary>
    public int EstimatedRank { get; }

    /// <summary>Gets whether all original matrix columns are numerically independent.</summary>
    public bool IsFullColumnRank => EstimatedRank == Columns;

    /// <summary>
    /// Gets a newly allocated copy of the upper-triangular <c>R</c> factor.
    /// </summary>
    public DenseMatrix UpperTriangularFactor
    {
        get
        {
            var result = new DenseMatrix(Columns, Columns);
            for (var row = 0; row < Columns; row++)
            {
                var destination = result.GetMutableRowSpan(row);
                for (var column = row; column < Columns; column++)
                {
                    destination[column] = row == column
                        ? _rDiagonal[row]
                        : _qr[(row * Columns) + column];
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Gets a newly allocated thin orthogonal factor <c>Q</c> with dimensions
    /// <c>Rows x Columns</c>.
    /// </summary>
    /// <remarks>
    /// Least-squares solving does not construct this matrix; it applies the packed
    /// Householder reflectors directly. Materialize <c>Q</c> only when diagnostics,
    /// reconstruction, or another algorithm actually needs it.
    /// </remarks>
    public DenseMatrix ThinOrthogonalFactor
    {
        get
        {
            var q = new DenseMatrix(Rows, Columns);

            // Replaying the reflectors in reverse order constructs the same thin Q that
            // satisfies A = Q*R while avoiding storage of a full m x m orthogonal matrix.
            for (var reflector = Columns - 1; reflector >= 0; reflector--)
            {
                q.GetMutableRowSpan(reflector)[reflector] = 1.0;
                var reflectorPivot = _qr[(reflector * Columns) + reflector];
                if (reflectorPivot == 0.0)
                {
                    continue;
                }

                for (var column = reflector; column < Columns; column++)
                {
                    var dot = 0.0;
                    for (var row = reflector; row < Rows; row++)
                    {
                        var product = _qr[(row * Columns) + reflector] * q.GetRowSpan(row)[column];
                        EnsureFiniteComputation(product, "QR reconstruction produced a non-finite product.");
                        dot += product;
                        EnsureFiniteComputation(dot, "QR reconstruction overflowed while accumulating a reflector dot product.");
                    }

                    var scale = -dot / reflectorPivot;
                    EnsureFiniteComputation(scale, "QR reconstruction produced a non-finite reflector scale.");

                    for (var row = reflector; row < Rows; row++)
                    {
                        var destination = q.GetMutableRowSpan(row);
                        var updated = destination[column] + (scale * _qr[(row * Columns) + reflector]);
                        EnsureFiniteComputation(updated, "QR reconstruction produced a non-finite matrix entry.");
                        destination[column] = updated;
                    }
                }
            }

            return q;
        }
    }

    /// <summary>
    /// Computes a Householder QR factorization of a square or tall dense matrix.
    /// </summary>
    /// <param name="matrix">Finite matrix with at least as many rows as columns.</param>
    /// <param name="relativeRankTolerance">
    /// Relative threshold in the open interval (0, 1) used for the practical rank estimate.
    /// </param>
    public static QrFactorization Decompose(
        DenseMatrix matrix,
        double relativeRankTolerance = DefaultRelativeRankTolerance)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ValidateRelativeRankTolerance(relativeRankTolerance);

        if (matrix.Rows < matrix.Columns)
        {
            throw new ArgumentException(
                "Householder QR currently requires a square or tall matrix (rows >= columns). " +
                "Underdetermined systems are reserved for the planned SVD-based solver.",
                nameof(matrix));
        }

        var rows = matrix.Rows;
        var columns = matrix.Columns;
        var qr = new double[checked(rows * columns)];
        for (var row = 0; row < rows; row++)
        {
            matrix.GetRowSpan(row).CopyTo(qr.AsSpan(row * columns, columns));
        }

        var diagonal = new double[columns];

        for (var reflector = 0; reflector < columns; reflector++)
        {
            var norm = StableColumnNorm(qr, rows, columns, reflector);
            if (norm == 0.0)
            {
                diagonal[reflector] = 0.0;
                continue;
            }

            // Choosing the sign opposite the pivot avoids catastrophic cancellation in
            // the first Householder-vector component.
            if (qr[(reflector * columns) + reflector] < 0.0)
            {
                norm = -norm;
            }

            for (var row = reflector; row < rows; row++)
            {
                var index = (row * columns) + reflector;
                qr[index] /= norm;
                EnsureFiniteComputation(qr[index], "QR factorization produced a non-finite Householder vector.");
            }

            var pivotIndex = (reflector * columns) + reflector;
            qr[pivotIndex] += 1.0;
            EnsureFiniteComputation(qr[pivotIndex], "QR factorization produced a non-finite Householder pivot.");

            for (var column = reflector + 1; column < columns; column++)
            {
                var dot = 0.0;
                for (var row = reflector; row < rows; row++)
                {
                    var product = qr[(row * columns) + reflector] * qr[(row * columns) + column];
                    EnsureFiniteComputation(product, "QR factorization produced a non-finite reflector product.");
                    dot += product;
                    EnsureFiniteComputation(dot, "QR factorization overflowed while accumulating a reflector dot product.");
                }

                var scale = -dot / qr[pivotIndex];
                EnsureFiniteComputation(scale, "QR factorization produced a non-finite reflector scale.");

                for (var row = reflector; row < rows; row++)
                {
                    var index = (row * columns) + column;
                    var updated = qr[index] + (scale * qr[(row * columns) + reflector]);
                    EnsureFiniteComputation(updated, "QR factorization produced a non-finite matrix entry.");
                    qr[index] = updated;
                }
            }

            diagonal[reflector] = -norm;
        }

        var estimatedRank = EstimateRank(diagonal, relativeRankTolerance);
        return new QrFactorization(
            rows,
            columns,
            qr,
            diagonal,
            relativeRankTolerance,
            estimatedRank);
    }

    /// <summary>
    /// Solves the full-column-rank least-squares problem <c>min ||A*x-b||2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The method applies the packed Householder reflectors to form <c>Q^T*b</c>, then
    /// solves the leading triangular system <c>R*x = (Q^T*b)[0:n]</c>. It therefore avoids
    /// forming the normal equations <c>A^T*A</c>, which would square the condition number.
    /// </para>
    /// <para>
    /// Rank-deficient and underdetermined least-squares problems are intentionally deferred
    /// to the planned SVD milestone because a simple arbitrary solution would hide important
    /// diagnostic information.
    /// </para>
    /// </remarks>
    public double[] SolveLeastSquares(IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (rightHandSide.Count != Rows)
        {
            throw new ArgumentException(
                "Right-hand side length must match the factorized matrix row count.",
                nameof(rightHandSide));
        }

        if (!IsFullColumnRank)
        {
            throw new ArithmeticException(
                "Least-squares solve requires full column rank at the configured relative rank tolerance.");
        }

        var work = rightHandSide as double[] is { } array
            ? (double[])array.Clone()
            : rightHandSide.ToArray();
        ValidateFiniteVector(work, nameof(rightHandSide));

        // Compute Q^T*b by replaying the Householder transformations. Only this vector
        // changes; the compact factorization remains reusable for additional right sides.
        for (var reflector = 0; reflector < Columns; reflector++)
        {
            var reflectorPivot = _qr[(reflector * Columns) + reflector];
            var dot = 0.0;
            for (var row = reflector; row < Rows; row++)
            {
                var product = _qr[(row * Columns) + reflector] * work[row];
                EnsureFiniteComputation(product, "QR least-squares solve produced a non-finite reflector product.");
                dot += product;
                EnsureFiniteComputation(dot, "QR least-squares solve overflowed while accumulating a reflector dot product.");
            }

            var scale = -dot / reflectorPivot;
            EnsureFiniteComputation(scale, "QR least-squares solve produced a non-finite reflector scale.");

            for (var row = reflector; row < Rows; row++)
            {
                var updated = work[row] + (scale * _qr[(row * Columns) + reflector]);
                EnsureFiniteComputation(updated, "QR least-squares solve produced a non-finite transformed right-hand side.");
                work[row] = updated;
            }
        }

        // Solve the upper-triangular n x n system. The unused tail of work contains the
        // orthogonal residual coordinates and therefore does not participate in x.
        for (var row = Columns - 1; row >= 0; row--)
        {
            work[row] /= _rDiagonal[row];
            EnsureFiniteComputation(work[row], "QR least-squares back substitution produced a non-finite solution value.");

            for (var previous = 0; previous < row; previous++)
            {
                var updated = work[previous] - (work[row] * _qr[(previous * Columns) + row]);
                EnsureFiniteComputation(updated, "QR least-squares back substitution produced a non-finite intermediate value.");
                work[previous] = updated;
            }
        }

        var solution = new double[Columns];
        Array.Copy(work, solution, Columns);
        return solution;
    }

    private static double StableColumnNorm(
        double[] matrix,
        int rows,
        int columns,
        int column)
    {
        var scale = 0.0;
        var scaledSquares = 1.0;

        for (var row = column; row < rows; row++)
        {
            var magnitude = System.Math.Abs(matrix[(row * columns) + column]);
            if (magnitude == 0.0)
            {
                continue;
            }

            if (scale < magnitude)
            {
                var ratio = scale / magnitude;
                scaledSquares = 1.0 + (scaledSquares * ratio * ratio);
                scale = magnitude;
            }
            else
            {
                var ratio = magnitude / scale;
                scaledSquares += ratio * ratio;
            }
        }

        if (scale == 0.0)
        {
            return 0.0;
        }

        var norm = scale * System.Math.Sqrt(scaledSquares);
        EnsureFiniteComputation(norm, "QR factorization produced a non-finite column norm.");
        return norm;
    }

    private static int EstimateRank(double[] diagonal, double relativeRankTolerance)
    {
        var largest = 0.0;
        for (var index = 0; index < diagonal.Length; index++)
        {
            largest = System.Math.Max(largest, System.Math.Abs(diagonal[index]));
        }

        if (largest == 0.0)
        {
            return 0;
        }

        var threshold = largest * relativeRankTolerance;
        var rank = 0;
        for (var index = 0; index < diagonal.Length; index++)
        {
            if (System.Math.Abs(diagonal[index]) > threshold)
            {
                rank++;
            }
        }

        return rank;
    }

    private static void ValidateRelativeRankTolerance(double value)
    {
        if (!double.IsFinite(value) || value <= 0.0 || value >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Relative rank tolerance must be finite and strictly between zero and one.");
        }
    }

    private static void ValidateFiniteVector(IReadOnlyList<double> values, string parameterName)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (!double.IsFinite(values[index]))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Vector values must be finite.");
            }
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
