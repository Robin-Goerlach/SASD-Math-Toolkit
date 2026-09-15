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
}
