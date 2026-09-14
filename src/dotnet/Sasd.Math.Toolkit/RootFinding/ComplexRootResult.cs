using System.Numerics;
using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Describes the outcome of a complex root-finding iteration.
/// </summary>
/// <param name="Root">Best root estimate produced by the solver.</param>
/// <param name="FunctionValue">Function value at <paramref name="Root"/>.</param>
/// <param name="Iterations">Number of completed iterations.</param>
/// <param name="Status">Termination status.</param>
/// <param name="Message">Optional diagnostic message.</param>
public sealed record ComplexRootResult(
    Complex Root,
    Complex FunctionValue,
    int Iterations,
    IterationStatus Status,
    string? Message = null)
{
    /// <summary>
    /// Gets whether the algorithm reported convergence.
    /// </summary>
    public bool Converged => Status == IterationStatus.Converged;

    /// <summary>
    /// Gets the absolute residual <c>|f(root)|</c>.
    /// </summary>
    public double Residual => FunctionValue.Magnitude;
}
