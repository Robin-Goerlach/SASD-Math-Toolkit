using Sasd.Numerics.Common;

namespace Sasd.Numerics.Integration;

/// <summary>
/// Diagnostic result returned by Romberg integration.
/// </summary>
/// <param name="Value">Best extrapolated integral estimate.</param>
/// <param name="EstimatedError">Difference between the last two diagonal Romberg estimates, or <see langword="null"/> when only the base trapezoid level was requested.</param>
/// <param name="Levels">Number of Romberg levels constructed.</param>
/// <param name="FunctionEvaluations">Number of calls made to the supplied integrand.</param>
/// <param name="Status">Convergence status.</param>
public sealed record RombergIntegrationResult(
    double Value,
    double? EstimatedError,
    int Levels,
    int FunctionEvaluations,
    IterationStatus Status)
{
    /// <summary>
    /// Gets whether the diagonal Romberg sequence met the requested tolerance.
    /// </summary>
    public bool Converged => Status == IterationStatus.Converged;
}
