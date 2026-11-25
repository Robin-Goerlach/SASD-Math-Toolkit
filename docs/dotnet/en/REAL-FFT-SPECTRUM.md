# Compact real FFT spectrum

## Purpose

`FastFourierTransform.ForwardRealCompact` is the user-facing real-signal FFT convenience API. It keeps only the non-redundant half of the spectrum and returns a `RealFftSpectrum` that remembers the original signal length.

This is an independently written C# implementation. The historical Borland toolbox is used only as a functional V1 milestone; historical source code and handbook prose are not reused.

## Why a real spectrum can be compact

For a real-valued input sequence, the discrete Fourier transform has Hermitian symmetry:

`X[N-k] = conjugate(X[k])`.

The negative-frequency bins therefore contain no independent information. For an input length `N`, the compact representation stores bins

`0 .. N/2`

for a total of `N/2 + 1` bins when the input is non-empty. Bin 0 is DC. For an even radix-2 length, bin `N/2` is the Nyquist bin.

The older `ForwardReal` API remains available and returns all `N` complex bins. The compact API exists so application code does not need to remember or reconstruct the symmetry manually.

## Result model

`RealFftSpectrum` is immutable and exposes:

- `OriginalLength` — the number of real input samples;
- `BinCount` — the number of stored bins;
- `Bins` — a read-only view of the bins;
- an indexer for direct bin access;
- `GetNormalizedFrequency(k)` — bin frequency in cycles per sample;
- `ToArray()` — a defensive copy when an array is required.

If the physical sample rate is `Fs`, the frequency represented by bin `k` is

`f_k = k * Fs / N`.

Equivalently, `GetNormalizedFrequency(k) * Fs` gives the frequency in hertz when `Fs` is measured in samples per second.

## Inverse transform

`FastFourierTransform.InverseReal` accepts a `RealFftSpectrum`. It restores the omitted negative-frequency bins using complex conjugation, then delegates to the same normalized complex inverse FFT used elsewhere in the toolkit.

The forward FFT is unscaled and the inverse divides by `N`, so a forward/inverse round trip reconstructs the original real sequence within floating-point round-off.

## Example

```csharp
using Sasd.Numerics.Transforms;

double[] samples = [1.0, 0.0, -1.0, 0.0, 1.0, 0.0, -1.0, 0.0];

var spectrum = FastFourierTransform.ForwardRealCompact(samples);

for (var k = 0; k < spectrum.BinCount; k++)
{
    Console.WriteLine($"{k}: f/Fs={spectrum.GetNormalizedFrequency(k)}, |X|={spectrum[k].Magnitude}");
}

var restored = FastFourierTransform.InverseReal(spectrum);
```

## Current numerical scope

The reference FFT is radix-2 Cooley-Tukey, so every non-empty FFT input length must be a power of two. Empty inputs are represented by an empty compact spectrum. Real input samples must be finite.

The compact implementation currently obtains the full transform and then retains its independent bins. That is intentionally simple and reviewable for V1. A later optimized backend may compute a packed real FFT directly without changing the public `RealFftSpectrum` abstraction.

## Relationship to convolution and correlation

FFT spectra are also an efficient computational path for convolution and cross-correlation. The current V1 library already contains real helpers. Complex convolution and complex cross-correlation remain separate compatibility items and will be completed next rather than overloading this compact-spectrum milestone.
