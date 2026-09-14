using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Common convergence options for scalar root solvers.
/// </summary>
public sealed record RootFindingOptions(
    double Tolerance = NumericConstants.DefaultTolerance,
    int MaximumIterations = NumericConstants.DefaultMaximumIterations)
{
    public void Validate()
    {
        NumericGuard.Positive(Tolerance, nameof(Tolerance));
        NumericGuard.Positive(MaximumIterations, nameof(MaximumIterations));
    }
}
