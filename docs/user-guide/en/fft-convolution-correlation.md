# FFT, convolution and correlation

The Fast Fourier Transform (FFT) changes a sampled sequence from the time/sample domain into a discrete frequency representation. SASD Math Toolkit currently uses a readable radix-2 Cooley-Tukey reference implementation.

## Complex FFT

For complex samples, use `Forward` and `Inverse`:

```csharp
using System.Numerics;
using Sasd.Numerics.Transforms;

Complex[] samples = [1, 2, 3, 4];
var spectrum = FastFourierTransform.Forward(samples);
var restored = FastFourierTransform.Inverse(spectrum);
```

The forward transform is unscaled. The inverse transform divides by the sequence length, so a forward/inverse round trip returns the original sequence within floating-point accuracy.

Every non-empty sequence must currently have a power-of-two length.

## Real input: full or compact spectrum

`ForwardReal` is the straightforward compatibility API. It converts the real samples to complex values and returns all `N` Fourier bins.

For most application code, prefer `ForwardRealCompact`:

```csharp
using Sasd.Numerics.Transforms;

double[] samples = [1.0, 0.0, -1.0, 0.0, 1.0, 0.0, -1.0, 0.0];
var spectrum = FastFourierTransform.ForwardRealCompact(samples);

Console.WriteLine(spectrum.OriginalLength); // 8
Console.WriteLine(spectrum.BinCount);       // 5

for (var k = 0; k < spectrum.BinCount; k++)
{
    Console.WriteLine($"bin={k}, f/Fs={spectrum.GetNormalizedFrequency(k)}, magnitude={spectrum[k].Magnitude}");
}
```

Because the input is real, the negative-frequency bins are complex conjugates of the positive-frequency bins. Storing both halves would duplicate information. `RealFftSpectrum` therefore stores DC through Nyquist only.

If your sample rate is `sampleRate`, the physical frequency of bin `k` is

```csharp
var frequencyHz = spectrum.GetNormalizedFrequency(k) * sampleRate;
```

## Reconstructing the real signal

Use `InverseReal` with the compact result:

```csharp
var restored = FastFourierTransform.InverseReal(spectrum);
```

The toolkit rebuilds the omitted conjugate half internally. Application code does not need to know the packing layout.

## FFT versus Fourier least-squares approximation

Do not confuse this chapter with `LeastSquares.FitFiveTermFourier`. The FFT analyzes frequency bins of a regularly sampled sequence. The five-term least-squares model fits a small periodic model at a caller-specified fundamental frequency and can work with irregular x coordinates. They solve different problems despite both using sine/cosine ideas.

## Convolution and correlation

Real-valued helpers are currently available through `ConvolveReal` and `CrossCorrelateReal`. Complex-valued counterparts are still V1 work items and will be documented here when their API is stable.

## Practical limits

The current implementation prioritizes clarity and testability rather than maximum FFT throughput. It allocates intermediate arrays and the compact real API currently computes a full FFT before discarding the redundant half. Later optimized or native backends can improve this while preserving the public API.

Always be explicit about sample rate, windowing, scaling and spectral leakage when interpreting a spectrum. The FFT computes the transform of the samples you provide; it does not automatically apply a window or infer a physical sampling interval.
