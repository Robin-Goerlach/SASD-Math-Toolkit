namespace Sasd.Numerics.Common;

/// <summary>
/// Shared numerical defaults used by the dependency-free reference implementations.
/// </summary>
/// <remarks>
/// These constants are defaults and guard thresholds, not universal statements about numerical
/// accuracy. Public algorithms expose explicit tolerances/options where the caller is expected to
/// choose a scale appropriate to the mathematical problem. The toolkit intentionally avoids a
/// mutable process-wide tolerance because one global value cannot correctly describe all domains.
/// </remarks>
public static class NumericConstants
{
    /// <summary>
    /// General-purpose default convergence tolerance used by APIs whose scale makes this value sensible.
    /// </summary>
    /// <remarks>
    /// A returned value being within this tolerance of an internal convergence criterion does not
    /// imply twelve correct decimal digits in the mathematical solution. Conditioning and the exact
    /// residual/error definition remain relevant.
    /// </remarks>
    public const double DefaultTolerance = 1e-12;

    /// <summary>
    /// Small absolute guard threshold used to detect divisors, pivots or norms that are numerically negligible.
    /// </summary>
    /// <remarks>
    /// This is deliberately not named "epsilon": it is neither <see cref="double.Epsilon"/> nor a
    /// replacement for problem-scaled equality tests. Algorithms that expose a public pivot or
    /// singularity tolerance allow callers to override this guard when the problem scale requires it.
    /// </remarks>
    public const double NearlyZero = 1e-15;

    /// <summary>
    /// Default safety limit for iterative algorithms that use the shared iteration convention.
    /// </summary>
    public const int DefaultMaximumIterations = 100;
}
