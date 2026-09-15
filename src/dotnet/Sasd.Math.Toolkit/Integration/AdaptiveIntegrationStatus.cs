namespace Sasd.Numerics.Integration;

/// <summary>
/// Describes how an adaptive quadrature stopped refining its interval partition.
/// </summary>
public enum AdaptiveIntegrationStatus
{
    /// <summary>
    /// Every accepted panel satisfied the requested local error criterion.
    /// </summary>
    Converged = 0,

    /// <summary>
    /// At least one panel reached the configured recursion-depth limit before satisfying
    /// the requested error criterion. The returned value is still the best estimate that
    /// was available at that limit.
    /// </summary>
    MaximumDepthReached = 1,

    /// <summary>
    /// Floating-point resolution no longer allowed an interval to be subdivided into a
    /// distinct midpoint. The returned value is the best estimate available at that point.
    /// </summary>
    NumericalResolutionReached = 2
}
