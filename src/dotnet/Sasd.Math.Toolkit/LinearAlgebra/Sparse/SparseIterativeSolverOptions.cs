namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Shared convergence and validation options for iterative sparse linear-system solvers.
/// </summary>
/// <remarks>
/// <para>
/// A solver accepts convergence when the Euclidean residual norm satisfies
/// <c>||b-A*x||2 &lt;= max(AbsoluteTolerance, RelativeTolerance * ||b||2)</c>.
/// Keeping absolute and relative tolerances separate makes the stopping rule explicit for both
/// very small and strongly scaled right-hand sides.
/// </para>
/// <para>
/// The options type is intentionally solver-neutral so Conjugate Gradient, GMRES and BiCGSTAB can
/// share the same convergence vocabulary instead of developing incompatible tolerance semantics.
/// Individual solvers may impose additional mathematical contracts.
/// </para>
/// </remarks>
public sealed record SparseIterativeSolverOptions
{
    /// <summary>Gets the absolute residual tolerance.</summary>
    public double AbsoluteTolerance { get; init; } = 1e-12;

    /// <summary>Gets the residual tolerance relative to <c>||b||2</c>.</summary>
    public double RelativeTolerance { get; init; } = 1e-10;

    /// <summary>Gets the maximum number of completed solver iterations.</summary>
    public int MaximumIterations { get; init; } = 1000;

    /// <summary>
    /// Gets whether solvers whose mathematical contract requires symmetry should validate the
    /// sparse matrix before iteration begins.
    /// </summary>
    public bool ValidateSymmetry { get; init; } = true;

    /// <summary>
    /// Gets the relative coefficient tolerance used by optional sparse symmetry validation.
    /// </summary>
    public double SymmetryTolerance { get; init; } = 1e-12;

    internal void Validate()
    {
        if (!double.IsFinite(AbsoluteTolerance) || AbsoluteTolerance < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(AbsoluteTolerance),
                "Absolute tolerance must be finite and non-negative.");
        }

        if (!double.IsFinite(RelativeTolerance) || RelativeTolerance < 0.0 || RelativeTolerance >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RelativeTolerance),
                "Relative tolerance must be finite and in the interval [0, 1).");
        }

        if (AbsoluteTolerance == 0.0 && RelativeTolerance == 0.0)
        {
            throw new ArgumentException(
                "At least one of absolute or relative tolerance must be greater than zero.");
        }

        if (MaximumIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumIterations),
                "Maximum iterations must be greater than zero.");
        }

        if (!double.IsFinite(SymmetryTolerance) || SymmetryTolerance < 0.0 || SymmetryTolerance >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SymmetryTolerance),
                "Symmetry tolerance must be finite and in the interval [0, 1).");
        }
    }
}
