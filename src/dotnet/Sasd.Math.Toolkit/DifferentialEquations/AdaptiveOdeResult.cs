namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Result of an adaptive scalar ODE integration.
/// </summary>
/// <remarks>
/// Only accepted steps are exposed as solution points. Rejected trial steps are
/// counted separately because they are useful diagnostics for tolerance and step-size
/// choices, but they are not part of the numerical solution trajectory.
/// </remarks>
public sealed class AdaptiveOdeResult
{
    internal AdaptiveOdeResult(
        IEnumerable<OdePoint> points,
        int acceptedSteps,
        int rejectedSteps,
        AdaptiveOdeStatus status,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        var copy = points.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("An ODE result must contain at least the initial point.", nameof(points));
        }

        Points = Array.AsReadOnly(copy);
        AcceptedSteps = acceptedSteps;
        RejectedSteps = rejectedSteps;
        Status = status;
        Message = message;
    }

    /// <summary>Gets the initial point and every subsequently accepted solution point.</summary>
    public IReadOnlyList<OdePoint> Points { get; }

    /// <summary>Gets the number of trial steps that were accepted.</summary>
    public int AcceptedSteps { get; }

    /// <summary>Gets the number of trial steps that were rejected by the local error controller.</summary>
    public int RejectedSteps { get; }

    /// <summary>Gets the total number of attempted steps.</summary>
    public int AttemptedSteps => AcceptedSteps + RejectedSteps;

    /// <summary>Gets the termination status.</summary>
    public AdaptiveOdeStatus Status { get; }

    /// <summary>Gets an optional diagnostic message.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the requested end point was reached successfully.</summary>
    public bool Completed => Status == AdaptiveOdeStatus.Completed;

    /// <summary>Gets the last accepted solution point.</summary>
    public OdePoint FinalPoint => Points[Points.Count - 1];
}
