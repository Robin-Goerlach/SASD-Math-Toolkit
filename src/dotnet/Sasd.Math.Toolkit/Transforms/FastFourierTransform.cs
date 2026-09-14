using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>
/// Radix-2 Cooley-Tukey FFT plus convolution and cross-correlation helpers.
/// </summary>
public static class FastFourierTransform
{
    public static Complex[] Forward(IReadOnlyList<Complex> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Transform(input, inverse: false);
    }

    public static Complex[] Inverse(IReadOnlyList<Complex> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Transform(input, inverse: true);
    }

    public static Complex[] ForwardReal(IReadOnlyList<double> input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Forward(input.Select(value => new Complex(value, 0.0)).ToArray());
    }

    public static double[] ConvolveReal(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Count == 0 || right.Count == 0) return [];

        var outputLength = left.Count + right.Count - 1;
        var size = NextPowerOfTwo(outputLength);
        var a = new Complex[size];
        var b = new Complex[size];
        for (var i = 0; i < left.Count; i++) a[i] = new Complex(left[i], 0.0);
        for (var i = 0; i < right.Count; i++) b[i] = new Complex(right[i], 0.0);
        a = Forward(a);
        b = Forward(b);
        for (var i = 0; i < size; i++) a[i] *= b[i];
        var result = Inverse(a);
        return result.Take(outputLength).Select(value => value.Real).ToArray();
    }

    public static double[] CrossCorrelateReal(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var reversed = right.Reverse().ToArray();
        return ConvolveReal(left, reversed);
    }

    private static Complex[] Transform(IReadOnlyList<Complex> input, bool inverse)
    {
        if (input.Count == 0) return [];
        if (!IsPowerOfTwo(input.Count)) throw new ArgumentException("Radix-2 FFT requires a power-of-two input length.", nameof(input));

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
        }

        if (inverse)
        {
            for (var i = 0; i < data.Length; i++) data[i] /= data.Length;
        }

        return data;
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
        var result = 1;
        while (result < value) result <<= 1;
        return result;
    }
}
