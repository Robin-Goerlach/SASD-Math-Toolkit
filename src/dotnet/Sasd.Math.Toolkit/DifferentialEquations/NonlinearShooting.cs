using Sasd.Numerics.Common;
using Sasd.Numerics.RootFinding;

namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Nonlinear shooting solver for scalar second-order Dirichlet boundary-value problems.
/// </summary>
/// <remarks>
/// For <c>y'' = g(x, y, y')</c> with values prescribed at both ends, the missing
/// initial slope is treated as a scalar root-finding variable. Each trial slope is
/// integrated with the existing second-order RK4 solver, and the right-boundary
/// residual is driven toward zero with the existing secant root solver.
/// </remarks>
public static class NonlinearShooting
{
    /// <summary>
    /// Solves a nonlinear second-order boundary-value problem by secant shooting.
    /// </summary>
    /// <param name="secondDerivative">Function returning <c>y'' = g(x, y, y')</c>.</param>
    /// <param name="x0">Left interval endpoint.</param>
    /// <param name="leftValue">Required value <c>y(x0)</c>.</param>
    /// <param name="xEnd">Right interval endpoint; it must be greater than <paramref name="x0"/>.</param>
    /// <param name="rightValue">Required value <c>y(xEnd)</c>.</param>
    /// <param name="step">Nominal positive RK4 step used for every shooting trial.</param>
    /// <param name="firstSlopeGuess">First finite estimate for the unknown initial slope.</param>
    /// <param name="secondSlopeGuess">Second finite estimate for the unknown initial slope.</param>
    /// <param name="options">Optional convergence controls for the secant slope search.</param>
    /// <returns>
    /// A result containing the final trajectory, slope estimate, boundary residual and
    /// iterative termination status.
    /// </returns>
    /// <remarks>
    /// The method is intentionally a composition of two existing numerical building
    /// blocks: RK4 integrates each initial-value trial, while the secant solver adjusts
    /// the slope. This keeps one implementation of each algorithm and makes the
    /// boundary-value layer responsible only for the shooting transformation.
    /// </remarks>
    public static NonlinearShootingResult Solve(
        Func<double, double, double, double> secondDerivative,
        double x0,
        double leftValue,
        double xEnd,
        double rightValue,
        double step,
        double firstSlopeGuess,
        double secondSlopeGuess,
        NonlinearShootingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(secondDerivative);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(leftValue, nameof(leftValue));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Finite(rightValue, nameof(rightValue));
        NumericGuard.Positive(step, nameof(step));
        NumericGuard.Finite(firstSlopeGuess, nameof(firstSlopeGuess));
        NumericGuard.Finite(secondSlopeGuess, nameof(secondSlopeGuess));

        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        options ??= new NonlinearShootingOptions();
        options.Validate();

        IReadOnlyList<SecondOrderOdePoint> IntegrateShot(double initialSlope)
        {
            return RungeKutta.FourthOrderSecondOrder(
                secondDerivative,
                x0,
                leftValue,
                initialSlope,
                xEnd,
                step);
        }

        double BoundaryResidual(double initialSlope)
        {
            var trajectory = IntegrateShot(initialSlope);
            var residual = trajectory[^1].Y - rightValue;
            if (!double.IsFinite(residual))
            {
                throw new ArithmeticException("Nonlinear shooting produced a non-finite right-boundary residual.");
            }

            return residual;
        }

        // The unknown initial slope is just a scalar root of the boundary-residual
        // function. Reusing RootSolvers.Secant avoids a second copy of secant logic and
        // keeps its numerical-breakdown semantics consistent across the toolkit.
        var rootResult = RootSolvers.Secant(
            BoundaryResidual,
            firstSlopeGuess,
            secondSlopeGuess,
            new RootFindingOptions(options.BoundaryTolerance, options.MaximumIterations));

        // Reintegrate once with the final slope estimate so the returned trajectory is
        // unambiguous. Avoiding this extra solve is a possible later optimization; the
        // current reference implementation favors simple, auditable control flow.
        var finalTrajectory = IntegrateShot(rootResult.Root);
        var finalResidual = finalTrajectory[^1].Y - rightValue;
        if (!double.IsFinite(finalResidual))
        {
            throw new ArithmeticException("Nonlinear shooting produced a non-finite final boundary residual.");
        }

        var status = rootResult.Status;
        var message = rootResult.Message;

        // RootSolvers.Secant may also stop because successive slope estimates become
        // sufficiently close. For a boundary-value solver, success should additionally
        // mean that the actual boundary condition itself is satisfied.
        if (status == IterationStatus.Converged
            && System.Math.Abs(finalResidual) > options.BoundaryTolerance)
        {
            status = IterationStatus.NumericalBreakdown;
            message = "The secant slope update stagnated before the requested right-boundary tolerance was satisfied.";
        }

        return new NonlinearShootingResult(
            finalTrajectory,
            rootResult.Root,
            finalResidual,
            rootResult.Iterations,
            status,
            message);
    }
}
