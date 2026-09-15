using System.Numerics;
using Sasd.Numerics.Transforms;

namespace Sasd.Math.Toolkit.Tests;

public sealed class FourierTransformTests
{
    [Fact]
    public void CompactRealSpectrum_MatchesNonNegativeHalfOfFullSpectrum()
    {
        double[] input = [1.0, -0.5, 2.0, 0.25, -1.5, 3.0, 0.75, -2.0];

        var full = FastFourierTransform.ForwardReal(input);
        var compact = FastFourierTransform.ForwardRealCompact(input);

        Assert.Equal(input.Length, compact.OriginalLength);
        Assert.Equal((input.Length / 2) + 1, compact.BinCount);
        Assert.Equal(compact.BinCount, compact.Bins.Count);

        for (var bin = 0; bin < compact.BinCount; bin++)
        {
            Assert.InRange(Complex.Abs(full[bin] - compact[bin]), 0.0, 1e-12);
            Assert.Equal(bin / (double)input.Length, compact.GetNormalizedFrequency(bin), 12);
        }
    }

    [Fact]
    public void CompactRealSpectrum_RoundTripsRealSignal()
    {
        double[] input = [0.5, 1.5, -2.0, 4.0, 0.25, -0.75, 3.5, -1.0];

        var spectrum = FastFourierTransform.ForwardRealCompact(input);
        var restored = FastFourierTransform.InverseReal(spectrum);

        Assert.Equal(input.Length, restored.Length);
        for (var index = 0; index < input.Length; index++)
        {
            Assert.InRange(System.Math.Abs(input[index] - restored[index]), 0.0, 1e-10);
        }
    }

    [Fact]
    public void CompactRealSpectrum_HandlesEmptyAndSingletonInputs()
    {
        var empty = FastFourierTransform.ForwardRealCompact(Array.Empty<double>());
        Assert.Equal(0, empty.OriginalLength);
        Assert.Equal(0, empty.BinCount);
        Assert.Empty(FastFourierTransform.InverseReal(empty));
        Assert.Throws<InvalidOperationException>(() => empty.GetNormalizedFrequency(0));

        var singleton = FastFourierTransform.ForwardRealCompact([3.25]);
        Assert.Equal(1, singleton.OriginalLength);
        Assert.Equal(1, singleton.BinCount);
        Assert.Equal(new Complex(3.25, 0.0), singleton[0]);
        Assert.Equal(3.25, FastFourierTransform.InverseReal(singleton)[0], 12);
    }

    [Fact]
    public void CompactRealSpectrum_IsDefensiveAndRejectsInvalidInput()
    {
        var spectrum = FastFourierTransform.ForwardRealCompact([1.0, 2.0, 3.0, 4.0]);
        var copy = spectrum.ToArray();
        copy[0] = new Complex(999.0, 999.0);
        Assert.NotEqual(copy[0], spectrum[0]);

        Assert.Throws<ArgumentException>(() =>
            FastFourierTransform.ForwardRealCompact([1.0, 2.0, 3.0]));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastFourierTransform.ForwardRealCompact([1.0, double.NaN, 3.0, 4.0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => spectrum.GetNormalizedFrequency(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => spectrum.GetNormalizedFrequency(spectrum.BinCount));
    }

    [Fact]
    public void ComplexConvolution_MatchesDirectLinearConvolution()
    {
        Complex[] left = [new(1.0, 1.0), new(2.0, -1.0)];
        Complex[] right = [new(3.0, 0.0), new(0.0, -1.0)];
        Complex[] expected = [new(3.0, 3.0), new(7.0, -4.0), new(-1.0, -2.0)];

        var actual = FastFourierTransform.ConvolveComplex(left, right);

        AssertComplexSequence(expected, actual, 1e-10);
    }

    [Fact]
    public void ComplexCrossCorrelation_UsesConjugationAndDocumentedLagOrder()
    {
        Complex[] left = [new(1.0, 1.0), new(2.0, -1.0)];
        Complex[] right = [new(3.0, 0.0), new(0.0, -1.0)];

        // Output indices 0, 1, 2 correspond to lags -1, 0, +1 because right.Count == 2.
        Complex[] expected = [new(-1.0, 1.0), new(4.0, 5.0), new(6.0, -3.0)];

        var actual = FastFourierTransform.CrossCorrelateComplex(left, right);

        AssertComplexSequence(expected, actual, 1e-10);

        // At zero lag the result is the complex inner product sum(left[k] * conj(right[k])).
        var expectedZeroLag = (left[0] * Complex.Conjugate(right[0]))
            + (left[1] * Complex.Conjugate(right[1]));
        Assert.InRange(Complex.Abs(actual[right.Length - 1] - expectedZeroLag), 0.0, 1e-10);
    }

    [Fact]
    public void RealConvolutionAndCorrelation_ShareTheComplexCoreBehavior()
    {
        double[] left = [1.0, 2.0, -1.0];
        double[] right = [0.5, -2.0];

        var realConvolution = FastFourierTransform.ConvolveReal(left, right);
        var complexConvolution = FastFourierTransform.ConvolveComplex(
            left.Select(value => new Complex(value, 0.0)).ToArray(),
            right.Select(value => new Complex(value, 0.0)).ToArray());

        var realCorrelation = FastFourierTransform.CrossCorrelateReal(left, right);
        var complexCorrelation = FastFourierTransform.CrossCorrelateComplex(
            left.Select(value => new Complex(value, 0.0)).ToArray(),
            right.Select(value => new Complex(value, 0.0)).ToArray());

        Assert.Equal(complexConvolution.Length, realConvolution.Length);
        Assert.Equal(complexCorrelation.Length, realCorrelation.Length);

        for (var index = 0; index < realConvolution.Length; index++)
        {
            Assert.InRange(System.Math.Abs(realConvolution[index] - complexConvolution[index].Real), 0.0, 1e-10);
            Assert.InRange(System.Math.Abs(complexConvolution[index].Imaginary), 0.0, 1e-10);
        }

        for (var index = 0; index < realCorrelation.Length; index++)
        {
            Assert.InRange(System.Math.Abs(realCorrelation[index] - complexCorrelation[index].Real), 0.0, 1e-10);
            Assert.InRange(System.Math.Abs(complexCorrelation[index].Imaginary), 0.0, 1e-10);
        }
    }

    [Fact]
    public void ConvolutionAndCorrelation_HandleEmptyAndRejectNonFiniteComplexInput()
    {
        Assert.Empty(FastFourierTransform.ConvolveComplex(Array.Empty<Complex>(), [Complex.One]));
        Assert.Empty(FastFourierTransform.CrossCorrelateComplex([Complex.One], Array.Empty<Complex>()));
        Assert.Empty(FastFourierTransform.ConvolveReal(Array.Empty<double>(), [1.0]));
        Assert.Empty(FastFourierTransform.CrossCorrelateReal([1.0], Array.Empty<double>()));

        Assert.Throws<ArgumentOutOfRangeException>(() => FastFourierTransform.Forward(
            [Complex.One, new Complex(double.NaN, 0.0)]));

        Assert.Throws<ArgumentOutOfRangeException>(() => FastFourierTransform.ConvolveComplex(
            [Complex.One],
            [new Complex(0.0, double.PositiveInfinity)]));

        Assert.Throws<ArgumentOutOfRangeException>(() => FastFourierTransform.CrossCorrelateReal(
            [1.0, 2.0],
            [1.0, double.NaN]));
    }

    private static void AssertComplexSequence(
        IReadOnlyList<Complex> expected,
        IReadOnlyList<Complex> actual,
        double tolerance)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            Assert.InRange(Complex.Abs(expected[index] - actual[index]), 0.0, tolerance);
        }
    }
}
