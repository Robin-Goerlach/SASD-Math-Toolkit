namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// Represents one immutable solution state of a coupled second-order ODE system.
/// </summary>
public sealed class SecondOrderSystemOdePoint
{
    private readonly IReadOnlyList<double> _values;
    private readonly IReadOnlyList<double> _firstDerivatives;

    /// <summary>
    /// Creates a snapshot of the values and first derivatives at one independent-variable value.
    /// </summary>
    public SecondOrderSystemOdePoint(
        double x,
        IReadOnlyList<double> values,
        IReadOnlyList<double> firstDerivatives)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(firstDerivatives);
        if (!double.IsFinite(x))
        {
            throw new ArgumentOutOfRangeException(nameof(x), "The independent-variable value must be finite.");
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("At least one equation value is required.", nameof(values));
        }

        if (values.Count != firstDerivatives.Count)
        {
            throw new ArgumentException(
                "Values and first derivatives must have the same dimension.",
                nameof(firstDerivatives));
        }

        var valueSnapshot = new double[values.Count];
        var derivativeSnapshot = new double[firstDerivatives.Count];
        for (var i = 0; i < values.Count; i++)
        {
            if (!double.IsFinite(values[i]) || !double.IsFinite(firstDerivatives[i]))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(values),
                    "All values and first derivatives must be finite.");
            }

            valueSnapshot[i] = values[i];
            derivativeSnapshot[i] = firstDerivatives[i];
        }

        X = x;
        _values = Array.AsReadOnly(valueSnapshot);
        _firstDerivatives = Array.AsReadOnly(derivativeSnapshot);
    }

    /// <summary>Gets the independent-variable value.</summary>
    public double X { get; }

    /// <summary>Gets the number of coupled second-order equations.</summary>
    public int Dimension => _values.Count;

    /// <summary>Gets the read-only vector of current equation values.</summary>
    public IReadOnlyList<double> Values => _values;

    /// <summary>Gets the read-only vector of current first derivatives.</summary>
    public IReadOnlyList<double> FirstDerivatives => _firstDerivatives;

    /// <summary>Returns one equation value by zero-based equation index.</summary>
    public double GetValue(int equationIndex)
    {
        ValidateEquationIndex(equationIndex);
        return _values[equationIndex];
    }

    /// <summary>Returns one first derivative by zero-based equation index.</summary>
    public double GetFirstDerivative(int equationIndex)
    {
        ValidateEquationIndex(equationIndex);
        return _firstDerivatives[equationIndex];
    }

    private void ValidateEquationIndex(int equationIndex)
    {
        if (equationIndex < 0 || equationIndex >= Dimension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(equationIndex),
                $"Equation index must be between 0 and {Dimension - 1}.");
        }
    }
}
