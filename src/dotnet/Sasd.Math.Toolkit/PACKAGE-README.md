# SASD Math Toolkit

`Sasd.Math.Toolkit` is the .NET 10 reference implementation of the SASD mathematical foundation. It combines independently implemented classical numerical methods with a modern 2026 layer for robust dense and sparse linear algebra.

Current areas include root finding, interpolation, differentiation, integration, dense matrix decompositions, sparse Krylov solvers, eigenvalue methods, ordinary differential equations, least-squares approximation, FFT/convolution/correlation and numerical diagnostics.

## Small example

```csharp
using Sasd.Numerics.RootFinding;

var root = RootSolvers.NewtonRaphson(
    x => Math.Cos(x) - x,
    x => -Math.Sin(x) - 1.0,
    initialGuess: 0.5);

if (root.Converged)
{
    Console.WriteLine(root.Root);
}
```

Modern sparse systems use canonical CSR storage and status-bearing iterative results. For example, symmetric positive-definite systems can use CG/PCG, while general square nonsymmetric systems can use restarted GMRES or BiCGSTAB.

## Design goals

- explicit tolerances and diagnostic status instead of hidden global convergence state;
- defensive public ownership and validation of non-finite input;
- readable dependency-free managed reference algorithms;
- numerically stronger modern formulations where appropriate, including Householder QR, Cholesky and SVD;
- optional higher-performance backends only behind stable abstractions and only when measured workloads justify them.

## Documentation and source

Repository and full documentation:

https://github.com/Robin-Goerlach/SASD-Math-Toolkit

The Markdown handbook under `docs/user-guide/` is the editorial source of truth. German and English PDF editions are generated from that material through the repository's LaTeX build layer.

## Provenance and license

SASD Math Toolkit is an independent clean-room implementation. Historical numerical-toolbox material is used only as a high-level functional/reference catalog; historical source code and substantial manual text are not copied into this project.

License: MIT.
