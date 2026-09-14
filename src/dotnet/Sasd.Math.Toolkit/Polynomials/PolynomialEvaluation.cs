using System.Numerics;

namespace Sasd.Numerics.Polynomials;

/// <summary>
/// Contains a polynomial value and its first two derivatives at the same argument.
/// </summary>
/// <param name="Value">Value of the polynomial.</param>
/// <param name="FirstDerivative">Value of the first derivative.</param>
/// <param name="SecondDerivative">Value of the second derivative.</param>
public readonly record struct PolynomialEvaluation(
    Complex Value,
    Complex FirstDerivative,
    Complex SecondDerivative);
