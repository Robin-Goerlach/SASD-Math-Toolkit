using Sasd.Numerics.Common;

namespace Sasd.Numerics.DifferentialEquations;

public static partial class RungeKutta
{
    /// <summary>
    /// Solves a coupled system of second-order equations with classical RK4 by
    /// converting values and first derivatives into one first-order state vector.
    /// </summary>
    /// <param name="secondDerivatives">
    /// Function returning all second derivatives. Its arguments are <c>x</c>, the
    /// current values <c>[y0, ..., y(m-1)]</c>, and the corresponding first
    /// derivatives <c>[y0', ..., y(m-1)']</c>.
    /// </param>
    /// <param name="x0">Initial independent-variable value.</param>
    /// <param name="initialValues">Initial values for every coupled equation.</param>
    /// <param name="initialFirstDerivatives">Initial first derivatives in the same order as the values.</param>
    /// <param name="xEnd">Requested end point; it must be greater than <paramref name="x0"/>.</param>
    /// <param name="step">Nominal positive RK4 step size. The final step is shortened when necessary.</param>
    /// <returns>Immutable snapshots containing all values and first derivatives at each RK4 point.</returns>
    /// <remarks>
    /// For <c>m</c> equations, the internal first-order state is arranged as
    /// <c>[y0, ..., y(m-1), y0', ..., y(m-1)']</c>. Its derivative is
    /// <c>[y0', ..., y(m-1)', y0'', ..., y(m-1)'']</c>. The public result keeps the
    /// value and derivative groups separate so callers do not have to know this
    /// packing convention.
    /// </remarks>
    public static IReadOnlyList<SecondOrderSystemOdePoint> FourthOrderSecondOrderSystem(
        Func<double, IReadOnlyList<double>, IReadOnlyList<double>, double[]> secondDerivatives,
        double x0,
        IReadOnlyList<double> initialValues,
        IReadOnlyList<double> initialFirstDerivatives,
        double xEnd,
        double step)
    {
        ArgumentNullException.ThrowIfNull(secondDerivatives);
        ArgumentNullException.ThrowIfNull(initialValues);
        ArgumentNullException.ThrowIfNull(initialFirstDerivatives);
        NumericGuard.Finite(x0, nameof(x0));
        NumericGuard.Finite(xEnd, nameof(xEnd));
        NumericGuard.Positive(step, nameof(step));

        if (initialValues.Count == 0)
        {
            throw new ArgumentException("At least one second-order equation is required.", nameof(initialValues));
        }

        if (initialValues.Count != initialFirstDerivatives.Count)
        {
            throw new ArgumentException(
                "Initial values and first derivatives must have the same dimension.",
                nameof(initialFirstDerivatives));
        }

        ValidateFiniteInitialState(initialValues, nameof(initialValues));
        ValidateFiniteInitialState(initialFirstDerivatives, nameof(initialFirstDerivatives));
        if (xEnd <= x0)
        {
            throw new ArgumentException("xEnd must be greater than x0.", nameof(xEnd));
        }

        var dimension = initialValues.Count;
        var packedInitialState = new double[dimension * 2];
        for (var i = 0; i < dimension; i++)
        {
            packedInitialState[i] = initialValues[i];
            packedInitialState[dimension + i] = initialFirstDerivatives[i];
        }

        var systemPoints = FourthOrderSystem(
            (x, packedState) =>
            {
                // Keep the callback API mathematical rather than exposing the packed
                // implementation detail. The small copies also prevent a callback
                // from mutating the RK4 stage state accidentally.
                var values = new double[dimension];
                var firstDerivatives = new double[dimension];
                for (var i = 0; i < dimension; i++)
                {
                    values[i] = packedState[i];
                    firstDerivatives[i] = packedState[dimension + i];
                }

                var accelerations = secondDerivatives(x, values, firstDerivatives)
                    ?? throw new ArithmeticException(
                        "The second-derivative function returned null during RK4 integration.");

                if (accelerations.Length != dimension)
                {
                    throw new ArgumentException(
                        "Second-derivative dimension does not match the number of coupled equations.");
                }

                var packedDerivative = new double[dimension * 2];
                for (var i = 0; i < dimension; i++)
                {
                    packedDerivative[i] = firstDerivatives[i];
                    packedDerivative[dimension + i] = accelerations[i];
                }

                return packedDerivative;
            },
            x0,
            packedInitialState,
            xEnd,
            step);

        var result = new List<SecondOrderSystemOdePoint>(systemPoints.Count);
        foreach (var point in systemPoints)
        {
            var values = new double[dimension];
            var firstDerivatives = new double[dimension];
            for (var i = 0; i < dimension; i++)
            {
                values[i] = point.Y[i];
                firstDerivatives[i] = point.Y[dimension + i];
            }

            result.Add(new SecondOrderSystemOdePoint(point.X, values, firstDerivatives));
        }

        return result;
    }
}
