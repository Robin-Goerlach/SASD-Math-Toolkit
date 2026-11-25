# Polynomial and complex roots

This document describes the first dedicated polynomial-root layer of the C# implementation.

## Design intent

The historical V1 compatibility catalog needs Newton-Horner, Muller and Laguerre methods with polynomial deflation. Instead of embedding polynomial arithmetic inside individual solvers, the SASD implementation introduces a small reusable `Sasd.Numerics.Polynomials` foundation. This is intentionally useful beyond V1: interpolation, approximation, symbolic helpers and later language ports can share the same coefficient convention and reference vectors.

`Polynomial` is immutable and stores coefficients in **descending power order**. For example, `[1, -6, 11, -6]` means `x^3 - 6x^2 + 11x - 6`.

## Available operations

- `Polynomial.Evaluate` — Horner evaluation.
- `Polynomial.EvaluateWithDerivatives` — value, first derivative and second derivative in one extended-Horner pass.
- `Polynomial.Derivative` — creates the formal first derivative.
- `Polynomial.Deflate` — synthetic division by `(x - root)`, returning quotient and remainder.
- `PolynomialRootSolvers.NewtonHorner` — Newton iteration specialized for polynomials.
- `ComplexRootSolvers.Muller` — general complex Muller iteration for a supplied function.
- `PolynomialRootSolvers.Laguerre` — one complex polynomial root.
- `PolynomialRootSolvers.FindAllRootsLaguerre` — repeated Laguerre search, synthetic deflation and final root polishing.

All complex APIs use `System.Numerics.Complex`.

## Numerical behavior

The implementation deliberately favors readable numerical safeguards over early optimization. Muller chooses the larger quadratic denominator to reduce cancellation. Laguerre makes the same larger-denominator choice for its two possible steps. Multi-root Laguerre uses deterministic fallback starts on a Cauchy root-bound circle only when the zero start does not converge.

Deflation necessarily propagates rounding error. Therefore the all-roots routine uses the deflated polynomial only to discover subsequent roots and then polishes every discovered root against the original polynomial. `PolynomialRootsResult.MaximumResidual` reports the largest final `|p(root)|` against that original polynomial.

The coefficient constructor removes only **exact** leading zeros. It intentionally does not discard merely small leading coefficients, because doing so would silently change the mathematical degree.

## Scope and future work

This milestone closes the root-finding items in the historical V1 compatibility matrix. It does not yet attempt advanced polynomial conditioning, companion-matrix methods, arbitrary precision, interval arithmetic or high-performance vectorized evaluation. Those are separate future concerns and should not complicate the understandable reference implementation prematurely.
