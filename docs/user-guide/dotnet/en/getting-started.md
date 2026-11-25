# Getting started with C#/.NET

## Current development form

SASD Math Toolkit currently targets .NET 10 and is developed as a class library inside this repository. Until a package feed is introduced, the simplest development setup is to reference `src/dotnet/Sasd.Math.Toolkit/Sasd.Math.Toolkit.csproj` from another project or work directly with the repository solution.

Build the complete repository with:

```bash
dotnet restore Sasd.Math.Toolkit.slnx
dotnet build Sasd.Math.Toolkit.slnx --configuration Release
dotnet test Sasd.Math.Toolkit.slnx --configuration Release
```

## Namespace organization

The assembly is named `Sasd.Math.Toolkit`, while numerical APIs use the `Sasd.Numerics` namespace root. This avoids ambiguity with `System.Math` and keeps the mathematical domains explicit, for example:

```csharp
using Sasd.Numerics.Differentiation;
using Sasd.Numerics.Integration;
using Sasd.Numerics.LinearAlgebra;
```

## A first calculation

For a callable function, a first derivative can be estimated directly:

```csharp
var derivative = NumericalDifferentiation.FirstDerivative(Math.Sin, 0.3);
Console.WriteLine(derivative);
```

The result should be close to `Math.Cos(0.3)`. Numerical methods return approximations, so comparisons should normally use tolerances rather than exact floating-point equality.

## Run the graphical numerical demo

The repository contains a cross-platform sample that exercises real toolkit APIs and generates a self-contained HTML/SVG report:

```bash
dotnet run --project samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj
```

Open the generated `sasd-math-toolkit-demo.html` file in a browser. The report currently includes root finding, integration, compact real FFT analysis, convolution and correlation. It has no network or JavaScript dependency, so it is also useful as a deterministic smoke/demo artifact.

This sample replaces the role of the historical graphics demonstrations with newly written C#/.NET code. Rendering remains outside the numerical library so application projects are not forced to depend on a particular UI or charting technology.

## Error handling

Programming-contract errors such as incompatible dimensions, invalid indices or non-finite inputs are generally reported through standard .NET exceptions. Iterative algorithms whose convergence can legitimately fail usually return a result object containing status, iteration count and residual information.

This distinction is intentional: malformed input is different from a mathematically valid iteration that simply does not converge within the requested limits.
