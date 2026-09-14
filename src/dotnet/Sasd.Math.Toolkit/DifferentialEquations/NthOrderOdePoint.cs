namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Represents one solution state of a scalar ordinary differential equation of arbitrary order.
/// </summary>
/// <remarks>
/// The state follows the conventional companion-system ordering:
/// <c>[y, y', y'', ..., y^(n-1)]</c>. The stored values are copied at construction time so a
/// returned trajectory cannot be changed by mutating an array that was used internally by a solver.
/// </remarks>
public sealed class NthOrderOdePoint
{
    private readonly IReadOnlyList<double> _state;

    /// <summary>
    /// Initializes an immutable snapshot of an nth-order ODE state.
    /// </summary>
    /// <param name="x">Independent-variable value.</param>
    /// <param name="state">Values <c>[y, y', ..., y^(n-1)]</c>.</param>
    public NthOrderOdePoint(double x, IReadOnlyList<double> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The independent-variable value must be finite.");
        }

        if (state.Count == 0)
        {
            throw new ArgumentException("The derivative state must contain at least y itself.", nameof(state));
        }

        var snapshot = new double[state.Count];
        for (var i = 0; i < snapshot.Length; i++)
        {
            if (!double.IsFinite(state[i]))
            {
                throw new ArgumentOutOfRangeException(nameof(state), "All derivative-state values must be finite.");
            }

            snapshot[i] = state[i];
        }

        X = x;
        _state = Array.AsReadOnly(snapshot);
    }

    /// <summary>Gets the independent-variable value.</summary>
    public double X { get; }

    /// <summary>
    /// Gets the order of the represented scalar ODE, which is also the number of state values.
    /// </summary>
    public int Order => _state.Count;

    /// <summary>Gets the solution value <c>y</c>.</summary>
    public double Y => _state[0];

    /// <summary>
    /// Gets the read-only state <c>[y, y', ..., y^(n-1)]</c>.
    /// </summary>
    public IReadOnlyList<double> State => _state;

    /// <summary>
    /// Returns a derivative by mathematical order. Order zero returns <c>y</c>, order one returns
    /// <c>y'</c>, and so on up to <c>n-1</c>.
    /// </summary>
    public double GetDerivative(int derivativeOrder)
    {
        if (derivativeOrder < 0 || derivativeOrder >= _state.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(derivativeOrder),
                $"Derivative order must be between 0 and {_state.Count - 1} for this state.");
        }

        return _state[derivativeOrder];
    }
}
