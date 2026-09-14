using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Result of nonlinear shooting for a scalar second-order Dirichlet boundary-value problem.
/// </summary>
/// <remarks>
/// Nonlinear shooting is iterative, so a result also records the termination status,
/// iteration count and final boundary residual. A non-converged result still contains
/// the trajectory associated with the last usable slope estimate, which can be useful
/// for diagnostics without pretending that the requested boundary condition was met.
/// </remarks>
public sealed class NonlinearShootingResult
{
    private readonly IReadOnlyList<SecondOrderOdePoint> _points;

    internal NonlinearShootingResult(
        IReadOnlyList<SecondOrderOdePoint> points,
        double initialSlope,
        double rightBoundaryResidual,
        int iterations,
        IterationStatus status,
        string? message)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("A shooting result must contain at least one point.", nameof(points));
        }

        if (!double.IsFinite(initialSlope) || !double.IsFinite(rightBoundaryResidual))
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Shooting diagnostics must be finite.");
        }

        if (iterations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iteration count must not be negative.");
        }

        _points = Array.AsReadOnly(points.ToArray());
        InitialSlope = initialSlope;
        RightBoundaryResidual = rightBoundaryResidual;
        Iterations = iterations;
        Status = status;
        Message = message;
    }

    /// <summary>Gets the complete trajectory for the final slope estimate.</summary>
    public IReadOnlyList<SecondOrderOdePoint> Points => _points;

    /// <summary>Gets the left endpoint of the final trajectory.</summary>
    public SecondOrderOdePoint InitialPoint => _points[0];

    /// <summary>Gets the right endpoint of the final trajectory.</summary>
    public SecondOrderOdePoint FinalPoint => _points[^1];

    /// <summary>Gets the final estimate of the unknown initial derivative <c>y'(x0)</c>.</summary>
    public double InitialSlope { get; }

    /// <summary>Gets <c>y(xEnd) - requestedRightBoundary</c> for the final shot.</summary>
    public double RightBoundaryResidual { get; }

    /// <summary>Gets the number of secant corrections performed.</summary>
    public int Iterations { get; }

    /// <summary>Gets how the slope search terminated.</summary>
    public IterationStatus Status { get; }

    /// <summary>Gets optional diagnostic information for non-converged results.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the requested boundary tolerance was satisfied.</summary>
    public bool Converged => Status == IterationStatus.Converged;
}
