using Sasd.Numerics.Common;

namespace Sasd.Numerics.LinearAlgebra.Sparse;

/// <summary>
/// Diagnostic result returned by iterative sparse linear-system solvers.
/// </summary>
/// <remarks>
/// <para>
/// The result reuses the toolkit-wide <see cref="IterationStatus"/> vocabulary while adding
/// diagnostics that are specific to linear solves: Euclidean residual norm, relative residual,
/// the effective convergence threshold and the norm of the right-hand side.
/// </para>
/// <para>
/// A non-converged result can still contain a useful best-so-far solution. Callers should therefore
/// inspect <see cref="Status"/> and <see cref="ResidualNorm"/> rather than assuming that the mere
/// presence of a solution means the requested tolerance was met.
/// </para>
/// </remarks>
public sealed class SparseLinearSolveResult
{
    private readonly double[] _solution;

    internal SparseLinearSolveResult(
        double[] solution,
        int iterations,
        IterationStatus status,
        double residualNorm,
        double initialResidualNorm,
        double rightHandSideNorm,
        double convergenceThreshold,
        string? message = null)
    {
        _solution = solution;
        Iterations = iterations;
        Status = status;
        ResidualNorm = residualNorm;
        InitialResidualNorm = initialResidualNorm;
        RightHandSideNorm = rightHandSideNorm;
        ConvergenceThreshold = convergenceThreshold;
        Message = message;
    }

    /// <summary>Gets a defensive copy of the best available solution.</summary>
    public double[] Solution => (double[])_solution.Clone();

    /// <summary>Gets the number of completed solver iterations.</summary>
    public int Iterations { get; }

    /// <summary>Gets the reason why iteration stopped.</summary>
    public IterationStatus Status { get; }

    /// <summary>Gets whether the solver reported convergence.</summary>
    public bool Converged => Status == IterationStatus.Converged;

    /// <summary>Gets the final Euclidean residual norm, or NaN when no finite residual is available.</summary>
    public double ResidualNorm { get; }

    /// <summary>Gets the Euclidean residual norm before the first iteration, or NaN if unavailable.</summary>
    public double InitialResidualNorm { get; }

    /// <summary>Gets the Euclidean norm of the right-hand side.</summary>
    public double RightHandSideNorm { get; }

    /// <summary>
    /// Gets the effective absolute residual threshold used for convergence.
    /// </summary>
    public double ConvergenceThreshold { get; }

    /// <summary>Gets an optional diagnostic message for non-routine termination.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the final residual norm is finite.</summary>
    public bool HasFiniteResidual => double.IsFinite(ResidualNorm);

    /// <summary>
    /// Gets <c>ResidualNorm / RightHandSideNorm</c> when meaningful.
    /// </summary>
    /// <remarks>
    /// When the right-hand side is exactly zero, a zero residual maps to zero while a non-zero
    /// residual maps to positive infinity. If the residual itself is unavailable, this property is NaN.
    /// </remarks>
    public double RelativeResidualNorm
    {
        get
        {
            if (!double.IsFinite(ResidualNorm))
            {
                return double.NaN;
            }

            if (RightHandSideNorm == 0.0)
            {
                return ResidualNorm == 0.0 ? 0.0 : double.PositiveInfinity;
            }

            return ResidualNorm / RightHandSideNorm;
        }
    }

    /// <summary>Gets one solution component without allocating a complete solution copy.</summary>
    public double GetSolutionValue(int index)
    {
        if ((uint)index >= (uint)_solution.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _solution[index];
    }

    /// <summary>Copies the best available solution into caller-provided storage.</summary>
    public void CopySolutionTo(Span<double> destination)
    {
        if (destination.Length != _solution.Length)
        {
            throw new ArgumentException("Destination length must match solution length.", nameof(destination));
        }

        _solution.AsSpan().CopyTo(destination);
    }
}
