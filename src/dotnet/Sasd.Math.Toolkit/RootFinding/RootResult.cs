using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Result returned by a scalar root-finding algorithm.
/// </summary>
public sealed record RootResult(
    double Root,
    double FunctionValue,
    int Iterations,
    IterationStatus Status,
    string? Message = null)
{
    public bool Converged => Status == IterationStatus.Converged;
}
