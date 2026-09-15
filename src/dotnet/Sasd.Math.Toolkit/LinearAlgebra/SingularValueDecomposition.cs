namespace Sasd.Numerics.LinearAlgebra;

/// <summary>
/// Singular value decomposition for real dense matrices with minimum-norm least-squares support.
/// </summary>
/// <remarks>
/// <para>
/// The decomposition represents an <c>m x n</c> matrix as <c>A = U*S*V^T</c>. The public
/// <see cref="LeftSingularVectors"/> and <see cref="RightSingularVectors"/> properties expose
/// thin factors with <c>min(m,n)</c> columns, while the singular values are sorted from largest
/// to smallest.
/// </para>
/// <para>
/// The managed reference implementation uses a one-sided Jacobi SVD. It is intentionally
/// independent from <c>A^T*A</c>, so it does not square the condition number merely to obtain
/// singular vectors. That makes it a substantially better basis for rank diagnostics,
/// pseudoinverses and difficult least-squares problems than normal equations.
/// </para>
/// <para>
/// Rank is a numerical decision, not an exact algebraic property in floating-point arithmetic.
/// Singular values at or below <see cref="RankThreshold"/> are treated as truncated when solving
/// or forming a pseudoinverse. Callers can therefore choose a tolerance appropriate to the scale
/// and scientific meaning of their problem.
/// </para>
/// </remarks>
public sealed class SingularValueDecomposition
{
    /// <summary>Default relative threshold used for numerical-rank decisions.</summary>
    public const double DefaultRelativeRankTolerance = 1e-12;

    /// <summary>Default normalized column-correlation target for Jacobi convergence.</summary>
    public const double DefaultRelativeOrthogonalityTolerance = 1e-12;

    /// <summary>Default maximum number of complete Jacobi sweeps.</summary>
    public const int DefaultMaximumSweeps = 100;

    private readonly double[] _leftSingularVectors;
    private readonly double[] _rightSingularVectors;
    private readonly double[] _singularValues;

    private SingularValueDecomposition(
        int rows,
        int columns,
        double[] singularValues,
        double[] leftSingularVectors,
        double[] rightSingularVectors,
        double relativeRankTolerance,
        double relativeOrthogonalityTolerance,
        int sweeps)
    {
        Rows = rows;
        Columns = columns;
        _singularValues = singularValues;
        _leftSingularVectors = leftSingularVectors;
        _rightSingularVectors = rightSingularVectors;
        RelativeRankTolerance = relativeRankTolerance;
        RelativeOrthogonalityTolerance = relativeOrthogonalityTolerance;
        Sweeps = sweeps;

        LargestSingularValue = singularValues.Length == 0 ? 0.0 : singularValues[0];
        RankThreshold = LargestSingularValue * relativeRankTolerance;

        var rank = 0;
        for (var index = 0; index < singularValues.Length; index++)
        {
            if (singularValues[index] > RankThreshold)
            {
                rank++;
            }
        }

        EstimatedRank = rank;

        if (EstimatedRank < Components || LargestSingularValue == 0.0)
        {
            ConditionNumber = double.PositiveInfinity;
            ReciprocalConditionNumber = 0.0;
        }
        else
        {
            var smallest = singularValues[^1];
            ConditionNumber = LargestSingularValue / smallest;
            ReciprocalConditionNumber = smallest / LargestSingularValue;
        }
    }

    /// <summary>Gets the number of rows of the original matrix.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns of the original matrix.</summary>
    public int Columns { get; }

    /// <summary>Gets the number of singular triplets, <c>min(Rows, Columns)</c>.</summary>
    public int Components => _singularValues.Length;

    /// <summary>Gets the relative threshold used for numerical-rank decisions.</summary>
    public double RelativeRankTolerance { get; }

    /// <summary>Gets the normalized column-correlation target used by the Jacobi iteration.</summary>
    public double RelativeOrthogonalityTolerance { get; }

    /// <summary>Gets the number of Jacobi sweeps used to reach the requested orthogonality.</summary>
    public int Sweeps { get; }

    /// <summary>Gets the largest singular value.</summary>
    public double LargestSingularValue { get; }

    /// <summary>
    /// Gets the absolute singular-value threshold below which components are truncated by
    /// rank-aware operations.
    /// </summary>
    public double RankThreshold { get; }

    /// <summary>Gets the estimated numerical rank at the configured threshold.</summary>
    public int EstimatedRank { get; }

    /// <summary>Gets whether all <c>min(m,n)</c> singular directions are numerically retained.</summary>
    public bool IsFullRank => EstimatedRank == Components;

    /// <summary>
    /// Gets the 2-norm condition number implied by the singular values, or positive infinity
    /// when the matrix is numerically rank deficient at the configured threshold.
    /// </summary>
    public double ConditionNumber { get; }

    /// <summary>
    /// Gets the reciprocal 2-norm condition number, or zero for a numerically rank-deficient matrix.
    /// </summary>
    public double ReciprocalConditionNumber { get; }

    /// <summary>Gets a defensive copy of the singular values in descending order.</summary>
    public double[] SingularValues => (double[])_singularValues.Clone();

    /// <summary>Gets a newly allocated thin left-singular-vector matrix.</summary>
    public DenseMatrix LeftSingularVectors =>
        CopyMatrix(_leftSingularVectors, Rows, Components);

    /// <summary>Gets a newly allocated thin right-singular-vector matrix.</summary>
    public DenseMatrix RightSingularVectors =>
        CopyMatrix(_rightSingularVectors, Columns, Components);

    /// <summary>Gets one singular value without allocating the complete singular-value array.</summary>
    public double GetSingularValue(int index)
    {
        if ((uint)index >= (uint)Components)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _singularValues[index];
    }

    /// <summary>
    /// Computes the SVD of an arbitrary non-empty real dense matrix.
    /// </summary>
    /// <param name="matrix">Finite input matrix.</param>
    /// <param name="relativeRankTolerance">
    /// Relative singular-value threshold in the open interval (0, 1).
    /// </param>
    /// <param name="relativeOrthogonalityTolerance">
    /// Maximum normalized cross-correlation accepted between Jacobi columns.
    /// </param>
    /// <param name="maximumSweeps">Maximum number of complete one-sided Jacobi sweeps.</param>
    /// <exception cref="ArithmeticException">
    /// Thrown when the Jacobi iteration cannot reach the requested orthogonality within the
    /// configured sweep budget or an intermediate floating-point operation becomes non-finite.
    /// </exception>
    public static SingularValueDecomposition Decompose(
        DenseMatrix matrix,
        double relativeRankTolerance = DefaultRelativeRankTolerance,
        double relativeOrthogonalityTolerance = DefaultRelativeOrthogonalityTolerance,
        int maximumSweeps = DefaultMaximumSweeps)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ValidateRelativeTolerance(relativeRankTolerance, nameof(relativeRankTolerance));
        ValidateRelativeTolerance(relativeOrthogonalityTolerance, nameof(relativeOrthogonalityTolerance));
        if (maximumSweeps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSweeps), "Maximum sweeps must be greater than zero.");
        }

        if (matrix.Rows >= matrix.Columns)
        {
            var core = DecomposeTall(matrix, relativeOrthogonalityTolerance, maximumSweeps);
            return new SingularValueDecomposition(
                matrix.Rows,
                matrix.Columns,
                core.SingularValues,
                core.LeftSingularVectors,
                core.RightSingularVectors,
                relativeRankTolerance,
                relativeOrthogonalityTolerance,
                core.Sweeps);
        }

        // The one-sided kernel is simplest and most efficient for square/tall matrices.
        // For a wide matrix A, factor A^T = Ut*S*Vt^T and use
        // A = Vt*S*Ut^T. This preserves the same singular values and swaps the roles of
        // the thin left/right singular vectors without forming normal equations.
        var transposed = Transpose(matrix);
        var transposedCore = DecomposeTall(
            transposed,
            relativeOrthogonalityTolerance,
            maximumSweeps);

        return new SingularValueDecomposition(
            matrix.Rows,
            matrix.Columns,
            transposedCore.SingularValues,
            transposedCore.RightSingularVectors,
            transposedCore.LeftSingularVectors,
            relativeRankTolerance,
            relativeOrthogonalityTolerance,
            transposedCore.Sweeps);
    }

    /// <summary>
    /// Solves <c>min ||A*x-b||2</c> using the truncated SVD and returns the minimum-norm solution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Components whose singular value is at or below <see cref="RankThreshold"/> are omitted.
    /// This handles rank-deficient and underdetermined systems without inventing an arbitrary
    /// basic solution. The returned vector is the minimum-Euclidean-norm solution associated
    /// with the configured truncation threshold.
    /// </para>
    /// <para>
    /// The operation reuses the factorization and does not modify the supplied right-hand side.
    /// </para>
    /// </remarks>
    public double[] SolveLeastSquares(IReadOnlyList<double> rightHandSide)
    {
        ArgumentNullException.ThrowIfNull(rightHandSide);
        if (rightHandSide.Count != Rows)
        {
            throw new ArgumentException(
                "Right-hand side length must match the original matrix row count.",
                nameof(rightHandSide));
        }

        ValidateFiniteVector(rightHandSide, nameof(rightHandSide));

        var solution = new double[Columns];
        for (var component = 0; component < Components; component++)
        {
            var singularValue = _singularValues[component];
            if (singularValue <= RankThreshold)
            {
                continue;
            }

            var projection = 0.0;
            for (var row = 0; row < Rows; row++)
            {
                var product = _leftSingularVectors[(row * Components) + component] * rightHandSide[row];
                EnsureFinite(product, "SVD least-squares projection produced a non-finite product.");
                projection += product;
                EnsureFinite(projection, "SVD least-squares projection overflowed.");
            }

            var coefficient = projection / singularValue;
            EnsureFinite(coefficient, "SVD least-squares solve produced a non-finite singular coefficient.");

            for (var column = 0; column < Columns; column++)
            {
                var index = column;
                var updated = solution[index]
                    + (_rightSingularVectors[(column * Components) + component] * coefficient);
                EnsureFinite(updated, "SVD least-squares solve produced a non-finite solution value.");
                solution[index] = updated;
            }
        }

        return solution;
    }

    /// <summary>
    /// Forms the Moore-Penrose pseudoinverse using the configured numerical-rank threshold.
    /// </summary>
    /// <remarks>
    /// The returned matrix has shape <c>Columns x Rows</c>. Truncated singular directions
    /// contribute zero, matching <see cref="SolveLeastSquares(IReadOnlyList{double})"/>.
    /// </remarks>
    public DenseMatrix PseudoInverse()
    {
        var result = new DenseMatrix(Columns, Rows);

        for (var component = 0; component < Components; component++)
        {
            var singularValue = _singularValues[component];
            if (singularValue <= RankThreshold)
            {
                continue;
            }

            var inverseSingularValue = 1.0 / singularValue;
            EnsureFinite(inverseSingularValue, "SVD pseudoinverse produced a non-finite reciprocal singular value.");

            for (var outputRow = 0; outputRow < Columns; outputRow++)
            {
                var weightedRight = _rightSingularVectors[(outputRow * Components) + component]
                    * inverseSingularValue;
                EnsureFinite(weightedRight, "SVD pseudoinverse produced a non-finite weighted singular vector.");

                var destination = result.GetMutableRowSpan(outputRow);
                for (var outputColumn = 0; outputColumn < Rows; outputColumn++)
                {
                    var updated = destination[outputColumn]
                        + (weightedRight * _leftSingularVectors[(outputColumn * Components) + component]);
                    EnsureFinite(updated, "SVD pseudoinverse produced a non-finite matrix entry.");
                    destination[outputColumn] = updated;
                }
            }
        }

        return result;
    }

    private static TallSvdCore DecomposeTall(
        DenseMatrix matrix,
        double relativeOrthogonalityTolerance,
        int maximumSweeps)
    {
        var rows = matrix.Rows;
        var columns = matrix.Columns;
        var work = new double[checked(rows * columns)];
        for (var row = 0; row < rows; row++)
        {
            matrix.GetRowSpan(row).CopyTo(work.AsSpan(row * columns, columns));
        }

        var right = new double[checked(columns * columns)];
        for (var index = 0; index < columns; index++)
        {
            right[(index * columns) + index] = 1.0;
        }

        var sweeps = 0;
        var converged = false;

        for (var sweep = 1; sweep <= maximumSweeps; sweep++)
        {
            sweeps = sweep;
            var rotatedAnyPair = false;

            for (var leftColumn = 0; leftColumn < columns - 1; leftColumn++)
            {
                for (var rightColumn = leftColumn + 1; rightColumn < columns; rightColumn++)
                {
                    var leftNorm = StableColumnNorm(work, rows, columns, leftColumn);
                    var rightNorm = StableColumnNorm(work, rows, columns, rightColumn);
                    if (leftNorm == 0.0 || rightNorm == 0.0)
                    {
                        continue;
                    }

                    var correlation = NormalizedColumnDot(
                        work,
                        rows,
                        columns,
                        leftColumn,
                        rightColumn,
                        leftNorm,
                        rightNorm);

                    if (System.Math.Abs(correlation) <= relativeOrthogonalityTolerance)
                    {
                        continue;
                    }

                    var commonScale = System.Math.Max(leftNorm, rightNorm);
                    var leftRatio = leftNorm / commonScale;
                    var rightRatio = rightNorm / commonScale;
                    var alpha = leftRatio * leftRatio;
                    var beta = rightRatio * rightRatio;
                    var gamma = correlation * leftRatio * rightRatio;

                    if (gamma == 0.0)
                    {
                        // Extremely different column scales can make the rotation smaller than
                        // representable precision. The tiny direction is naturally handled by the
                        // later rank threshold, so forcing an unstable rotation would be worse.
                        continue;
                    }

                    var tangent = StableJacobiTangent(alpha, beta, gamma);
                    var cosine = 1.0 / System.Math.Sqrt(1.0 + (tangent * tangent));
                    var sine = tangent * cosine;
                    EnsureFinite(cosine, "SVD Jacobi rotation produced a non-finite cosine.");
                    EnsureFinite(sine, "SVD Jacobi rotation produced a non-finite sine.");

                    RotateColumns(work, rows, columns, leftColumn, rightColumn, cosine, sine);
                    RotateColumns(right, columns, columns, leftColumn, rightColumn, cosine, sine);
                    rotatedAnyPair = true;
                }
            }

            if (!rotatedAnyPair)
            {
                converged = true;
                break;
            }
        }

        if (!converged)
        {
            converged = ColumnsAreOrthogonal(
                work,
                rows,
                columns,
                relativeOrthogonalityTolerance);
        }

        if (!converged)
        {
            throw new ArithmeticException(
                "One-sided Jacobi SVD did not reach the requested orthogonality within the configured sweep budget.");
        }

        var singularValues = new double[columns];
        for (var column = 0; column < columns; column++)
        {
            singularValues[column] = StableColumnNorm(work, rows, columns, column);
        }

        SortSingularTripletsDescending(singularValues, work, right, rows, columns);

        var left = new double[checked(rows * columns)];
        for (var column = 0; column < columns; column++)
        {
            var singularValue = singularValues[column];
            if (singularValue == 0.0)
            {
                continue;
            }

            for (var row = 0; row < rows; row++)
            {
                var value = work[(row * columns) + column] / singularValue;
                EnsureFinite(value, "SVD normalization produced a non-finite left singular vector entry.");
                left[(row * columns) + column] = value;
            }
        }

        CompleteExactZeroLeftSingularVectors(left, singularValues, rows, columns);
        return new TallSvdCore(singularValues, left, right, sweeps);
    }

    private static double StableColumnNorm(double[] matrix, int rows, int columns, int column)
    {
        var scale = 0.0;
        var scaledSquares = 1.0;

        for (var row = 0; row < rows; row++)
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
        EnsureFinite(norm, "SVD column norm exceeded the representable floating-point range.");
        return norm;
    }

    private static double NormalizedColumnDot(
        double[] matrix,
        int rows,
        int columns,
        int leftColumn,
        int rightColumn,
        double leftNorm,
        double rightNorm)
    {
        var result = 0.0;
        for (var row = 0; row < rows; row++)
        {
            var left = matrix[(row * columns) + leftColumn] / leftNorm;
            var right = matrix[(row * columns) + rightColumn] / rightNorm;
            var product = left * right;
            result += product;
            EnsureFinite(result, "SVD normalized column dot product became non-finite.");
        }

        // Round-off can move a theoretically bounded correlation a few ulps outside [-1, 1].
        return System.Math.Clamp(result, -1.0, 1.0);
    }

    private static double StableJacobiTangent(double alpha, double beta, double gamma)
    {
        var delta = beta - alpha;
        if (delta == 0.0)
        {
            return gamma < 0.0 ? -1.0 : 1.0;
        }

        var twiceGammaMagnitude = 2.0 * System.Math.Abs(gamma);
        var denominator = System.Math.Abs(delta)
            + System.Math.Sqrt((delta * delta) + (twiceGammaMagnitude * twiceGammaMagnitude));
        var magnitude = twiceGammaMagnitude / denominator;

        var negative = (delta < 0.0) != (gamma < 0.0);
        return negative ? -magnitude : magnitude;
    }

    private static void RotateColumns(
        double[] matrix,
        int rows,
        int columns,
        int leftColumn,
        int rightColumn,
        double cosine,
        double sine)
    {
        for (var row = 0; row < rows; row++)
        {
            var leftIndex = (row * columns) + leftColumn;
            var rightIndex = (row * columns) + rightColumn;
            var left = matrix[leftIndex];
            var right = matrix[rightIndex];

            var rotatedLeft = (cosine * left) - (sine * right);
            var rotatedRight = (sine * left) + (cosine * right);
            EnsureFinite(rotatedLeft, "SVD Jacobi rotation produced a non-finite matrix entry.");
            EnsureFinite(rotatedRight, "SVD Jacobi rotation produced a non-finite matrix entry.");

            matrix[leftIndex] = rotatedLeft;
            matrix[rightIndex] = rotatedRight;
        }
    }

    private static bool ColumnsAreOrthogonal(
        double[] matrix,
        int rows,
        int columns,
        double tolerance)
    {
        for (var leftColumn = 0; leftColumn < columns - 1; leftColumn++)
        {
            for (var rightColumn = leftColumn + 1; rightColumn < columns; rightColumn++)
            {
                var leftNorm = StableColumnNorm(matrix, rows, columns, leftColumn);
                var rightNorm = StableColumnNorm(matrix, rows, columns, rightColumn);
                if (leftNorm == 0.0 || rightNorm == 0.0)
                {
                    continue;
                }

                var correlation = NormalizedColumnDot(
                    matrix,
                    rows,
                    columns,
                    leftColumn,
                    rightColumn,
                    leftNorm,
                    rightNorm);
                if (System.Math.Abs(correlation) > tolerance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void SortSingularTripletsDescending(
        double[] singularValues,
        double[] leftWork,
        double[] right,
        int rows,
        int columns)
    {
        for (var target = 0; target < singularValues.Length - 1; target++)
        {
            var best = target;
            for (var candidate = target + 1; candidate < singularValues.Length; candidate++)
            {
                if (singularValues[candidate] > singularValues[best])
                {
                    best = candidate;
                }
            }

            if (best == target)
            {
                continue;
            }

            (singularValues[target], singularValues[best]) =
                (singularValues[best], singularValues[target]);
            SwapColumns(leftWork, rows, columns, target, best);
            SwapColumns(right, columns, columns, target, best);
        }
    }

    private static void SwapColumns(
        double[] matrix,
        int rows,
        int columns,
        int firstColumn,
        int secondColumn)
    {
        for (var row = 0; row < rows; row++)
        {
            var first = (row * columns) + firstColumn;
            var second = (row * columns) + secondColumn;
            (matrix[first], matrix[second]) = (matrix[second], matrix[first]);
        }
    }

    private static void CompleteExactZeroLeftSingularVectors(
        double[] left,
        double[] singularValues,
        int rows,
        int columns)
    {
        for (var column = 0; column < columns; column++)
        {
            if (singularValues[column] != 0.0)
            {
                continue;
            }

            if (!TryCreateOrthonormalCompletion(left, rows, columns, column))
            {
                throw new ArithmeticException(
                    "SVD could not construct a deterministic orthonormal basis for a zero singular direction.");
            }
        }
    }

    private static bool TryCreateOrthonormalCompletion(
        double[] left,
        int rows,
        int columns,
        int targetColumn)
    {
        var candidate = new double[rows];

        for (var axis = 0; axis < rows; axis++)
        {
            Array.Clear(candidate);
            candidate[axis] = 1.0;

            // Two modified Gram-Schmidt passes make the deterministic completion more robust
            // when the already computed singular vectors are close to the chosen coordinate axis.
            for (var pass = 0; pass < 2; pass++)
            {
                for (var previous = 0; previous < targetColumn; previous++)
                {
                    var projection = 0.0;
                    for (var row = 0; row < rows; row++)
                    {
                        projection += candidate[row] * left[(row * columns) + previous];
                    }

                    for (var row = 0; row < rows; row++)
                    {
                        candidate[row] -= projection * left[(row * columns) + previous];
                    }
                }
            }

            var norm = StableVectorNorm(candidate);
            if (norm <= 1e-12)
            {
                continue;
            }

            for (var row = 0; row < rows; row++)
            {
                left[(row * columns) + targetColumn] = candidate[row] / norm;
            }

            return true;
        }

        return false;
    }

    private static double StableVectorNorm(IReadOnlyList<double> values)
    {
        var scale = 0.0;
        var scaledSquares = 1.0;
        for (var index = 0; index < values.Count; index++)
        {
            var magnitude = System.Math.Abs(values[index]);
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

        return scale == 0.0 ? 0.0 : scale * System.Math.Sqrt(scaledSquares);
    }

    private static DenseMatrix Transpose(DenseMatrix matrix)
    {
        var result = new DenseMatrix(matrix.Columns, matrix.Rows);
        for (var row = 0; row < matrix.Rows; row++)
        {
            var source = matrix.GetRowSpan(row);
            for (var column = 0; column < matrix.Columns; column++)
            {
                result.GetMutableRowSpan(column)[row] = source[column];
            }
        }

        return result;
    }

    private static DenseMatrix CopyMatrix(double[] source, int rows, int columns)
    {
        var result = new DenseMatrix(rows, columns);
        for (var row = 0; row < rows; row++)
        {
            source.AsSpan(row * columns, columns).CopyTo(result.GetMutableRowSpan(row));
        }

        return result;
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
        for (var index = 0; index < values.Count; index++)
        {
            if (!double.IsFinite(values[index]))
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

    private sealed class TallSvdCore
    {
        public TallSvdCore(
            double[] singularValues,
            double[] leftSingularVectors,
            double[] rightSingularVectors,
            int sweeps)
        {
            SingularValues = singularValues;
            LeftSingularVectors = leftSingularVectors;
            RightSingularVectors = rightSingularVectors;
            Sweeps = sweeps;
        }

        public double[] SingularValues { get; }
        public double[] LeftSingularVectors { get; }
        public double[] RightSingularVectors { get; }
        public int Sweeps { get; }
    }
}
