using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>
/// Dependency-free radix-2 Cooley-Tukey FFT plus convolution and cross-correlation helpers.
/// </summary>
/// <remarks>
/// The forward transform is unscaled. The inverse transform divides every result by the
/// sequence length, so <c>Inverse(Forward(x))</c> reconstructs <c>x</c> within floating-point
/// round-off. The implementation intentionally favors clarity over micro-optimization.
/// </remarks>
public static partial class FastFourierTransform
{
    /// <summary>Computes the full complex radix-2 discrete Fourier spectrum.</summary>
    public static Complex[] Forward(IReadOnlyList<Complex> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateFiniteComplexInput(input, nameof(input));
        return Transform(input, inverse: false);
    }

    /// <summary>Computes the normalized inverse complex radix-2 transform.</summary>
    public static Complex[] Inverse(IReadOnlyList<Complex> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateFiniteComplexInput(input, nameof(input));
        return Transform(input, inverse: true);
    }

    /// <summary>
    /// Computes the full complex spectrum of a real-valued input sequence.
    /// </summary>
    /// <remarks>
    /// This compatibility API returns all <c>N</c> complex bins even though the negative-
    /// frequency half is redundant for real input. Use <see cref="ForwardRealCompact"/>
    /// when that redundancy should be hidden from application code.
    /// </remarks>
    public static Complex[] ForwardReal(IReadOnlyList<double> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateFiniteRealInput(input, nameof(input));
        return Forward(input.Select(value => new Complex(value, 0.0)).ToArray());
    }

    /// <summary>
    /// Computes only the non-redundant half-spectrum for a real-valued input sequence.
    /// </summary>
    /// <remarks>
    /// For a non-empty real input of length <c>N</c>, the result stores exactly
    /// <c>N/2 + 1</c> bins: DC, the positive-frequency bins, and the Nyquist bin.
    /// The original length travels with the result so <see cref="InverseReal"/> can
    /// reconstruct the omitted conjugate half without a separate packing convention.
    /// </remarks>
    public static RealFftSpectrum ForwardRealCompact(IReadOnlyList<double> input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var fullSpectrum = ForwardReal(input);
        if (input.Count == 0)
        {
            return new RealFftSpectrum(0, Array.Empty<Complex>());
        }

        var compactBinCount = (input.Count / 2) + 1;
        var compactBins = new Complex[compactBinCount];
        Array.Copy(fullSpectrum, compactBins, compactBinCount);
        return new RealFftSpectrum(input.Count, compactBins);
    }

    /// <summary>
    /// Reconstructs a real-valued sequence from a compact real FFT spectrum.
    /// </summary>
    /// <remarks>
    /// The omitted negative-frequency bins are restored using Hermitian symmetry before
    /// the ordinary complex inverse FFT is evaluated. The DC and Nyquist bins are forced
    /// to their mathematically real values to discard harmless round-off-sized imaginary
    /// components introduced by the forward transform.
    /// </remarks>
    public static double[] InverseReal(RealFftSpectrum spectrum)
    {
        ArgumentNullException.ThrowIfNull(spectrum);

        if (spectrum.OriginalLength == 0)
        {
            return [];
        }

        var length = spectrum.OriginalLength;
        var fullSpectrum = new Complex[length];
        fullSpectrum[0] = new Complex(spectrum[0].Real, 0.0);

        if (length > 1)
        {
            var nyquistBin = length / 2;
            for (var bin = 1; bin < nyquistBin; bin++)
            {
                var value = spectrum[bin];
                fullSpectrum[bin] = value;
                fullSpectrum[length - bin] = Complex.Conjugate(value);
            }

            fullSpectrum[nyquistBin] = new Complex(spectrum[nyquistBin].Real, 0.0);
        }

        var restored = Inverse(fullSpectrum);
        var result = new double[length];
        for (var index = 0; index < length; index++)
        {
            if (!double.IsFinite(restored[index].Real))
            {
                throw new ArithmeticException("Inverse real FFT produced a non-finite sample.");
            }

            result[index] = restored[index].Real;
        }

        return result;
    }

    private static Complex[] Transform(IReadOnlyList<Complex> input, bool inverse)
    {
        if (input.Count == 0) return [];
        if (!IsPowerOfTwo(input.Count))
        {
            throw new ArgumentException("Radix-2 FFT requires a power-of-two input length.", nameof(input));
        }

        var data = input.ToArray();
        BitReversePermutation(data);

        for (var length = 2; length <= data.Length; length <<= 1)
        {
            var angle = (inverse ? 2.0 : -2.0) * System.Math.PI / length;
            var wLength = Complex.FromPolarCoordinates(1.0, angle);
            for (var start = 0; start < data.Length; start += length)
            {
                var w = Complex.One;
                var half = length / 2;
                for (var offset = 0; offset < half; offset++)
                {
                    var even = data[start + offset];
                    var odd = data[start + offset + half] * w;
                    data[start + offset] = even + odd;
                    data[start + offset + half] = even - odd;
                    w *= wLength;
                }
            }

            // Avoid an integer overflow in the loop increment for the largest representable
            // power-of-two array length. Real-world allocations will normally be far smaller,
            // but the guard keeps the control flow correct independently of machine memory.
            if (length == data.Length)
            {
                break;
            }
        }

        if (inverse)
        {
            for (var i = 0; i < data.Length; i++) data[i] /= data.Length;
        }

        return data;
    }

    private static void ValidateFiniteRealInput(IReadOnlyList<double> input, string parameterName)
    {
        for (var index = 0; index < input.Count; index++)
        {
            if (!double.IsFinite(input[index]))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"Real-valued input sample at index {index} must be finite.");
            }
        }
    }

    private static void ValidateFiniteComplexInput(IReadOnlyList<Complex> input, string parameterName)
    {
        for (var index = 0; index < input.Count; index++)
        {
            var value = input[index];
            if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"Complex input sample at index {index} must have finite real and imaginary parts.");
            }
        }
    }

    private static void BitReversePermutation(Complex[] data)
    {
        var j = 0;
        for (var i = 1; i < data.Length; i++)
        {
            var bit = data.Length >> 1;
            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }
            j ^= bit;
            if (i < j) (data[i], data[j]) = (data[j], data[i]);
        }
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A positive transform length is required.");
        }

        var result = 1;
        while (result < value)
        {
            if (result >= (1 << 30))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "The required radix-2 transform length exceeds the supported Int32 array range.");
            }

            result <<= 1;
        }

        return result;
    }
}
