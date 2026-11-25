# Numerical integration design

## Scope

The V1 integration layer provides composite trapezoid and Simpson rules, adaptive Simpson quadrature, Romberg extrapolation, one-panel five-point Gauss-Legendre quadrature, and adaptive five-point Gauss-Legendre refinement. The implementation is independent clean-room code; historical documentation is used only to define the compatibility target.

## API layering

The fixed rules return a `double` because they have no convergence state beyond their caller-selected panel count. Adaptive Simpson and adaptive Gauss-Legendre now have two layers: the original convenience methods return the best value estimate, while `AdaptiveSimpsonDetailed` and `AdaptiveGaussLegendre5Detailed` return `AdaptiveIntegrationResult` with status, an error indicator, callback count, and accepted-panel count.

Romberg follows the same non-breaking pattern. `Romberg` retains the scalar return type, while `RombergDetailed` reports `IterationStatus`, levels, callback count, and the last diagonal difference.

This lets simple applications remain concise without forcing release-quality applications to discard convergence information.

## Validation contract

Public routines require finite `a` and `b`, `a < b`, and a positive representable interval width. Fixed-panel methods require positive panel counts; composite Simpson additionally requires an even count. Adaptive tolerances and depth limits must be positive. Romberg is capped at 30 levels because its reference subdivision model uses powers of two stored in `int`.

Every callback result is checked for finiteness. Non-finite integrand results are programming/data-domain failures and therefore raise `ArithmeticException`; they are not treated as ordinary adaptive non-convergence.

Intermediate estimates are also checked so arithmetic overflow is surfaced close to its source instead of returning `NaN` or infinity as a plausible integral.

## Adaptive Simpson

The routine compares a Simpson estimate over `[a,b]` with the sum of Simpson estimates over the two half intervals. The local error indicator is `|split - whole| / 15`, and the returned value includes the classical `delta / 15` correction. The requested tolerance is split between recursive children.

When the local criterion is not met before the depth limit, the best corrected estimate is still returned but the status becomes `MaximumDepthReached`. If floating-point values can no longer represent a distinct midpoint, the status becomes `NumericalResolutionReached`.

## Romberg

Romberg reuses the previous trapezoid level and evaluates only the new odd-grid samples. Richardson extrapolation then constructs the diagonal sequence. Convergence is tested by the absolute difference between consecutive diagonal estimates. If the configured level count is exhausted, the last estimate is returned with `IterationStatus.MaximumIterationsReached` rather than silently claiming convergence.

## Gauss-Legendre

The five-node rule maps standard Gauss-Legendre nodes and weights from `[-1,1]` onto the requested interval. The adaptive form compares one rule over a panel with the sum of two rules over its halves. When recursion continues, those already-computed half-panel estimates are reused as the children's whole estimates; this avoids unnecessary callback duplication without changing the public model.

The adaptive Gauss error value is the refinement difference used by the algorithm. It is an indicator, not a rigorous universal error bound.

## Performance posture

The code intentionally remains a readable reference implementation. There are no vectorized kernels, pooled work buffers, parallel callback execution, or specialized singularity transforms in V1. Such optimizations can be considered later behind the same public contracts after profiling demonstrates a need.
