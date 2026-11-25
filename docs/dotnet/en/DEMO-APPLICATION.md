# V1 graphical demonstration application

## Purpose

The historical Numerical Methods Toolbox included graphical demonstration programs. SASD Math Toolkit replaces that role with a newly written .NET sample application rather than attempting to recreate DOS-era graphics code or redistribute historical examples.

The sample generates a self-contained HTML document with inline SVG charts. This choice keeps the demonstration cross-platform and inspectable while avoiding a UI-framework dependency in the numerical library.

## Architecture boundary

The reusable `Sasd.Math.Toolkit` assembly remains independent from browsers, files and chart rendering. The demonstration code lives under:

`./samples/dotnet/Sasd.Math.Toolkit.Sample/`

The sample references the numerical library, never the other way around. This dependency direction is important because future GUI, notebook or web front ends should be able to use the same mathematical core without inheriting demonstration-specific code.

The tiny `SvgLineChart` renderer is intentionally sample-local. It is not intended to become a general charting API. A future SASD UI/graphics component can replace it without changing the numerical algorithms.

## Demonstrated numerical paths

The generated report currently exercises four representative workflows:

1. Newton-Raphson root finding for `cos(x) - x`.
2. Adaptive Simpson integration of `sin(x)` over `[0, pi]`.
3. Compact real FFT analysis of a deterministic two-tone signal.
4. FFT-backed real convolution and cross-correlation with the documented full-lag convention.

The report shows both numerical summary values and SVG plots. All formatting uses the invariant culture so output is deterministic across developer machines and CI runners.

## Running the demo

From the repository root:

```bash
dotnet run --project samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj
```

The default result is `sasd-math-toolkit-demo.html` in the current directory. A custom output path can be supplied as the single argument.

No network connection is required to view the generated file. CSS and SVG are embedded and no JavaScript or third-party charting library is used.

## Testing

`DemoApplicationTests` verifies that report generation is deterministic, culture-independent, self-contained and free of non-finite numerical output. The CI build also compiles the executable sample together with the library.

This gives the historical graphics-demo compatibility item an explicit modern replacement while keeping visualization concerns outside the reusable numerical core.
