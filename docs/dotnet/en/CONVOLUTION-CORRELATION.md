# FFT convolution and cross-correlation

## Purpose

`FastFourierTransform` now exposes linear convolution and cross-correlation for both real and complex sequences. These helpers complete the historical V1 convolution/correlation catalog while using one shared FFT implementation rather than duplicating separate numerical kernels for real and complex data.

This is an independently written implementation. Historical Borland material is used only as a functional checklist for V1 coverage; no historical source code or handbook prose is reused.

## Linear convolution

For finite sequences `x` and `h`, linear convolution is

`y[n] = sum_k x[k] * h[n-k]`

over all indices where both sequence elements exist.

The public APIs are:

```csharp
FastFourierTransform.ConvolveComplex(left, right);
FastFourierTransform.ConvolveReal(left, right);
```

For non-empty inputs the result length is always

`left.Count + right.Count - 1`.

An empty input produces an empty output.

## FFT implementation strategy

The reference implementation performs linear convolution by:

1. choosing the required linear-convolution output length;
2. zero-padding both inputs to the next power of two;
3. applying the existing radix-2 FFT to both padded sequences;
4. multiplying corresponding frequency bins;
5. applying the normalized inverse FFT;
6. retaining only the non-padded linear-convolution samples.

This is intentionally readable rather than aggressively optimized. It also means the public convolution contract is independent of how a future optimized backend performs the operation internally.

The real overload converts its data to complex values and delegates to the same complex FFT convolution core. The tiny imaginary terms produced by floating-point round-off are discarded when returning the real result.

## Complex cross-correlation convention

Complex correlation requires an explicit conjugation convention. SASD Math Toolkit defines

`r[l] = sum_k left[k] * conjugate(right[k-l])`

over the valid overlap.

The implementation expresses this as convolution with a reversed, complex-conjugated right sequence. This identity keeps correlation and convolution on the same tested FFT core.

The public APIs are:

```csharp
FastFourierTransform.CrossCorrelateComplex(left, right);
FastFourierTransform.CrossCorrelateReal(left, right);
```

For non-empty inputs the result contains lags from

`-(right.Count - 1)` through `left.Count - 1`.

Therefore output index `i` corresponds to

`lag = i - (right.Count - 1)`.

At zero lag, complex correlation equals the overlapping inner product

`sum_k left[k] * conjugate(right[k])`.

This lag mapping is documented explicitly because correlation libraries often choose different output orderings or sign conventions.

## Example

```csharp
using System.Numerics;
using Sasd.Numerics.Transforms;

Complex[] left = [new(1, 1), new(2, -1)];
Complex[] right = [new(3, 0), new(0, -1)];

var convolution = FastFourierTransform.ConvolveComplex(left, right);
var correlation = FastFourierTransform.CrossCorrelateComplex(left, right);

var zeroLagIndex = right.Length - 1;
Console.WriteLine(correlation[zeroLagIndex]);
```

## Input validation

Complex FFT and convolution/correlation inputs must have finite real and imaginary components. Real helpers require finite samples. Invalid non-finite values are rejected at the public API boundary rather than being allowed to spread silently through a transform.

The base FFT still requires power-of-two lengths for direct `Forward`/`Inverse` calls. Convolution callers do not need to satisfy that constraint because the helper performs its own zero-padding to a suitable radix-2 transform length.

## Numerical notes

FFT convolution has floating-point round-off and should not be compared with exact equality except for specially chosen trivial data. The tests use independent short direct-convolution results and numerical tolerances.

For very short sequences, direct nested-loop convolution could be faster than allocating FFT work arrays. V1 deliberately keeps one clear FFT-backed implementation. Hybrid thresholds, reusable work buffers, SIMD and native FFT backends are optimization topics for later profiling rather than V1 API concerns.
