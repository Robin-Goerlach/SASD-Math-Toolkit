namespace Sasd.Numerics.Common;

/// <summary>
/// Standard result envelope for iterative algorithms.
/// </summary>
/// <typeparam name="T">Type of the calculated value.</typeparam>
public sealed record IterativeResult<T>(
    T Value,
    int Iterations,
    IterationStatus Status,
    double Residual = double.NaN,
    string? Message = null)
{
    public bool Converged => Status == IterationStatus.Converged;
}
