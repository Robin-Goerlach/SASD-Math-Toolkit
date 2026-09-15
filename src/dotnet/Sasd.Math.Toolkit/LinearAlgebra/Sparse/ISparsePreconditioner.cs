namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Applies an approximate inverse used to precondition iterative sparse linear-system solvers.
/// </summary>
/// <remarks>
/// <para>
/// A preconditioner conceptually provides the operation <c>z = M^-1 * r</c> without requiring
/// callers or solvers to materialize <c>M^-1</c>. Implementations should be deterministic, avoid
/// mutating caller-owned input, and support repeated application efficiently because the operation
/// normally appears once or more per solver iteration.
/// </para>
/// <para>
/// The interface intentionally describes only the application contract. Mathematical properties
/// such as symmetry or positive definiteness remain requirements of the solver that consumes the
/// preconditioner. For example, Preconditioned Conjugate Gradient requires an SPD preconditioner,
/// whereas later nonsymmetric Krylov solvers can accept a broader class of preconditioners.
/// </para>
/// </remarks>
public interface ISparsePreconditioner
{
    /// <summary>Gets the vector dimension accepted by this preconditioner.</summary>
    int Size { get; }

    /// <summary>
    /// Applies the approximate inverse to <paramref name="source"/> and writes the result to
    /// <paramref name="destination"/>.
    /// </summary>
    /// <remarks>
    /// Implementations should permit repeated use without per-call allocation where practical.
    /// Source and destination may refer to overlapping storage only when the concrete implementation
    /// explicitly documents that behavior.
    /// </remarks>
    void Apply(ReadOnlySpan<double> source, Span<double> destination);
}
