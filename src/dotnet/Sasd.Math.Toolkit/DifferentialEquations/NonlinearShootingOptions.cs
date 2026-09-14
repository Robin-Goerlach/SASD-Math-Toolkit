using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Convergence controls for nonlinear shooting of scalar second-order boundary-value problems.
/// </summary>
/// <param name="BoundaryTolerance">
/// Positive tolerance used for the right-boundary residual and the secant slope update.
/// </param>
/// <param name="MaximumIterations">Maximum number of secant corrections of the unknown initial slope.</param>
public sealed record NonlinearShootingOptions(
    double BoundaryTolerance = NumericConstants.DefaultTolerance,
    int MaximumIterations = NumericConstants.DefaultMaximumIterations)
{
    internal void Validate()
    {
        NumericGuard.Positive(BoundaryTolerance, nameof(BoundaryTolerance));
        NumericGuard.Positive(MaximumIterations, nameof(MaximumIterations));
    }
}
