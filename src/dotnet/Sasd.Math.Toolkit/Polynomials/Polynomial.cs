using System.Numerics;

namespace Sasd.Numerics.Polynomials;

/// <summary>
/// Immutable polynomial with coefficients stored in descending order of power.
/// </summary>
/// <remarks>
/// A coefficient vector <c>[a0, a1, ..., an]</c> represents
/// <c>a0*x^n + a1*x^(n-1) + ... + an</c>. Complex coefficients are supported so
/// the same type can be reused by real and complex root-finding algorithms.
/// </remarks>
public sealed class Polynomial
{
    private readonly Complex[] _coefficients;
    private readonly IReadOnlyList<Complex> _coefficientView;

    /// <summary>
    /// Creates a polynomial from real coefficients in descending power order.
    /// </summary>
    /// <param name="coefficients">Finite real coefficients.</param>
    public Polynomial(IEnumerable<double> coefficients)
        : this(ConvertRealCoefficients(coefficients))
    {
    }

    /// <summary>
    /// Creates a polynomial from complex coefficients in descending power order.
    /// </summary>
    /// <param name="coefficients">Finite complex coefficients.</param>
    public Polynomial(IEnumerable<Complex> coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);

        var input = coefficients.ToArray();
        if (input.Length == 0)
        {
            throw new ArgumentException("A polynomial requires at least one coefficient.", nameof(coefficients));
        }

        foreach (var coefficient in input)
        {
            EnsureFinite(coefficient, nameof(coefficients));
        }

        // Canonicalize leading exact zeros. We deliberately do not use a tolerance here:
        // silently discarding a very small but intentional leading coefficient would change
        // the mathematical degree and can be worse than retaining an ill-scaled polynomial.
        var firstSignificant = 0;
        while (firstSignificant < input.Length - 1 && input[firstSignificant] == Complex.Zero)
        {
            firstSignificant++;
        }

        _coefficients = input[firstSignificant..];
        _coefficientView = Array.AsReadOnly(_coefficients);
    }

    /// <summary>
    /// Gets the polynomial degree. The canonical zero polynomial has degree zero.
    /// </summary>
    public int Degree => _coefficients.Length - 1;

    /// <summary>
    /// Gets the leading coefficient.
    /// </summary>
    public Complex LeadingCoefficient => _coefficients[0];

    /// <summary>
    /// Gets a read-only view of the coefficients in descending power order.
    /// </summary>
    public IReadOnlyList<Complex> Coefficients => _coefficientView;

    /// <summary>
    /// Gets whether this object represents the zero polynomial.
    /// </summary>
    public bool IsZero => Degree == 0 && _coefficients[0] == Complex.Zero;

    /// <summary>
    /// Evaluates the polynomial at <paramref name="x"/> with Horner's method.
    /// </summary>
    public Complex Evaluate(Complex x)
    {
        EnsureFinite(x, nameof(x));

        var value = _coefficients[0];
        for (var index = 1; index < _coefficients.Length; index++)
        {
            value = (value * x) + _coefficients[index];
        }

        return value;
    }

    /// <summary>
    /// Evaluates the polynomial and its first two derivatives in one extended-Horner pass.
    /// </summary>
    /// <remarks>
    /// Sharing one pass is useful for Newton-Horner and Laguerre iterations and avoids
    /// constructing derivative polynomials merely to evaluate them at one point.
    /// </remarks>
    public PolynomialEvaluation EvaluateWithDerivatives(Complex x)
    {
        EnsureFinite(x, nameof(x));

        var value = _coefficients[0];
        var firstDerivative = Complex.Zero;
        var secondDerivativeAccumulator = Complex.Zero;

        for (var index = 1; index < _coefficients.Length; index++)
        {
            secondDerivativeAccumulator = (secondDerivativeAccumulator * x) + firstDerivative;
            firstDerivative = (firstDerivative * x) + value;
            value = (value * x) + _coefficients[index];
        }

        return new PolynomialEvaluation(
            value,
            firstDerivative,
            2.0 * secondDerivativeAccumulator);
    }

    /// <summary>
    /// Creates the formal first derivative.
    /// </summary>
    public Polynomial Derivative()
    {
        if (Degree == 0)
        {
            return new Polynomial(new[] { Complex.Zero });
        }

        var derivative = new Complex[Degree];
        for (var index = 0; index < Degree; index++)
        {
            derivative[index] = _coefficients[index] * (Degree - index);
        }

        return new Polynomial(derivative);
    }

    /// <summary>
    /// Divides this polynomial by <c>(x - root)</c> using synthetic division.
    /// </summary>
    /// <param name="root">Root/factor value to remove.</param>
    /// <returns>The quotient and the numerical remainder.</returns>
    public PolynomialDeflationResult Deflate(Complex root)
    {
        EnsureFinite(root, nameof(root));

        if (Degree < 1)
        {
            throw new InvalidOperationException("A constant polynomial cannot be deflated.");
        }

        var quotientCoefficients = new Complex[Degree];
        quotientCoefficients[0] = _coefficients[0];

        for (var index = 1; index < quotientCoefficients.Length; index++)
        {
            quotientCoefficients[index] = _coefficients[index] + (root * quotientCoefficients[index - 1]);
        }

        var remainder = _coefficients[^1] + (root * quotientCoefficients[^1]);
        return new PolynomialDeflationResult(new Polynomial(quotientCoefficients), remainder);
    }

    private static IEnumerable<Complex> ConvertRealCoefficients(IEnumerable<double> coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);

        foreach (var coefficient in coefficients)
        {
            if (!double.IsFinite(coefficient))
            {
                throw new ArgumentOutOfRangeException(nameof(coefficients), "Polynomial coefficients must be finite.");
            }

            yield return new Complex(coefficient, 0.0);
        }
    }

    private static void EnsureFinite(Complex value, string parameterName)
    {
        if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Complex values must have finite real and imaginary parts.");
        }
    }
}
