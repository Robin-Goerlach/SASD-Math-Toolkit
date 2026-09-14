using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Configuration for adaptive Runge-Kutta-Fehlberg 4(5) integration.
/// </summary>
public sealed class RungeKuttaFehlbergOptions
{
    /// <summary>Gets or initializes the first trial step size.</summary>
    public double InitialStep { get; init; } = 0.1;

    /// <summary>Gets or initializes the smallest ordinary adaptive step size.</summary>
    public double MinimumStep { get; init; } = 1e-8;

    /// <summary>Gets or initializes the largest adaptive step size.</summary>
    public double MaximumStep { get; init; } = 1.0;

    /// <summary>Gets or initializes the absolute component of the local-error tolerance.</summary>
    public double AbsoluteTolerance { get; init; } = 1e-8;

    /// <summary>Gets or initializes the relative component of the local-error tolerance.</summary>
    public double RelativeTolerance { get; init; } = 1e-8;

    /// <summary>Gets or initializes the maximum number of accepted plus rejected trial steps.</summary>
    public int MaximumStepAttempts { get; init; } = 100_000;

    /// <summary>
    /// Gets or initializes the safety multiplier applied to the theoretically estimated
    /// next step size. Values below one deliberately leave margin for the next trial.
    /// </summary>
    public double SafetyFactor { get; init; } = 0.9;

    /// <summary>Gets or initializes the strongest permitted reduction factor after one trial.</summary>
    public double MinimumScaleFactor { get; init; } = 0.1;

    /// <summary>Gets or initializes the strongest permitted growth factor after one trial.</summary>
    public double MaximumScaleFactor { get; init; } = 5.0;

    internal void Validate()
    {
        NumericGuard.Positive(InitialStep, nameof(InitialStep));
        NumericGuard.Positive(MinimumStep, nameof(MinimumStep));
        NumericGuard.Positive(MaximumStep, nameof(MaximumStep));
        NumericGuard.Positive(AbsoluteTolerance, nameof(AbsoluteTolerance));
        NumericGuard.Positive(RelativeTolerance, nameof(RelativeTolerance));
        NumericGuard.Positive(MaximumStepAttempts, nameof(MaximumStepAttempts));
        NumericGuard.Positive(SafetyFactor, nameof(SafetyFactor));
        NumericGuard.Positive(MinimumScaleFactor, nameof(MinimumScaleFactor));
        NumericGuard.Positive(MaximumScaleFactor, nameof(MaximumScaleFactor));

        if (MinimumStep > InitialStep || InitialStep > MaximumStep)
        {
            throw new ArgumentException("Step sizes must satisfy MinimumStep <= InitialStep <= MaximumStep.");
        }

        if (SafetyFactor > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(SafetyFactor), "SafetyFactor must not exceed one.");
        }

        if (MinimumScaleFactor > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumScaleFactor), "MinimumScaleFactor must not exceed one.");
        }

        if (MaximumScaleFactor < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumScaleFactor), "MaximumScaleFactor must be at least one.");
        }

        if (MinimumScaleFactor > MaximumScaleFactor)
        {
            throw new ArgumentException("MinimumScaleFactor must not exceed MaximumScaleFactor.");
        }
    }
}
