using System.Collections.ObjectModel;
using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>Immutable compact spectrum produced from a real-valued radix-2 FFT input sequence.</summary>
public sealed class RealFftSpectrum
{
    private readonly Complex[] _bins;
    private readonly ReadOnlyCollection<Complex> _readOnlyBins;

    internal RealFftSpectrum(int originalLength, IReadOnlyList<Complex> bins)
        : this(originalLength, CopyBins(bins))
    {
    }

    private RealFftSpectrum(int originalLength, Complex[] ownedBins)
    {
        ArgumentNullException.ThrowIfNull(ownedBins);
        ValidateShape(originalLength, ownedBins.Length);
        ValidateFiniteBins(ownedBins);

        OriginalLength = originalLength;
        _bins = ownedBins;
        _readOnlyBins = Array.AsReadOnly(_bins);
    }

    /// <summary>
    /// Creates a spectrum by taking ownership of a freshly allocated internal array. This avoids
    /// a redundant full-bin copy on the FFT hot path while preserving public immutability.
    /// </summary>
    internal static RealFftSpectrum FromOwnedBins(int originalLength, Complex[] bins) =>
        new(originalLength, bins);

    public int OriginalLength { get; }
    public int BinCount => _bins.Length;
    public IReadOnlyList<Complex> Bins => _readOnlyBins;
    public Complex this[int binIndex] => _bins[binIndex];

    /// <summary>Gets the bin frequency in cycles per sample.</summary>
    public double GetNormalizedFrequency(int binIndex)
    {
        if (OriginalLength == 0)
        {
            throw new InvalidOperationException("An empty spectrum has no frequency bins.");
        }

        if ((uint)binIndex >= (uint)_bins.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(binIndex));
        }

        return binIndex / (double)OriginalLength;
    }

    /// <summary>Returns a defensive copy of the compact bins.</summary>
    public Complex[] ToArray() => (Complex[])_bins.Clone();

    private static Complex[] CopyBins(IReadOnlyList<Complex> bins)
    {
        ArgumentNullException.ThrowIfNull(bins);
        return bins.ToArray();
    }

    private static void ValidateShape(int originalLength, int binCount)
    {
        if (originalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalLength));
        }

        if (originalLength == 0)
        {
            if (binCount != 0)
            {
                throw new ArgumentException("An empty input must have an empty compact spectrum.", nameof(binCount));
            }

            return;
        }

        if (!IsPowerOfTwo(originalLength))
        {
            throw new ArgumentException("A compact real FFT spectrum must describe a radix-2 input length.", nameof(originalLength));
        }

        var expectedBinCount = (originalLength / 2) + 1;
        if (binCount != expectedBinCount)
        {
            throw new ArgumentException($"A real FFT of length {originalLength} requires exactly {expectedBinCount} compact bins.", nameof(binCount));
        }
    }

    private static void ValidateFiniteBins(IReadOnlyList<Complex> bins)
    {
        for (var index = 0; index < bins.Count; index++)
        {
            if (!double.IsFinite(bins[index].Real) || !double.IsFinite(bins[index].Imaginary))
            {
                throw new ArgumentOutOfRangeException(nameof(bins), "Spectrum bins must be finite complex values.");
            }
        }
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
