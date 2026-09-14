namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Result of a linear shooting solution for a scalar second-order boundary-value problem.
/// </summary>
/// <remarks>
/// The trajectory is stored as an immutable list of <see cref="SecondOrderOdePoint"/>
/// values. Besides the solution itself, the result exposes the reconstructed initial
/// slope and the final boundary residual so callers can inspect the numerical quality
/// of the shot instead of receiving only an opaque list of values.
/// </remarks>
public sealed class LinearShootingResult
{
    private readonly IReadOnlyList<SecondOrderOdePoint> _points;

    internal LinearShootingResult(
        IReadOnlyList<SecondOrderOdePoint> points,
        double initialSlope,
        double rightBoundaryResidual,
        double auxiliaryRightValue)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("A shooting result must contain at least one point.", nameof(points));
        }

        if (!double.IsFinite(initialSlope)
            || !double.IsFinite(rightBoundaryResidual)
            || !double.IsFinite(auxiliaryRightValue))
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Shooting diagnostics must be finite.");
        }

        _points = Array.AsReadOnly(points.ToArray());
        InitialSlope = initialSlope;
        RightBoundaryResidual = rightBoundaryResidual;
        AuxiliaryRightValue = auxiliaryRightValue;
    }

    /// <summary>Gets the complete boundary-value trajectory.</summary>
    public IReadOnlyList<SecondOrderOdePoint> Points => _points;

    /// <summary>Gets the first point of the trajectory.</summary>
    public SecondOrderOdePoint InitialPoint => _points[0];

    /// <summary>Gets the final point of the trajectory.</summary>
    public SecondOrderOdePoint FinalPoint => _points[^1];

    /// <summary>
    /// Gets the initial derivative <c>y'(x0)</c> selected by the shooting construction.
    /// </summary>
    public double InitialSlope { get; }

    /// <summary>
    /// Gets <c>y(xEnd) - requestedRightBoundary</c> for the combined trajectory.
    /// </summary>
    public double RightBoundaryResidual { get; }

    /// <summary>
    /// Gets the right-end value of the homogeneous auxiliary solution used as the
    /// denominator in the linear shooting correction.
    /// </summary>
    /// <remarks>
    /// A very small absolute value indicates that the chosen Dirichlet boundary map
    /// is singular or numerically ill-conditioned for this shooting formulation.
    /// </remarks>
    public double AuxiliaryRightValue { get; }
}
