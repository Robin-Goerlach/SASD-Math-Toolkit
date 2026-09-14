namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Describes how an adaptive ordinary-differential-equation integration terminated.
/// </summary>
public enum AdaptiveOdeStatus
{
    /// <summary>The requested end point was reached successfully.</summary>
    Completed = 0,

    /// <summary>The configured maximum number of attempted steps was exhausted.</summary>
    MaximumStepAttemptsReached = 1,

    /// <summary>The requested error tolerance could not be met without stepping below the configured minimum.</summary>
    MinimumStepSizeReached = 2,

    /// <summary>A non-finite intermediate value prevented a meaningful continuation.</summary>
    NumericalBreakdown = 3
}
