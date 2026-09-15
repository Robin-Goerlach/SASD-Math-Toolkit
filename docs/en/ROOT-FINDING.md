# Root finding

This note documents the implementation contracts behind the scalar and complex root-finding APIs. The user-oriented explanation and examples live in `docs/user-guide/en/root-finding.md`.

## Design principles

Root finding is treated as an iterative numerical operation with two different classes of failure. Invalid programming input is rejected with ordinary .NET exceptions. Mathematically valid iterations that cannot continue reliably return an explicit `IterationStatus` such as `NotBracketed`, `NumericalBreakdown`, or `MaximumIterationsReached`.

The implementation intentionally keeps convergence settings explicit through `RootFindingOptions`. There is no mutable process-wide tolerance or hidden global iteration state.

## Scalar convergence rule

Bisection, Newton-Raphson and secant iteration accept convergence when either

`|f(x)| <= tolerance`

or the change in the iterate satisfies

`|x(n) - x(n-1)| <= tolerance * max(1, |x(n-1)|)`.

The mixed residual/step rule is practical for a reference implementation, but callers performing sensitive work should still inspect the returned residual instead of treating `Converged` as a substitute for problem-specific error analysis.

## Numerical breakdown

Newton-Raphson refuses to divide by a derivative whose magnitude is at or below the toolkit's near-zero threshold. Secant iteration applies the same idea to the difference of the two function values. Bisection reports `NotBracketed` when the initial endpoint values have the same sign.

Any non-finite callback result is rejected immediately. Iterative updates are also checked before a callback is invoked again so an overflowed Newton or secant step cannot silently pass `Infinity` back into user code.

## Complex and polynomial solvers

Muller's method works directly with `System.Numerics.Complex` and can therefore leave the real axis naturally. Polynomial Newton-Horner and Laguerre reuse the immutable `Polynomial` type and extended Horner evaluation.

`FindAllRootsLaguerre` uses repeated Laguerre search and synthetic deflation. Its deterministic fallback starts are based on a Cauchy root bound. After all roots have been found, each root is polished against the original polynomial to reduce accumulated deflation error before the final maximum residual is reported.

## Optimization policy

The current code favors explicit control flow, readable diagnostics and reproducible behavior. More advanced safeguarded hybrids, scaling strategies or specialized high-performance polynomial solvers can be added later without changing the current result/status model.
