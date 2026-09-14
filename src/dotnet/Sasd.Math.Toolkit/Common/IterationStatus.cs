namespace Sasd.Numerics.Common;

/// <summary>
/// Describes how an iterative numerical algorithm terminated.
/// </summary>
public enum IterationStatus
{
    Converged = 0,
    MaximumIterationsReached = 1,
    InvalidInput = 2,
    NumericalBreakdown = 3,
    NotBracketed = 4
}
