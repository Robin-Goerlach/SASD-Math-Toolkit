namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Represents one coordinate entry supplied while constructing a sparse matrix.
/// </summary>
/// <remarks>
/// Validation is intentionally performed by the receiving sparse-matrix factory because row and
/// column bounds depend on the target matrix dimensions. Zero values are accepted as input and are
/// omitted from canonical compressed storage.
/// </remarks>
public readonly record struct SparseMatrixEntry(int Row, int Column, double Value);
