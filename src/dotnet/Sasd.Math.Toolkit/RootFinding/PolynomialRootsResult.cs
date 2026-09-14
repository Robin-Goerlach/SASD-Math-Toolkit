using System.Numerics;
using Sasd.Numerics.Common;

namespace Sasd.Numerics.RootFinding;

/// <summary>
/// Describes the result of finding all roots of a polynomial by repeated deflation.
/// </summary>
/// <param name="Roots">Roots found so far. A failed operation may contain a partial set.</param>
/// <param name="Iterations">Total iterations spent in root searches and polishing.</param>
/// <param name="Status">Overall termination status.</param>
/// <param name="MaximumResidual">Largest residual measured against the original polynomial.</param>
/// <param name="Message">Optional diagnostic message.</param>
public sealed record PolynomialRootsResult(
    IReadOnlyList<Complex> Roots,
    int Iterations,
    IterationStatus Status,
    double MaximumResidual,
    string? Message = null)
{
    /// <summary>
    /// Gets whether all requested roots were found.
    /// </summary>
    public bool Converged => Status == IterationStatus.Converged;
}
