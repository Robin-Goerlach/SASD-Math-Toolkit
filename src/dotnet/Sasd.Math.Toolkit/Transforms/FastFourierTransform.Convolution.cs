using System.Numerics;

namespace Sasd.Numerics.Transforms;

/// <summary>Convolution and cross-correlation operations built on the shared FFT implementation.</summary>
public static partial class FastFourierTransform
{
    public static Complex[] ConvolveComplex(IReadOnlyList<Complex> left, IReadOnlyList<Complex> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteComplexInput(left, nameof(left));
        ValidateFiniteComplexInput(right, nameof(right));
        return ConvolveComplexCore(left, right, correlate: false);
    }

    public static double[] ConvolveReal(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteRealInput(left, nameof(left));
        ValidateFiniteRealInput(right, nameof(right));
        return ConvolveRealCore(left, right, correlate: false);
    }

    /// <summary>
    /// Computes <c>r[l] = sum_k left[k] * conjugate(right[k-l])</c>. Result index <c>i</c>
    /// corresponds to lag <c>i - (right.Count - 1)</c>.
    /// </summary>
    public static Complex[] CrossCorrelateComplex(IReadOnlyList<Complex> left, IReadOnlyList<Complex> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteComplexInput(left, nameof(left));
        ValidateFiniteComplexInput(right, nameof(right));
        return ConvolveComplexCore(left, right, correlate: true);
    }

    public static double[] CrossCorrelateReal(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ValidateFiniteRealInput(left, nameof(left));
        ValidateFiniteRealInput(right, nameof(right));
        return ConvolveRealCore(left, right, correlate: true);
    }

    private static Complex[] ConvolveComplexCore(
        IReadOnlyList<Complex> left,
        IReadOnlyList<Complex> right,
        bool correlate)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return [];
        }

        var outputLength = GetConvolutionOutputLength(left.Count, right.Count);
        var transformLength = NextPowerOfTwo(outputLength);
        var leftWork = new Complex[transformLength];
        var rightWork = new Complex[transformLength];

        for (var index = 0; index < left.Count; index++)
        {
            leftWork[index] = left[index];
        }

        if (correlate)
        {
            // Correlation is convolution with a reversed, conjugated right sequence. Build that
            // sequence directly in the zero-padded transform buffer instead of allocating an
            // intermediate reversed array.
            for (var index = 0; index < right.Count; index++)
            {
                rightWork[index] = Complex.Conjugate(right[right.Count - 1 - index]);
            }
        }
        else
        {
            for (var index = 0; index < right.Count; index++)
            {
                rightWork[index] = right[index];
            }
        }

        ExecuteConvolutionInPlace(leftWork, rightWork);
        var result = new Complex[outputLength];
        Array.Copy(leftWork, result, outputLength);
        return result;
    }

    private static double[] ConvolveRealCore(
        IReadOnlyList<double> left,
        IReadOnlyList<double> right,
        bool correlate)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return [];
        }

        var outputLength = GetConvolutionOutputLength(left.Count, right.Count);
        var transformLength = NextPowerOfTwo(outputLength);
        var leftWork = new Complex[transformLength];
        var rightWork = new Complex[transformLength];

        // Real overloads now populate the padded complex buffers directly. The previous path first
        // allocated two exact-size complex arrays and then copied them into two padded arrays.
        for (var index = 0; index < left.Count; index++)
        {
            leftWork[index] = new Complex(left[index], 0.0);
        }

        if (correlate)
        {
            for (var index = 0; index < right.Count; index++)
            {
                rightWork[index] = new Complex(right[right.Count - 1 - index], 0.0);
            }
        }
        else
        {
            for (var index = 0; index < right.Count; index++)
            {
                rightWork[index] = new Complex(right[index], 0.0);
            }
        }

        ExecuteConvolutionInPlace(leftWork, rightWork);
        var result = new double[outputLength];
        for (var index = 0; index < outputLength; index++)
        {
            result[index] = leftWork[index].Real;
        }

        return result;
    }

    /// <summary>
    /// Transforms the two owned padded arrays in place, stores their spectral product in
    /// <paramref name="leftWork"/>, and inverse-transforms that same array. No full-length
    /// spectrum/result copies are required.
    /// </summary>
    private static void ExecuteConvolutionInPlace(Complex[] leftWork, Complex[] rightWork)
    {
        TransformInPlace(leftWork, inverse: false);
        TransformInPlace(rightWork, inverse: false);

        for (var bin = 0; bin < leftWork.Length; bin++)
        {
            var product = leftWork[bin] * rightWork[bin];
            if (!double.IsFinite(product.Real) || !double.IsFinite(product.Imaginary))
            {
                throw new ArithmeticException("FFT convolution spectrum multiplication produced a non-finite value.");
            }

            leftWork[bin] = product;
        }

        TransformInPlace(leftWork, inverse: true);
    }

    private static int GetConvolutionOutputLength(int leftCount, int rightCount)
    {
        try
        {
            return checked(leftCount + rightCount - 1);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(leftCount), "The requested convolution result is too large for an Int32-indexed array.");
        }
    }
}
