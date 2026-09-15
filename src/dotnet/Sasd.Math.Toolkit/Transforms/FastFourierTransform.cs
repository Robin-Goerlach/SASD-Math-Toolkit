using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>
/// Dependency-free radix-2 Cooley-Tukey FFT plus convolution and cross-correlation helpers.
/// </summary>
/// <remarks>
/// The public APIs retain defensive-copy semantics: caller-owned arrays are never transformed in
/// place. Internally, however, arrays that the toolkit has just allocated are transformed in place
/// so real FFT, inverse-real reconstruction, convolution and correlation do not create redundant
/// full-length spectrum copies.
/// </remarks>
public static partial class FastFourierTransform
{
    /// <summary>Computes the full complex radix-2 discrete Fourier spectrum.</summary>
    public static Complex[] Forward(IReadOnlyList<Complex> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateFiniteComplexInput(input, nameof(input));
        ValidateTransformLength(input.Count, nameof(input));

        var data = input.ToArray();
        TransformInPlace(data, inverse: false);
        return data;
    }

    /// <summary>Computes the normalized inverse complex radix-2 transform.</summary>
    public static Complex[] Inverse(IReadOnlyList<Complex> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateFiniteComplexInput(input, nameof(input));
        ValidateTransformLength(input.Count, nameof(input));

        var data = input.ToArray();
        TransformInPlace(data, inverse: true);
        return data;
    }

    /// <summary>Computes the full complex spectrum of a real-valued input sequence.</summary>
    public static Complex[] ForwardReal(IReadOnlyList<double> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateFiniteRealInput(input, nameof(input));
        ValidateTransformLength(input.Count, nameof(input));

        if (input.Count == 0)
        {
            return [];
        }

        // Build the complex working array directly. The old path created this array through LINQ,
        // validated it again as complex input and then copied it a second time in Transform().
        var data = new Complex[input.Count];
        for (var index = 0; index < input.Count; index++)
        {
            data[index] = new Complex(input[index], 0.0);
        }

        TransformInPlace(data, inverse: false);
        return data;
    }

    /// <summary>Computes only the non-redundant half-spectrum for a real-valued input sequence.</summary>
    public static RealFftSpectrum ForwardRealCompact(IReadOnlyList<double> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var fullSpectrum = ForwardReal(input);
        if (input.Count == 0)
        {
            return RealFftSpectrum.FromOwnedBins(0, []);
        }

        var compactBinCount = (input.Count / 2) + 1;
        var compactBins = new Complex[compactBinCount];
        Array.Copy(fullSpectrum, compactBins, compactBinCount);

        // compactBins is newly allocated and cannot be observed elsewhere. Transferring ownership
        // avoids another defensive copy while RealFftSpectrum remains immutable to public callers.
        return RealFftSpectrum.FromOwnedBins(input.Count, compactBins);
    }

    /// <summary>Reconstructs a real-valued sequence from a compact real FFT spectrum.</summary>
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

        // fullSpectrum is an internal working array, so an additional public-Inverse copy would
        // provide no safety. Transform it directly and project the real part once.
        TransformInPlace(fullSpectrum, inverse: true);
        var result = new double[length];
        for (var index = 0; index < length; index++)
        {
            result[index] = fullSpectrum[index].Real;
        }

        return result;
    }

    /// <summary>
    /// Transforms an owned working array in place. Public APIs must validate/copy caller-owned data
    /// before reaching this method; internal algorithms may pass arrays they created themselves.
    /// </summary>
    private static void TransformInPlace(Complex[] data, bool inverse)
    {
        if (data.Length == 0)
        {
            return;
        }

        BitReversePermutation(data);
        for (var length = 2; length <= data.Length; length <<= 1)
        {
            var angle = (inverse ? 2.0 : -2.0) * System.Math.PI / length;
            var wLength = Complex.FromPolarCoordinates(1.0, angle);
            var half = length / 2;

            for (var start = 0; start < data.Length; start += length)
            {
                var w = Complex.One;
                for (var offset = 0; offset < half; offset++)
                {
                    var even = data[start + offset];
                    var odd = data[start + offset + half] * w;
                    data[start + offset] = even + odd;
                    data[start + offset + half] = even - odd;
                    w *= wLength;
                }
            }

            if (length == data.Length)
            {
                break;
            }
        }

        if (inverse)
        {
            var scale = 1.0 / data.Length;
            for (var i = 0; i < data.Length; i++)
            {
                data[i] *= scale;
            }
        }

        // One O(N) validation pass is cheap relative to O(N log N) transformation and gives all
        // internal optimized paths the same protection against arithmetic overflow.
        EnsureFiniteTransformOutput(data);
    }

    private static void ValidateTransformLength(int length, string parameterName)
    {
        if (length != 0 && !IsPowerOfTwo(length))
        {
            throw new ArgumentException("Radix-2 FFT requires a power-of-two input length.", parameterName);
        }
    }

    private static void ValidateFiniteRealInput(IReadOnlyList<double> input, string parameterName)
    {
        for (var index = 0; index < input.Count; index++)
        {
            if (!double.IsFinite(input[index]))
            {
                throw new ArgumentOutOfRangeException(parameterName, $"Real-valued input sample at index {index} must be finite.");
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
                throw new ArgumentOutOfRangeException(parameterName, $"Complex input sample at index {index} must have finite real and imaginary parts.");
            }
        }
    }

    private static void EnsureFiniteTransformOutput(IReadOnlyList<Complex> data)
    {
        for (var index = 0; index < data.Count; index++)
        {
            var value = data[index];
            if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary))
            {
                throw new ArithmeticException("FFT arithmetic produced a non-finite value.");
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
            if (i < j)
            {
                (data[i], data[j]) = (data[j], data[i]);
            }
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
                throw new ArgumentOutOfRangeException(nameof(value), "The required radix-2 transform length exceeds the supported Int32 array range.");
            }

            result <<= 1;
        }

        return result;
    }
}
