# SASD Math Toolkit sample / graphical demo

This project is the modern demonstration application for the C#/.NET implementation. It replaces the historical graphics-demo role with a new clean-room, cross-platform report generator rather than recreating a DOS graphics environment.

Run it from the repository root:

```bash
dotnet run --project samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj
```

By default it writes `sasd-math-toolkit-demo.html` to the current directory. An explicit destination can be supplied as the single command-line argument:

```bash
dotnet run --project samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj -- ./artifacts/demo.html
```

Open the generated HTML file in any modern browser. The document is self-contained: SVG charts and CSS are embedded, and there are no JavaScript, CDN, charting-library or network dependencies.

The graphical report demonstrates real toolkit calls for Newton root finding, adaptive Simpson integration, compact real FFT analysis, convolution and cross-correlation. Rendering code remains inside the sample project so the numerical library itself stays independent of UI and file formats.

After writing the report, the sample also executes a deterministic **2026 sparse-linear-algebra smoke case**. A 24x24 nonsymmetric tridiagonal system with a known solution is solved with right-preconditioned restarted GMRES and BiCGSTAB. The console output reports both iteration counts and an independently recomputed true residual using `SparseMatrixDiagnostics.ResidualEuclideanNorm`. This is a correctness/release smoke example, not a benchmark or a claim that one solver is faster than the other.
