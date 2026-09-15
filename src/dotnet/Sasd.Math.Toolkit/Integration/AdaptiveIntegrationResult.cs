namespace Sasd.Numerics.Integration;

/// <summary>
/// Diagnostic result returned by an adaptive quadrature routine.
/// </summary>
/// <param name="Value">Best integral estimate produced by the refinement process.</param>
/// <param name="EstimatedError">Accumulated local error indicator. This can be positive infinity when floating-point resolution prevents further subdivision.</param>
/// <param name="FunctionEvaluations">Number of calls made to the supplied integrand.</param>
/// <param name="AcceptedPanels">Number of final panels contributing to <paramref name="Value"/>.</param>
/// <param name="Status">Reason why adaptive refinement stopped.</param>
public sealed record AdaptiveIntegrationResult(
    double Value,
    double EstimatedError,
    int FunctionEvaluations,
    int AcceptedPanels,
    AdaptiveIntegrationStatus Status)
{
    /// <summary>
    /// Gets whether all accepted panels satisfied the requested local error criterion.
    /// </summary>
    public bool Converged => Status == AdaptiveIntegrationStatus.Converged;
}
