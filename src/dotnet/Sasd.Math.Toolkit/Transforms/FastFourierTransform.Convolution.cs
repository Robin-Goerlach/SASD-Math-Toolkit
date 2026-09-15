using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>
/// Convolution and cross-correlation operations built on the shared FFT implementation.
/// </summary>
public static partial class FastFourierTransform
{
    /// <summary>
    /// Computes the full linear convolution of two complex sequences.
    /// </summary>
    /// <remarks>
    /// The output length is <c>left.Count + right.Count - 1</c>. Both inputs are zero-padded
    /// to the next radix-2 length, transformed with the shared FFT, multiplied pointwise and
    /// transformed back. An empty input produces an empty result.
    /// </remarks>
    public static Complex[] ConvolveComplex(
        IReadOnlyList<Complex> left,
        IReadOnlyList<Complex> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteComplexInput(left, nameof(left));
        ValidateFiniteComplexInput(right, nameof(right));

        return ConvolveComplexCore(left, right);
    }

    /// <summary>
    /// Computes the full linear convolution of two real-valued sequences.
    /// </summary>
    /// <remarks>
    /// This convenience overload deliberately delegates to the same complex FFT convolution
    /// core as <see cref="ConvolveComplex"/>. The small imaginary round-off component of the
    /// inverse transform is discarded after the result has been checked for finiteness.
    /// </remarks>
    public static double[] ConvolveReal(
        IReadOnlyList<double> left,
        IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteRealInput(left, nameof(left));
        ValidateFiniteRealInput(right, nameof(right));

        var complexLeft = ToComplex(left);
        var complexRight = ToComplex(right);
        return ExtractRealPart(ConvolveComplexCore(complexLeft, complexRight));
    }

    /// <summary>
    /// Computes the full complex cross-correlation of two sequences.
    /// </summary>
    /// <remarks>
    /// The implemented convention is
    /// <c>r[l] = sum_k left[k] * conjugate(right[k-l])</c> over the valid overlap.
    /// The returned array contains lags from <c>-(right.Count-1)</c> through
    /// <c>left.Count-1</c>; therefore result index <c>i</c> corresponds to
    /// <c>lag = i - (right.Count - 1)</c>. At zero lag this becomes the ordinary complex
    /// inner product of the overlapping sequences.
    /// </remarks>
    public static Complex[] CrossCorrelateComplex(
        IReadOnlyList<Complex> left,
        IReadOnlyList<Complex> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteComplexInput(left, nameof(left));
        ValidateFiniteComplexInput(right, nameof(right));

        return CrossCorrelateComplexCore(left, right);
    }

    /// <summary>
    /// Computes the full real-valued cross-correlation of two sequences.
    /// </summary>
    /// <remarks>
    /// The lag ordering is identical to <see cref="CrossCorrelateComplex"/>. Because real
    /// values are unchanged by complex conjugation, this preserves the behavior of the
    /// historical real helper while sharing the same correlation core.
    /// </remarks>
    public static double[] CrossCorrelateReal(
        IReadOnlyList<double> left,
        IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteRealInput(left, nameof(left));
        ValidateFiniteRealInput(right, nameof(right));

        var complexLeft = ToComplex(left);
        var complexRight = ToComplex(right);
        return ExtractRealPart(CrossCorrelateComplexCore(complexLeft, complexRight));
    }

    /// <summary>
    /// Shared FFT convolution kernel. Inputs must already have been validated by the public
    /// boundary or constructed internally from validated values.
    /// </summary>
    private static Complex[] ConvolveComplexCore(
        IReadOnlyList<Complex> left,
        IReadOnlyList<Complex> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return [];
        }

        int outputLength;
        try
        {
            outputLength = checked(left.Count + right.Count - 1);
        }
        catch (OverflowException exception)
        {
            throw new ArgumentOutOfRangeException(
                nameof(left),
                "The requested convolution result is too large for an Int32-indexed array.",
                exception);
        }

        var transformLength = NextPowerOfTwo(outputLength);
        var paddedLeft = new Complex[transformLength];
        var paddedRight = new Complex[transformLength];

        for (var index = 0; index < left.Count; index++)
        {
            paddedLeft[index] = left[index];
        }

        for (var index = 0; index < right.Count; index++)
        {
            paddedRight[index] = right[index];
        }

        var leftSpectrum = Forward(paddedLeft);
        var rightSpectrum = Forward(paddedRight);
        for (var bin = 0; bin < transformLength; bin++)
        {
            leftSpectrum[bin] *= rightSpectrum[bin];
        }

        var paddedResult = Inverse(leftSpectrum);
        var result = new Complex[outputLength];
        Array.Copy(paddedResult, result, outputLength);
        return result;
    }

    /// <summary>
    /// Expresses correlation as convolution with a reversed, conjugated right sequence.
    /// Keeping this identity in one place ensures that real and complex overloads use the
    /// same lag convention.
    /// </summary>
    private static Complex[] CrossCorrelateComplexCore(
        IReadOnlyList<Complex> left,
        IReadOnlyList<Complex> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return [];
        }

        var reversedConjugatedRight = new Complex[right.Count];
        for (var index = 0; index < right.Count; index++)
        {
            reversedConjugatedRight[index] = Complex.Conjugate(right[right.Count - 1 - index]);
        }

        return ConvolveComplexCore(left, reversedConjugatedRight);
    }

    private static Complex[] ToComplex(IReadOnlyList<double> input)
    {
        var result = new Complex[input.Count];
        for (var index = 0; index < input.Count; index++)
        {
            result[index] = new Complex(input[index], 0.0);
        }

        return result;
    }

    private static double[] ExtractRealPart(IReadOnlyList<Complex> input)
    {
        var result = new double[input.Count];
        for (var index = 0; index < input.Count; index++)
        {
            if (!double.IsFinite(input[index].Real) || !double.IsFinite(input[index].Imaginary))
            {
                throw new ArithmeticException("FFT convolution produced a non-finite output sample.");
            }

            result[index] = input[index].Real;
        }

        return result;
    }
}
