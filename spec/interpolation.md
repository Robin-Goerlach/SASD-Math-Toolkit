# Interpolation contract

This contract synchronizes interpolation behavior across SASD Math Toolkit
implementations. Language-specific containers, ownership types and exception
classes remain implementation details.

## Coordinate contract

Interpolation coordinates and evaluation points must be finite. Coordinate
arrays have equal nonzero length. Polynomial interpolation accepts distinct
abscissas in any order. Cubic splines require at least two strictly increasing
abscissas because their segments follow consecutive intervals.

Exact duplicate abscissas are invalid. Implementations must also reject a
non-finite intermediate rather than returning it as an interpolation result.

## Algorithm identifiers

| Identifier | Meaning |
| --- | --- |
| `interpolation.linear` | line through two distinct points; extrapolation is allowed |
| `interpolation.lagrange` | direct Lagrange-form polynomial evaluation |
| `interpolation.newton` | Newton divided differences and nested evaluation |
| `interpolation.spline.natural` | cubic spline with zero endpoint second derivatives |
| `interpolation.spline.clamped` | cubic spline with prescribed finite endpoint first derivatives |

Newton coefficients use the order of the supplied abscissas. A reusable
Newton interpolant owns its abscissas and coefficients independently from the
caller's mutable input.

## Spline representation and domain

On segment `i`, the spline is represented as

`S_i(x) = A_i + B_i*dx + C_i*dx^2 + D_i*dx^3`,

where `dx = x - knot_i`. Value, first derivative and second derivative are
available throughout the closed knot interval. Spline extrapolation is
rejected: it must not silently extend the first or last cubic beyond the data
domain.

A valid spline:

- interpolates every supplied knot value;
- is continuous in value and its first two derivatives at interior knots;
- satisfies its natural or clamped endpoint conditions;
- owns its completed knot and coefficient state independently.

## Batch execution

Evaluations at different target points are independent and may be distributed
across CPU workers. Construction of divided differences and the tridiagonal
spline solve remain sequential recurrences.

There is no universal profitable batch size. Implementations may offer
`sequential`, `automatic` and `parallel` policies, but the automatic work
threshold and worker cap must remain caller-adjustable. Selecting `parallel`
requests an execution strategy; it is not a promise that every machine or
application will run faster. Output order must match input order, and worker
failures must be propagated to the caller.
