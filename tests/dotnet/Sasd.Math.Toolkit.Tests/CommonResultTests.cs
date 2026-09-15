using Sasd.Numerics.Common;

namespace Sasd.Math.Toolkit.Tests;

public sealed class CommonResultTests
{
    [Fact]
    public void IterativeResult_SeparatesConvergenceFromResidualAvailability()
    {
        var converged = new IterativeResult<double>(
            1.0,
            4,
            IterationStatus.Converged,
            Residual: 1e-14);

        var iterationLimited = new IterativeResult<double>(
            0.99,
            10,
            IterationStatus.MaximumIterationsReached,
            Residual: 2e-5,
            Message: "Iteration limit reached.");

        var breakdown = new IterativeResult<double>(
            double.NaN,
            2,
            IterationStatus.NumericalBreakdown,
            Message: "No meaningful residual was available.");

        Assert.True(converged.Converged);
        Assert.True(converged.HasFiniteResidual);

        // A useful residual can exist even when the algorithm did not converge.
        // Status and residual answer different diagnostic questions.
        Assert.False(iterationLimited.Converged);
        Assert.True(iterationLimited.HasFiniteResidual);
        Assert.Equal(2e-5, iterationLimited.Residual);

        // Numerical breakdown may prevent the algorithm from producing a residual at all.
        Assert.False(breakdown.Converged);
        Assert.False(breakdown.HasFiniteResidual);
        Assert.True(double.IsNaN(breakdown.Residual));
    }
}
