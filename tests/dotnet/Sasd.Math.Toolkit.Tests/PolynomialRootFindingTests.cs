using System.Numerics;
using Sasd.Numerics.Common;
using Sasd.Numerics.Polynomials;
using Sasd.Numerics.RootFinding;

namespace Sasd.Math.Toolkit.Tests;

public sealed class PolynomialRootFindingTests
{
    [Fact]
    public void ExtendedHorner_EvaluatesValueAndDerivativesTogether()
    {
        var polynomial = new Polynomial(new[] { 1.0, -6.0, 11.0, -6.0 });

        var evaluation = polynomial.EvaluateWithDerivatives(new Complex(2.0, 0.0));

        AssertComplexNear(Complex.Zero, evaluation.Value, 1e-14);
        AssertComplexNear(new Complex(-1.0, 0.0), evaluation.FirstDerivative, 1e-14);
        AssertComplexNear(Complex.Zero, evaluation.SecondDerivative, 1e-14);
    }

    [Fact]
    public void Deflate_RemovesKnownLinearFactor()
    {
        var polynomial = new Polynomial(new[] { 1.0, -6.0, 11.0, -6.0 });

        var deflated = polynomial.Deflate(new Complex(2.0, 0.0));

        AssertComplexNear(Complex.Zero, deflated.Remainder, 1e-14);
        Assert.Equal(2, deflated.Quotient.Degree);
        AssertComplexNear(new Complex(1.0, 0.0), deflated.Quotient.Coefficients[0], 1e-14);
        AssertComplexNear(new Complex(-4.0, 0.0), deflated.Quotient.Coefficients[1], 1e-14);
        AssertComplexNear(new Complex(3.0, 0.0), deflated.Quotient.Coefficients[2], 1e-14);
    }

    [Fact]
    public void NewtonHorner_FindsPolynomialRoot()
    {
        var polynomial = new Polynomial(new[] { 1.0, 0.0, -2.0, -5.0 });

        var result = PolynomialRootSolvers.NewtonHorner(polynomial, new Complex(2.0, 0.0));

        Assert.True(result.Converged);
        Assert.InRange(result.Root.Real, 2.0945514815 - 1e-10, 2.0945514815 + 1e-10);
        Assert.InRange(System.Math.Abs(result.Root.Imaginary), 0.0, 1e-12);
        Assert.InRange(result.Residual, 0.0, 1e-9);
    }

    [Fact]
    public void Muller_CanLeaveRealAxisAndFindImaginaryRoot()
    {
        static Complex Function(Complex x) => (x * x) + Complex.One;

        var result = ComplexRootSolvers.Muller(
            Function,
            Complex.Zero,
            Complex.One,
            new Complex(2.0, 0.0));

        Assert.True(result.Converged);
        Assert.InRange(result.Residual, 0.0, 1e-10);
        Assert.InRange(System.Math.Abs(result.Root.Real), 0.0, 1e-10);
        Assert.InRange(System.Math.Abs(System.Math.Abs(result.Root.Imaginary) - 1.0), 0.0, 1e-10);
    }

    [Fact]
    public void Laguerre_FindsComplexRootOfQuartic()
    {
        var polynomial = new Polynomial(new[] { 1.0, 0.0, 0.0, 0.0, 1.0 });

        var result = PolynomialRootSolvers.Laguerre(polynomial, Complex.One);

        Assert.True(result.Converged);
        Assert.InRange(result.Residual, 0.0, 1e-9);
        Assert.InRange(System.Math.Abs(result.Root.Magnitude - 1.0), 0.0, 1e-9);
    }

    [Fact]
    public void FindAllRootsLaguerre_FindsRealAndComplexRootsWithDeflation()
    {
        var polynomial = new Polynomial(new[] { 1.0, 0.0, 0.0, 0.0, -1.0 });

        var result = PolynomialRootSolvers.FindAllRootsLaguerre(polynomial);

        Assert.Equal(IterationStatus.Converged, result.Status);
        Assert.Equal(4, result.Roots.Count);
        Assert.InRange(result.MaximumResidual, 0.0, 1e-8);

        var expected = new[]
        {
            new Complex(-1.0, 0.0),
            new Complex(0.0, -1.0),
            new Complex(0.0, 1.0),
            new Complex(1.0, 0.0)
        };

        foreach (var expectedRoot in expected)
        {
            Assert.Contains(result.Roots, actual => (actual - expectedRoot).Magnitude <= 1e-8);
        }
    }

    [Fact]
    public void PolynomialRootSolvers_RejectConstantPolynomial()
    {
        var polynomial = new Polynomial(new[] { 42.0 });

        Assert.Throws<ArgumentException>(() =>
            PolynomialRootSolvers.NewtonHorner(polynomial, Complex.Zero));
    }

    private static void AssertComplexNear(Complex expected, Complex actual, double tolerance)
    {
        Assert.InRange((actual - expected).Magnitude, 0.0, tolerance);
    }
}
