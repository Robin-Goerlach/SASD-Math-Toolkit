namespace Sasd.Numerics.Common;

/// <summary>
/// Shared numerical defaults. Algorithms expose explicit options so callers can override them.
/// </summary>
public static class NumericConstants
{
    public const double DefaultTolerance = 1e-12;
    public const double NearlyZero = 1e-15;
    public const int DefaultMaximumIterations = 100;
}
