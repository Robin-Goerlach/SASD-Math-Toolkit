# Root-finding and polynomial contract

This contract synchronizes scalar, complex and polynomial root behavior across
SASD Math Toolkit implementations. Language-specific callback, exception and
ownership types remain implementation details.

## Polynomial representation

Coefficients use descending power order. `[a0, a1, ..., an]` represents
`a0*x^n + a1*x^(n-1) + ... + an`. Coefficients and evaluation arguments must
be finite. Constructors remove exact leading zeros but must not use a tolerance
that changes the mathematical degree.

Required operations are Horner evaluation, combined value/first-/second-
derivative evaluation, formal first derivative and synthetic division by
`(x-root)` with an explicit remainder.

## Scalar solver identifiers

| Identifier | Required inputs | Special invariant |
| --- | --- | --- |
| `root.bisection` | function, ordered finite endpoints | initial endpoint values have opposite signs unless an endpoint is already a root |
| `root.newton-raphson` | function, derivative, finite start | derivative must remain numerically usable |
| `root.secant` | function, two finite starts | successive function values must define a usable slope |

The common convergence test accepts either `|f(x)| <= tolerance` or
`|x-current - x-previous| <= tolerance * max(1, |x-previous|)`. A solver still
returns the actual residual so callers can apply stronger problem-specific
acceptance criteria.

## Complex and polynomial identifiers

| Identifier | Meaning |
| --- | --- |
| `root.muller` | complex Muller iteration from three points |
| `root.newton-horner` | complex Newton iteration using extended Horner evaluation |
| `root.laguerre` | one complex polynomial root |
| `root.laguerre-all` | repeated Laguerre search, deflation and original-polynomial polishing |

Muller and Laguerre choose the larger-magnitude candidate denominator to reduce
subtractive cancellation. The all-roots operation is deterministic, may use a
fixed Cauchy-bound fallback pattern, returns a partial root set on failure and
reports its final maximum residual against the original polynomial.

## Failure and status semantics

- malformed or non-finite caller input is rejected as an API contract error;
- a non-finite callback result is rejected before it can enter another callback;
- missing bisection sign change returns `not_bracketed` and no invented root;
- a near-zero derivative, secant slope or complex denominator returns
  `numerical_breakdown`;
- exhausted iteration budget returns `maximum_iterations_reached` together
  with the best finite estimate and residual available;
- `converged` never means that a particular root among several possible roots
  was selected intentionally.

Single recurrences are sequential. Implementations must not claim useful
multi-core execution merely by adding threads to dependent iterations; batch
root searches may be parallelized separately once their deterministic contract
is defined.
