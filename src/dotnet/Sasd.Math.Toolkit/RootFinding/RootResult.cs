using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Describes the outcome of a scalar root-finding iteration.
/// </summary>
/// <param name="Root">Best root estimate produced by the solver.</param>
/// <param name="FunctionValue">Function value evaluated at <paramref name="Root"/>.</param>
/// <param name="Iterations">Number of completed iterations.</param>
/// <param name="Status">Reason why the solver stopped.</param>
/// <param name="Message">Optional diagnostic message.</param>
public sealed record RootResult(
    double Root,
    double FunctionValue,
    int Iterations,
    IterationStatus Status,
    string? Message = null)
{
    /// <summary>
    /// Gets whether the algorithm reported convergence.
    /// </summary>
    public bool Converged => Status == IterationStatus.Converged;

    /// <summary>
    /// Gets the absolute scalar residual <c>|f(root)|</c>.
    /// </summary>
    /// <remarks>
    /// A converged status is useful control-flow information, while the residual gives the
    /// caller a problem-facing quality measure that can be recorded or checked separately.
    /// </remarks>
    public double Residual => System.Math.Abs(FunctionValue);
}
