using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Common convergence options for real and complex root solvers.
/// </summary>
/// <param name="Tolerance">Positive absolute/relative convergence tolerance.</param>
/// <param name="MaximumIterations">Maximum iterations for one root search.</param>
public sealed record RootFindingOptions(
    double Tolerance = NumericConstants.DefaultTolerance,
    int MaximumIterations = NumericConstants.DefaultMaximumIterations)
{
    /// <summary>
    /// Validates the option values before an iterative algorithm starts.
    /// </summary>
    public void Validate()
    {
        NumericGuard.Positive(Tolerance, nameof(Tolerance));
        NumericGuard.Positive(MaximumIterations, nameof(MaximumIterations));
    }
}
