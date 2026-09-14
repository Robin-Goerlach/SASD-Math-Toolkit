using System.Numerics;

namespace Sasd.Numerics.Polynomials;

/// <summary>
/// Result of synthetic division by <c>(x - root)</c>.
/// </summary>
/// <param name="Quotient">Polynomial quotient after removing one linear factor.</param>
/// <param name="Remainder">Synthetic-division remainder. For an exact root this is zero.</param>
public sealed record PolynomialDeflationResult(
    Polynomial Quotient,
    Complex Remainder);
