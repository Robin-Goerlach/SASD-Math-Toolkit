namespace Sasd.Numerics.Common;

/// <summary>
/// Describes how an iterative numerical algorithm terminated.
/// </summary>
/// <remarks>
/// Termination status is deliberately separate from the numerical value returned by an
/// algorithm. A solver can produce a useful best-so-far approximation without satisfying
/// its convergence criterion, so callers should inspect both the status and any available
/// residual or error diagnostic before accepting a result.
/// </remarks>
public enum IterationStatus
{
    /// <summary>
    /// The algorithm satisfied its documented convergence criterion.
    /// </summary>
    /// <remarks>
    /// Convergence does not imply exactness. The reported residual, the conditioning of the
    /// mathematical problem and any independent reference information still determine how much
    /// confidence should be placed in the returned approximation.
    /// </remarks>
    Converged = 0,

    /// <summary>
    /// The configured iteration limit was reached before the convergence criterion was met.
    /// </summary>
    /// <remarks>
    /// The result object may still contain the best approximation and residual available at the
    /// limit. Callers should not silently treat that approximation as converged.
    /// </remarks>
    MaximumIterationsReached = 1,

    /// <summary>
    /// The supplied input did not satisfy the algorithm contract.
    /// </summary>
    /// <remarks>
    /// The current C#/.NET implementation normally reports public input-contract violations with
    /// <see cref="ArgumentException"/> or <see cref="ArgumentOutOfRangeException"/> before an
    /// iteration starts. This status remains part of the shared result vocabulary for APIs or
    /// future language implementations that intentionally choose a result-oriented validation path.
    /// </remarks>
    InvalidInput = 2,

    /// <summary>
    /// Iteration could not continue meaningfully because of a numerical breakdown.
    /// </summary>
    /// <remarks>
    /// Typical causes include a zero or numerically negligible divisor, a non-finite iterate,
    /// overflow in an intermediate calculation or another loss of a required numerical invariant.
    /// </remarks>
    NumericalBreakdown = 3,

    /// <summary>
    /// A bracketing root solver was given an interval that does not bracket a sign change.
    /// </summary>
    /// <remarks>
    /// This is an expected mathematical outcome for an otherwise valid interval, not a programmer
    /// error. It is therefore reported as a result status rather than as an exception.
    /// </remarks>
    NotBracketed = 4
}
