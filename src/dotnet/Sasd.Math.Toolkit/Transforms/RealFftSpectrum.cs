using System.Collections.ObjectModel;
using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>
/// Immutable compact spectrum produced from a real-valued radix-2 FFT input sequence.
/// </summary>
/// <remarks>
/// For a real input sequence of length <c>N</c>, the negative-frequency half of the
/// discrete Fourier spectrum is the complex conjugate of the positive-frequency half.
/// Therefore only bins <c>0..N/2</c> need to be stored. The original input length is
/// retained explicitly so the full Hermitian spectrum can be reconstructed without
/// asking callers to manage packing conventions themselves.
/// </remarks>
public sealed class RealFftSpectrum
{
    private readonly Complex[] _bins;
    private readonly ReadOnlyCollection<Complex> _readOnlyBins;

    internal RealFftSpectrum(int originalLength, IReadOnlyList<Complex> bins)
    {
        ArgumentNullException.ThrowIfNull(bins);

        if (originalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(originalLength));
        }

        if (originalLength == 0)
        {
            if (bins.Count != 0)
            {
                throw new ArgumentException("An empty input must have an empty compact spectrum.", nameof(bins));
            }

            OriginalLength = 0;
            _bins = [];
            _readOnlyBins = Array.AsReadOnly(_bins);
            return;
        }

        if (!IsPowerOfTwo(originalLength))
        {
            throw new ArgumentException(
                "A compact real FFT spectrum must describe a radix-2 input length.",
                nameof(originalLength));
        }

        var expectedBinCount = (originalLength / 2) + 1;
        if (bins.Count != expectedBinCount)
        {
            throw new ArgumentException(
                $"A real FFT of length {originalLength} requires exactly {expectedBinCount} compact bins.",
                nameof(bins));
        }

        _bins = bins.ToArray();
        for (var index = 0; index < _bins.Length; index++)
        {
            if (!double.IsFinite(_bins[index].Real) || !double.IsFinite(_bins[index].Imaginary))
            {
                throw new ArgumentOutOfRangeException(nameof(bins), "Spectrum bins must be finite complex values.");
            }
        }

        OriginalLength = originalLength;
        _readOnlyBins = Array.AsReadOnly(_bins);
    }

    /// <summary>Gets the number of real samples from which the spectrum was produced.</summary>
    public int OriginalLength { get; }

    /// <summary>Gets the number of stored non-negative-frequency bins.</summary>
    public int BinCount => _bins.Length;

    /// <summary>
    /// Gets an immutable view of the stored bins from DC through the Nyquist bin.
    /// </summary>
    public IReadOnlyList<Complex> Bins => _readOnlyBins;

    /// <summary>Gets a stored spectrum bin by index.</summary>
    public Complex this[int binIndex] => _bins[binIndex];

    /// <summary>
    /// Gets the bin frequency in cycles per sample. Multiply this value by the sample rate
    /// to obtain the physical frequency in cycles per second (Hz).
    /// </summary>
    public double GetNormalizedFrequency(int binIndex)
    {
        if (OriginalLength == 0)
        {
            throw new InvalidOperationException("An empty spectrum has no frequency bins.");
        }

        if (binIndex < 0 || binIndex >= _bins.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(binIndex));
        }

        return binIndex / (double)OriginalLength;
    }

    /// <summary>
    /// Returns a defensive copy of the compact bins for callers that need an array.
    /// </summary>
    public Complex[] ToArray() => _bins.ToArray();

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;
}
